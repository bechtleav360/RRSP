using Meros.Project.Program;
using Meros.Risk;
using Signum.Authorization;

namespace RRSP.Program;

public class NewProgramModel : ModelEntity
{
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    public Lite<UserEntity> Manager { get; set; }

    [StringLengthValidator(Max = 10)]
    public string ProgramPrefix { get; set; }

    public RiskManagementEmbedded? RiskManagement { get; set; }
}

[AutoInit]
public static class ProgramExpandedOperation
{
    public static readonly ConstructSymbol<ProgramEntity>.Simple Create;
}
