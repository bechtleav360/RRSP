using RRSP.Globals;
using Meros.PlanningProject.BusinessCase;
using Meros.PlanningProject.PSC;
using Meros.PlanningProject.WorkPackage;
using Meros.PortfolioExt;
using Meros.Project;
using Meros.Protocol;
using Meros.StatusReport;
using Signum.Basics;
using Signum.DynamicQuery;
using Signum.Engine.Sync;
using Signum.Files;
using Signum.UserAssets.Queries;
using Signum.Word;
using System.IO;
using Meros.PortfolioExt.InitiationRequest;
using Meros.Project.Portfolio;

namespace RRSP.Terminal;

static class WordTempleLoader
{
    public static void UpdateDomainRiskTemplate()
    {
        Database.Query<WordTemplateEntity>()
            .Where(t => t.Name.Contains("Risk")).UnsafeUpdate(t => t.Query, t => QueryLogic.GetQueryEntity(PortfolioQuery.Domains));
    }
    public static void LoadWordTemplates()
    {
        using (var tr = new Transaction())
        {
            var protocol = new WordTemplateEntity
            {
                Name = "Protocol",
                Query = QueryLogic.GetQueryEntity(typeof(MeetingProtocolEntity)),
                Culture = CultureInfoLogic.GetCultureInfoEntity("de"),
                FileName = "@[Date:yyyyMMdd] @[Customer.InternalName] @[Domain] @[MeetingType] @[Entity.Version].docx",
                Model = WordModelLogic.WordModelTypeToEntity.Value.GetOrThrow(typeof(MeetingProtocolWordModel)),
                Template = new FileEntity()
                {
                    BinaryFile = File.ReadAllBytes(Path.Combine("WordTemplates", "ProtocolTemplate.docx")),
                    FileName = "ProtocolTemplate.docx",
                }.ToLiteFat(),
                Orders = [

                new QueryOrderEmbedded
                {
                    Token = new QueryTokenEmbedded("Entity.Participants.Element.RowOrder"),
                    OrderType = OrderType.Ascending,
                },
                new QueryOrderEmbedded
                {
                    Token = new QueryTokenEmbedded("Entity.Agenda.Element.Index"),
                    OrderType = OrderType.Ascending,
                },
                new QueryOrderEmbedded
                {
                    Token = new QueryTokenEmbedded("Entity.ProtocolPoints.Element.AgendaIndex"),
                    OrderType = OrderType.Ascending,
                },
                new QueryOrderEmbedded
                {
                    Token = new QueryTokenEmbedded("Entity.ProtocolPoints.Element.SubIndex"),
                    OrderType = OrderType.Ascending,
                }
                ],
                DisableAuthorization = true,
            }.ParseData().Save();

            var statusReport = new WordTemplateEntity
            {
                Name = "Status Report",
                Query = QueryLogic.GetQueryEntity(typeof(StatusReportEntity)),
                Culture = CultureInfoLogic.GetCultureInfoEntity("de"),
                FileName = "023_@[ReportDate:yyyyMMdd] PSR @[Domain].pptx",
                Template = new FileEntity()
                {
                    BinaryFile = File.ReadAllBytes(Path.Combine("WordTemplates", "Status report.pptx")),
                    FileName = "Status report.pptx",
                }.ToLiteFat(),
                WordTransformer = RRSPWordTransformer.UpdateProjectStatusReport,
                Orders =
                [
                    new QueryOrderEmbedded
                    {
                        Token = new QueryTokenEmbedded("Entity.NextImportantActivities.Element.TargetDate"),
                        OrderType = OrderType.Ascending,
                    }
                ],
                DisableAuthorization = true,
            }.ParseData().Save();

            var risk = new WordTemplateEntity
            {
                Name = "Risk",
                Query = QueryLogic.GetQueryEntity(typeof(ProjectEntity)),
                Culture = CultureInfoLogic.GetCultureInfoEntity("de"),
                FileName = "Risks of @[Name].docx",
                Template = new FileEntity()
                {
                    BinaryFile = File.ReadAllBytes(Path.Combine("WordTemplates", "Risk.docx")),
                    FileName = "Risk.docx",
                }.ToLiteFat(),
                WordTransformer = RRSPWordTransformer.InsertRiskCategoryTable,
                DisableAuthorization = true,
            }.Save();

            var riskPercent = new WordTemplateEntity
            {
                Name = "Risk Percent",
                Query = QueryLogic.GetQueryEntity(typeof(ProjectEntity)),
                Culture = CultureInfoLogic.GetCultureInfoEntity("de"),
                FileName = "Risks of @[Name].docx",
                Template = new FileEntity()
                {
                    BinaryFile = File.ReadAllBytes(Path.Combine("WordTemplates", "Risk percentage.docx")),
                    FileName = "Risk percentage.docx",
                }.ToLiteFat(),
                WordTransformer = RRSPWordTransformer.InsertRiskCategoryTable,
                DisableAuthorization = true,
            }.Save();

            var contacts = new WordTemplateEntity
            {
                Name = "Project Contacts",
                Query = QueryLogic.GetQueryEntity(typeof(ProjectEntity)),
                Culture = CultureInfoLogic.GetCultureInfoEntity("de"),
                FileName = "@[Name] contact list.docx",
                Template = new FileEntity()
                {
                    BinaryFile = File.ReadAllBytes(Path.Combine("WordTemplates", "ProjectContacts.docx")),
                    FileName = "ProjectContacts.docx",
                }.ToLiteFat(),
                Orders =
                [
                    new QueryOrderEmbedded
                    {
                        Token = new QueryTokenEmbedded("Entity.InternalContacts.Element.Name"),
                        OrderType = OrderType.Ascending,
                    },
                    new QueryOrderEmbedded
                    {
                        Token = new QueryTokenEmbedded("Entity.CustomerContacts.Element.Name"),
                        OrderType = OrderType.Ascending,
                    },
                    new QueryOrderEmbedded
                    {
                        Token = new QueryTokenEmbedded("Entity.SupplierContacts.Element.Name"),
                        OrderType = OrderType.Ascending,
                    },
                ],
                DisableAuthorization = true,
            }.ParseData().Save();

            var charter = new WordTemplateEntity
            {
                Name = "Charter",
                Query = QueryLogic.GetQueryEntity(typeof(CharterEntity)),
                Culture = CultureInfoLogic.GetCultureInfoEntity("de"),
                Model = WordModelLogic.WordModelTypeToEntity.Value.GetOrThrow(typeof(ProjectCharterWordModel)),
                FileName = "Charter - @[Domain.Name] Version @[Entity.Baselines.Count].docx",
                Template = new FileEntity()
                {
                    BinaryFile = File.ReadAllBytes(Path.Combine("WordTemplates", "Charter.docx")),
                    FileName = "Charter.docx",
                }.ToLiteFat(),
                DisableAuthorization = true,
            }.Save();

            var changeRequest = new WordTemplateEntity
            {
                Name = "Project change request",
                Query = QueryLogic.GetQueryEntity(typeof(ChangeRequestEntity)),
                Culture = CultureInfoLogic.GetCultureInfoEntity("de"),
                Model = WordModelLogic.WordModelTypeToEntity.Value.GetOrThrow(typeof(ChangeRequestWordModel)),
                FileName = "@[Domain.(Project)] @[UniqID].docx",
                Template = new FileEntity()
                {
                    BinaryFile = File.ReadAllBytes(Path.Combine("WordTemplates", "ChangeRequest.docx")),
                    FileName = "ChangeRequest.docx",
                }.ToLiteFat(),
                DisableAuthorization = true,
            }.Save();

            var businessCase = new WordTemplateEntity
            {
                Name = "Business case",
                Query = QueryLogic.GetQueryEntity(typeof(BusinessCaseEntity)),
                Culture = CultureInfoLogic.GetCultureInfoEntity("de"),
                Model = WordModelLogic.WordModelTypeToEntity.Value.GetOrThrow(typeof(BusinessCaseWordModel)),
                FileName = "Business Case - @[Domain.Name] Version @[Entity.Baselines.Count].docx",
                Template = new FileEntity()
                {
                    BinaryFile = File.ReadAllBytes(Path.Combine("WordTemplates", "Business Case.docx")),
                    FileName = "Business Case.docx",
                }.ToLiteFat(),
                DisableAuthorization = true,
            }.Save();

            var workpackages = new WordTemplateEntity
            {
                Name = "Work package",
                Query = QueryLogic.GetQueryEntity(typeof(ProjectWorkPackageEntity)),
                Culture = CultureInfoLogic.GetCultureInfoEntity("de"),
                Model = WordModelLogic.WordModelTypeToEntity.Value.GetOrThrow(typeof(WorkPackageWordModel)),
                FileName = "@[ProjectPlan.Project] @[UniqID].docx",
                Template = new FileEntity()
                {
                    BinaryFile = File.ReadAllBytes(Path.Combine("WordTemplates", "Work Package.docx")),
                    FileName = "Work Package.docx",
                }.ToLiteFat(),
                DisableAuthorization = true,
            }.Save();

            new WordTemplateEntity
            {
                Name = "Initiation request",
                Query = QueryLogic.GetQueryEntity(typeof(InitiationRequestEntity)),
                Culture = CultureInfoLogic.GetCultureInfoEntity("de"),
                Model = WordModelLogic.WordModelTypeToEntity.Value.GetOrThrow(typeof(ProjectInitiationRequestWordModel)),
                FileName = "@[Title].docx",
                Template = new FileEntity()
                {
                    BinaryFile = File.ReadAllBytes(Path.Combine("WordTemplates", "Initiation request.docx")),
                    FileName = "Initiation request.docx",
                }.ToLiteFat(),
                DisableAuthorization = true,
            }.Save();

            var configuration = Starter.Configuration.Value;
            configuration.InDB().UnsafeUpdate()
                .Set(c => c.BusinessCaseReportTemplate, c => businessCase.ToLite())
                .Set(c => c.ChangeRequestReportTemplate, c => changeRequest.ToLite())
                .Set(c => c.ContactPersonReportTemplate, c => contacts.ToLite())
                .Set(c => c.MeetingProtocolReportTemplate, c => protocol.ToLite())
                .Set(c => c.ProjectCharterReportTemplate, c => charter.ToLite())
                .Set(c => c.RiskManagementReportTemplate, c => risk.ToLite())
                .Set(c => c.RiskManagementPercentReportTemplate, c => riskPercent.ToLite())
                .Set(c => c.StatusReportReportTemplate, c => statusReport.ToLite())
                .Set(c => c.WorkPackageReportTemplate, c => workpackages.ToLite())
                .Execute();
            tr.Commit();
        }
    }
}
