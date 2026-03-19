using System.IO;
using System.Xml.Linq;
using Signum.Authorization;
using Signum.UserAssets;
using Signum.Calendar;
using Signum.Engine.Maps;
using Signum.Engine.Sync;
using Meros.Tasks;
using Signum.Word;
using Signum.Basics;
using Signum.Files;
using Signum.Translation.Instances;
using RRSP.Project;
using DocumentFormat.OpenXml.VariantTypes;
using Meros.Project;
using Meros.PortfolioExt;
using Meros.PortfolioExt.InitiationRequest;

namespace RRSP.Test.Environment;

public class EnvironmentTest
{
    [Fact]
    public void GenerateTestEnvironment()
    {
        var authRules = XDocument.Load(@"..\..\..\..\RRSP.Terminal\AuthRules.xml");

        RRSPEnvironment.Start();
        
        using (Administrator.WithSnapshotOrTemplateDatabase())
        {
            Administrator.TotalGeneration();

            if (Connector.Current is PostgreSqlConnector pgcon)
                pgcon.ChangeConnectionStringDatabase(pgcon.DatabaseName());

            Schema.Current.Initialize();

            OperationLogic.AllowSaveGlobally = true;

            using (AuthLogic.Disable())
            {
                AuthLogic.LoadRoles(authRules);
                BasicLoader.LoadUsers();
                BasicLoader.LoadBasics();
                BasicLoader.CreateProtocolMasterData();

                new TaskLabelTypeEntity { Name = "Sprint" }.Save();
                new TaskLabelTypeEntity { Name = "Team" }.Save();
                new TaskLabelTypeEntity { Name = "Billable", Kind = TaskLabelKind.Billable }.Save();

                new TaskLabelTypeEntity { Name = "Iteration", Kind = TaskLabelKind.Iteration }.Save();
                new TaskLabelTypeEntity { Name = "Area", Kind = TaskLabelKind.Team }.Save();
                new TaskLabelTypeEntity { Name = "TFS-Type", Kind = TaskLabelKind.TaskType }.Save();

               
                CalendarDayLogic.CreateDays(
                    DateTime.Today.ToDateOnly().YearStart().AddYears(-1),
                    DateTime.Today.ToDateOnly().YearStart().AddYears(10));

                new WordTemplateEntity
                {
                    Name = "ProjectInitiationRequest",
                    Query = QueryLogic.GetQueryEntity(typeof(InitiationRequestEntity)),
                    Model = WordModelLogic.GetWordModelEntity(typeof(ProjectInitiationRequestWordModel)),
                    FileName = "Projektinitiierungsantrag.pdf",
                    Template = new FileEntity(@"..\..\..\..\RRSP.Terminal\WordTemplates\Initiation Request.docx").ToLiteFat(),
                    Culture = CultureInfoLogic.GetCultureInfoEntity("de"),
                }.Save();

                PortfolioGlobalConfigLoader.PortfolioGlobalConfiguration();

                TranslatedInstanceLogic.ImportExcelFile(@"..\..\..\..\RRSP.Terminal\InstanceTranslations\PortfolioEvaluationCriteria.en.View.xlsx", MatchTranslatedInstances.ByOriginalText);
                TranslatedInstanceLogic.ImportExcelFile(@"..\..\..\..\RRSP.Terminal\InstanceTranslations\PortfolioEvaluationDimension.en.View.xlsx", MatchTranslatedInstances.ByOriginalText);

                using (AuthLogic.UnsafeUserSession("System"))
                {
                    RRSPLoader.LoadDumyAndAll();
                    RRSPLoader.GenerateStatusReportHistory();

                    UserAssetsImporter.ImportAll(File.ReadAllBytes(@"..\..\..\..\RRSP.Terminal\ToolbarEntity1.xml"));
                }

                AuthLogic.ImportRulesScript(authRules, interactive: false)!.PlainSqlCommand().ExecuteLeaves();
            }
        }

        OperationLogic.AllowSaveGlobally = false;
    }

   
}
