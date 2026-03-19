using Meros.Project;
using Meros.Project.Program;
using Meros.Risk;
using Meros.Tasks;
using RRSP.Globals;
using Signum.API;
using Signum.Authorization;
using Signum.Authorization.Rules;

namespace RRSP.Program;

public static class RRSPProgramLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        if (sb.WebServerBuilder != null)
        {
            ReflectionServer.RegisterLike(typeof(NewProgramModel), () => TypeAuthLogic.GetAllowed(typeof(ProgramEntity)).MaxUI() >= TypeAllowedBasic.Read);
        }

        new Graph<ProgramEntity, DomainState>.Construct(ProgramExpandedOperation.Create)
        {
            ToStates = { DomainState.Active },
            Construct = (args) =>
            {
                var model = args.GetArg<NewProgramModel>();
                return CreateProgramFromModel(model);
            },
        }.Register();
    }

    private static ProgramEntity CreateProgramFromModel(NewProgramModel model)
    {
        var role = Starter.Configuration.Value.DefaultProgramMangerRole ?? throw new InvalidOperationException(GlobalMessage.DefaultProgramManagerRoleIsNotConfigured.NiceToString());

        var newProgram = new ProgramEntity
        {
            Name = model.Name,
            Manager = model.Manager,
            State = DomainState.Active,
            Phase = ProjectPhase.Initiating,
        };

        newProgram.Mixin<DomainTaskMixin>().Prefix = model.ProgramPrefix;
        newProgram.Mixin<DomainRiskMixin>().RiskManagement = model.RiskManagement;
        newProgram.Execute(ProgramOperation.Save);

        if (newProgram.Manager != null)
        {
            new MemberEntity
            {
                Domain = newProgram.ToLite(),
                User = newProgram.Manager,
                Role = role,
            }.Execute(MemberOperation.Save);
        }

        if (newProgram.Manager?.Is(UserEntity.Current) == false)
        {
            new MemberEntity
            {
                Domain = newProgram.ToLite(),
                User = UserEntity.Current,
                Role = role,
            }.Execute(MemberOperation.Save);
        }

        return newProgram;
    }
}
