using Meros.PlanningProject.PSC;
using Meros.Project;
using Signum.Authorization;
using Signum.Entities;
using Signum.Engine;
using Signum.Operations;
using Signum.Security;
using System;
using System.Linq;
using Xunit;
using Signum.Utilities;
using Signum.Authorization.Rules;
using Signum.Basics;
using Signum.Engine.Maps;
using Meros.Project.Program;
using Meros.Project.Portfolio;

namespace RRSP.Test.Logic;

public class DomainRoleByChildrenTest
{
    public DomainRoleByChildrenTest()
    {
        RRSPEnvironment.StartAndInitialize();
    }

    [Fact]
    public void RegisterDomainByChildren_GoalEntity_AllowsAccessThroughChildDomains()
    {
        using (var tr = new Transaction())
        {
            var parentPortfolio = Database.Query<PortfolioEntity>().SingleEx(p => p.Name == "Netzausbau");
            var childProgram = Database.Query<ProgramEntity>().SingleEx(pr => pr.Name == "Glasfaser für alle");

            var standardUserRole = Database.Query<RoleEntity>().SingleEx(r => r.Name == "Standard user");
            var testUser = Tools.GetOrCreateUser("Test DomainRole User", standardUserRole);

            var domainRole = Database.Query<DomainRoleEntity>().First(dr => dr.Level == DomainLevel.Program && dr.GetAccessLevel(typeof(GoalEntity)) != null);

            using (OperationLogic.AllowSave<MemberEntity>())
            {
                new MemberEntity
                {
                    User = testUser.ToLite(),
                    Domain = childProgram.ToLite(),
                    Role = domainRole.ToLite(),
                    Abbreviation = "TST",
                }.Save();
            }

            var retrievedGoal = Database.Query<GoalEntity>().SingleEx(g => g.Domain.Is(parentPortfolio));
            using (AuthLogic.Enable())
            using (UserHolder.UserSession(testUser))
            {
                var result = TypeAuthLogic.IsAllowedForDebug(retrievedGoal, TypeAllowedBasic.Read, true, FilterQueryArgs.FromEntity(retrievedGoal));
                Assert.True(retrievedGoal.InTypeCondition(DomainCondition.ByChildrenDomain));

                retrievedGoal.ToLite().Retrieve();
            }
        }
    }
}
