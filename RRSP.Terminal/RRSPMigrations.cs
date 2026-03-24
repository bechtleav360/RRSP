using Aspose.Pdf.Operators;
using Aspose.Words.XAttr;
using Meros.PlanningProject.BusinessCase;
using Meros.PlanningProject.PSC;
using Meros.PlanningProject.TaskManagement;
using Meros.PlanningProject.WorkPackage;
using Meros.PortfolioExt;
using Meros.PortfolioExt.InitiationRequest;
using Meros.PortfolioExt.Responsibilities;
using Meros.Project;
using Meros.Project.Portfolio;
using Meros.Protocol;
using Meros.Protocol.Decision;
using Meros.Risk;
using Meros.StatusReport;
using Meros.Tasks;
using Microsoft.Graph.Models;
using RRSP.Globals;
using RRSP.Project;
using RRSP.Test.Environment;
using Signum.Alerts;
using Signum.Authorization;
using Signum.Authorization.AuthToken;
using Signum.Basics;
using Signum.DiffLog;
using Signum.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Sync;
using Signum.Files;
using Signum.Mailing;
using Signum.Migrations;
using Signum.Security;
using Signum.Translation.Instances;
using Signum.UserAssets;
using Signum.UserAssets.Queries;
using Signum.Word;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace RRSP.Terminal;


class RRSPMigrations
{
    public static void CSharpMigrations(bool autoRun)
    {
        Schema.Current.Initialize();

        SafeConsole.WriteLineColor(ConsoleColor.Magenta, "Hi there!");

        OperationLogic.AllowSaveGlobally = true;

        new CSharpMigrationRunner
        {
            CreateRoles,
            CreateAuthRules,
            CreateSystemUser,
            CreateApplicationConfiguration,
            CreateLabelTypes,
            PortfolioGlobalConfigLoader.PortfolioGlobalConfiguration,
            CommonMigrations.CreatePlanLevelColor,
            CommonMigrations.CreateEstimationSizes,
            CommonMigrations.ImportTranslations,
            ImportUserAssets,
            WordTempleLoader.LoadWordTemplates,
            CommonMigrations.CreateProtocolMasterData,
            CommonMigrations.CreateRiskCategory,
            ProjectClassificationLogic.CreateProjectClassificationItems,
            CommonMigrations.CreateArtifacts,
            CommonMigrations.MapProjectMemberRole,
            //CommonMigrations.MoveGermanTranslations,
            CommonMigrations.LoadDomainParent,
            CommonMigrations.UpdatePlanLevelColor,
            CleanAPIKeysRRSP,
            WordTempleLoader.UpdateDomainRiskTemplate,
            UpdateProjectCharterTemplate,
            UpdateStatusReportTemplate,
            UpdateMeetingProtocolTemplate,
            UpdateParentDomain,
            UpdateProjectContacts,
            AddProtocolPointTypeProblem,
            ImportUserDashboard,
            AddDomainResponsibilities,
            FixProjectClassificationItem,
            SyncRoles3,
            CreateTasksForProtocolPoints,
            FixProtocolWordTemplate,
            FixColumns,
            FixProtocolPointType,
            AddInProgressColumn,
            AddProgramAndPortfolioResponsibilities,
            CreateMilestonesFromStatusReports,
            FillAlertTargetToString,
            UpdateInitRequestApprovedOn,
            UpdateProtocolPointTypeAndDecision,
            CreateDecisionAndProblemFromProtocolPoints,
            ImportAuthRules9, //version 0.25
            ImportToolbar7, //version 0.25
            ImportDashboards2, //version 0.25
        }.Run(autoRun);
    } //CSharpMigrations

    public static void CreateDecisionAndProblemFromProtocolPoints()
    {
        using (var tr = new Transaction())
        using (AuthLogic.UnsafeUserSession("System"))
        {
            var problems = Database.Query<ProtocolPointEntity>().Where(pp => pp.Type.Mixin<ProtocolPointTypeRiskMixin>().CreateRiskWithType == RiskType.Problem)
                .ToList()
                .Select(pp => pp.ConstructFrom(RiskOperation.CreateRiskFromProtocolPoint)).ToList();
            problems.SaveList();

            var decisions = Database.Query<ProtocolPointEntity>().Where(pp => pp.Type.Mixin<ProtocolPointTypeDecisionMixin>().CreateDecision)
                .ToList()
                .Select(pp => pp.ConstructFrom(DecisionOperation.CreateDecisionFromProtocolPoint)).ToList();
            decisions.SaveList();

            tr.Commit();
        }
    }

    public static void UpdateProtocolPointTypeAndDecision()
    {
        Database.Query<ProtocolPointDecisionEntity>().Where(ppd => ppd.Name == "Entschieden").UnsafeUpdate(ppd => ppd.Mixin<ProtocolPointDecisionDecisionMixin>().Status, ppd => DecisionStatus.Confirmed);
        Database.Query<ProtocolPointDecisionEntity>().Where(ppd => ppd.Name != "Entschieden").UnsafeUpdate(ppd => ppd.Mixin<ProtocolPointDecisionDecisionMixin>().Status, ppd => DecisionStatus.Pending);

        Database.Query<ProtocolPointTypeEntity>().Where(ppt => ppt.Name == "Entscheidung").UnsafeUpdate(ppt => ppt.Mixin<ProtocolPointTypeDecisionMixin>().CreateDecision, ppt => true);
        Database.Query<ProtocolPointTypeEntity>().Where(ppt => ppt.Name == "Problem").UnsafeUpdate(ppt => ppt.Mixin<ProtocolPointTypeRiskMixin>().CreateRiskWithType, ppt => RiskType.Problem);
        new ProtocolPointTypeEntity
        {
            Name = "Risiko",
            Abbreviation = "R",
        }.SetMixin((ProtocolPointTypeRiskMixin r) => r.CreateRiskWithType, RiskType.Risk).Save();
    }

    public static void UpdateInitRequestApprovedOn()
    {
        var list = Database.Query<InitiationRequestEntity>().Where(ir => ir.State == InitiationRequestState.Approved && ir.ApprovedOn == null).ToList();

        foreach (var item in list)
        {
            var approveDate = Database.Query<OperationLogEntity>()
                .Where(a => a.Target.Is(item) && a.Operation.Key == InitiationRequestOperation.Approve.Symbol.Key)
                .OrderByDescending(a => a.End)
                .Select(a => (DateTime?)a.End)
                .FirstOrDefault();
            item.InDB().UnsafeUpdate(i => i.ApprovedOn, i => approveDate);
        }
    }
    public static void FillAlertTargetToString()
    {
        using (var tr = new Transaction())
        {
            var alertsToUpdate = Database.Query<AlertEntity>()
                .Where(a => a.Target != null && a.TargetToString == null)
                .ToList();

            Console.WriteLine($"Found {alertsToUpdate.Count} alerts to update");

            int count = 0;
            foreach (var alert in alertsToUpdate)
            {
                try
                {
                    if (alert.Target != null && alert.Target.Exists())
                    {
                        alert.TargetToString = alert.Target.ToString()?.Truncate(200);
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating alert {alert.Id}: {ex.Message}");
                }
            }

            if (count > 0)
            {
                using (OperationLogic.AllowSave<AlertEntity>())
                {
                    alertsToUpdate.SaveList();
                }
                Console.WriteLine($"Successfully updated {count} alerts with TargetToString");
            }

            tr.Commit();
        }
    }

    public static void CreateMilestonesFromStatusReports()
    {
        using (var tr = new Transaction())
        {
            var milestones = Database.Query<MilestoneEntity>().ToList().GroupBy(m => new { m.Domain, m.Name, m.TargetDate }).ToDictionary(gr => gr.Key, gr => gr.First());
            foreach (var sr in Database.Query<StatusReportEntity>().Where(sr => sr.IsLast).ToList())
            {
                foreach (var m in sr.RecentMilestones)
                {
                    if (m.LinkedMilestone != null)
                        continue;
                    var key = new { sr.Domain, Name = m.Milestone, m.TargetDate };
                    if (!milestones.TryGetValue(key, out var milestone))
                    {
                        milestone = new MilestoneEntity
                        {
                            Domain = sr.Domain,
                            Name = m.Milestone,
                            TargetDate = m.TargetDate,
                        }.Save();
                        milestones[key] = milestone;
                    }
                    m.LinkedMilestone = milestone.ToLite();
                }
            }
            tr.Commit();
        }
    }

    public static void AddInProgressColumn()
    {
        Database.Query<ColumnEntity>().Where(c => c.Name.ToLower() == "in progress" && c.TaskState != TaskState.InProgress).UnsafeUpdate(c => c.TaskState, c => TaskState.InProgress);
        Database.Query<BoardEntity>().Where(b => !b.Columns.Any(c => c.TaskState == TaskState.InProgress)).Select(b => new ColumnEntity
        {
            Board = b.ToLite(),
            Name = "In Progress",
            TaskState = TaskState.InProgress,
        }).ToList().SaveList();
    }

    public static void FixProtocolPointType()
    {
        Database.Query<ProtocolPointTypeEntity>().Where(ppt => ppt.Name == "Beschluss").UnsafeUpdate()
            .Set(ppt => ppt.Name, c => "Entscheidung")
            .Set(ppt => ppt.Abbreviation, c => "E")
            .Execute();
    }

    public static void FixColumns()
    {
        Database.Query<ColumnEntity>().Where(c => c.TaskState == TaskState.ArchivedDone).UnsafeUpdate(c => c.TaskState, c => TaskState.Open);
        Database.Query<ColumnEntity>().Where(c => c.TaskState == TaskState.Rejected).UnsafeUpdate(c => c.TaskState, c => TaskState.Done);
    }

    public static void FixProtocolWordTemplate()
    {
        var list = Database.Query<WordTemplateEntity>().Where(wt => wt.Query.Is(QueryLogic.GetQueryEntity(typeof(MeetingProtocolEntity)))).ToList();
        foreach (var wt in list)
        {
            try
            {
                wt.Orders.Clear();
                wt.Orders = [
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
                }];
                wt.ParseData().Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fixing WordTemplate {wt.Id} {wt.Name}: {ex.Message}");
            }
        }
    }

    public static void CreateTasksForProtocolPoints()
    {
        using (var tr = new Transaction())
        using (AuthLogic.UnsafeUserSession("System"))
        {
            Database.Query<ProtocolPointTypeEntity>().Where(ppt => ppt.Name == "Aufgabe").UnsafeUpdate(ppt => ppt.Mixin<ProtocolPointTypeTaskMixin>().CreateTask, ppt => true);

            try
            {
                var results = Database.Query<ProtocolPointEntity>()
                    .Where(pp => !pp.Following().Any() &&
                                 pp.Type.Mixin<ProtocolPointTypeTaskMixin>().CreateTask &&
                                 pp.ChainTask() == null)
                    .ToList();
            }
            catch (FieldReaderException ex)
            {
                Console.WriteLine($"Column: {ex.ColumnName}, Ordinal: {ex.Ordinal}");
                throw;
            }

            foreach (var pp in Database.Query<ProtocolPointEntity>().Where(pp => !pp.Following().Any() && pp.Type.Mixin<ProtocolPointTypeTaskMixin>().CreateTask && pp.ChainTask() == null).ToList())
            {
                var state = pp.DoneOn.HasValue ? TaskState.Done : TaskManagementLogic.ProtocolPointStateToTaskState(pp.State);
                if(state != TaskState.Rejected)
                {
                    var title = ExcelHelpers.ConvertHtmlToPlainText(pp.Description).Split('\n').First().Trim().Etc(990);
                    var assignedTo = pp.Responsible is Lite<MemberEntity> m ? m.InDB(a => a.User) :
                            Database.Query<OperationLogEntity>()
                            .Where(a => a.Target.Is((Lite<IEntity>?)pp.MeetingProtocol ?? pp.ToLite()))
                            .OrderBy(a => a.Start)
                            .Select(a => (Lite<UserEntity>)a.User)
                            .FirstOrDefault();

                    pp.CreateTask(domain: pp.Domain, title: title, description: pp.Description, assignedTo: assignedTo, dueDate: pp.Deadline.ToDateTime(DateTimeKind.Utc),
                        state: state);
                }
            }

            foreach (var wp in Database.Query<ProjectWorkPackageEntity>().Where(wp => wp.Task() == null).ToList())
            {
                wp.CreateTask(domain: wp.ProjectPlan.InDB(pp => pp.Project), title: wp.Title, description: wp.Description, dueDate: wp.EndDate,
                    state: TaskManagementLogic.WorkPackageProgressToTaskState(wp.Progress));
            }
            tr.Commit();
        }
    }


    public static void FixProjectClassificationItem()
    {
        var pci = Database.Query<ProjectClassificationItemEntity>().Where(pci => pci.Number == 7).Single();
        pci.Answers.First().Title = "< 500.000 €";
        pci.Save();
    }

    public static void AddDomainResponsibilities()
    {
        using (var tr = new Transaction())
        {
            // =====================
            // PROJECT
            // =====================
            var psc = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Project,
                Name = "Projektlenkungsausschuss",
                ShortName = "PSC",
                Description = "Projektsteuerung & Freigaben (Vorsitz: PO)",
                DutyDescription = "Genehmigt Meilensteine und löst Eskalationen",
                Level = DomainResponsibilityLevel.SteeringLevel,
                IconName = "check-circle"
            }.Save();
            var po = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Project,
                Name = "Projekteigner/in (Project Owner)",
                ShortName = "PO",
                Description = "Business Owner & Projektsponsor",
                DutyDescription = "Berichtet an PSC, definiert Geschäftsziele",
                Level = DomainResponsibilityLevel.ControlLevel,
                OnlyOneMember = true,
                IconName = "check-square"
            }.Save();
            var sp = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Project,
                Name = "Lösungsanbieter/in (Solution Provider)",
                ShortName = "SP",
                Description = "Verantwortlich für Liefergegenstände",
                DutyDescription = "Berichtet an PSC, stellt Ressourcen bereit",
                Level = DomainResponsibilityLevel.ControlLevel,
                OnlyOneMember = true,
                IconName = "check-square"
            }.Save();

            var bm = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Project,
                Name = "Anforderungsmanager/in",
                ShortName = "BM",
                Description = "Geschäftsanforderungen (Beauftragt durch PO)",
                DutyDescription = "Arbeitet eng mit PM zusammen",
                Level = DomainResponsibilityLevel.AdministrativeLevel,
                OnlyOneMember = true,
                IconName = "user"
            }.Save();
            var pm = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Project,
                Name = "Projektleitung (Project Manager)",
                ShortName = "PM",
                Description = "Tagesaktuelle Projektführung",
                DutyDescription = "Berichtet an PSC, eskaliert Probleme",
                Level = DomainResponsibilityLevel.AdministrativeLevel,
                OnlyOneMember = true,
                IconName = "user"
            }.Save();
            var big = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Project,
                Name = "Anforderungsteam",
                ShortName = "BIG",
                Description = "Business Implementation",
                DutyDescription = "Plant & implementiert geschäftliche Änderungen",
                Level = DomainResponsibilityLevel.ExecutionLevel,
                IconName = "users"
            }.Save();
            var pct = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Project,
                Name = "Projektkernteam",
                ShortName = "PCT",
                Description = "Projektdurchführung",
                DutyDescription = "Führt Projektarbeit aus, Daily Meetings",
                Level = DomainResponsibilityLevel.ExecutionLevel,
                IconName = "users"
            }.Save();

            var pst = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Project,
                Name = "Projektunterstützungsteam",
                ShortName = "PST",
                Description = "Unterstützende Funktionen",
                DutyDescription = "Unterstützt PM mit Admin & Qualitätssicherung",
                Level = DomainResponsibilityLevel.ExecutionLevel,
                IconName = "gear"
            }.Save();

            var en = CultureInfoLogic.GetCultureInfoEntity("en");

            // PSC
            psc.SaveTranslation(en, e => e.Name, "Project Steering Committee");
            psc.SaveTranslation(en, e => e.Description, "Project Management & Approvals (Chair: PO)");
            psc.SaveTranslation(en, e => e.DutyDescription, "Approves milestones, resolves escalations");


            // PO
            po.SaveTranslation(en, e => e.Name, "Project Owner");
            po.SaveTranslation(en, e => e.Description, "Business Owner & Project Sponsor");
            po.SaveTranslation(en, e => e.DutyDescription, "Reports to PSC, defines business objectives");


            // SP
            sp.SaveTranslation(en, e => e.Name, "Solution Provider");
            sp.SaveTranslation(en, e => e.Description, "Responsible for delivered items");
            sp.SaveTranslation(en, e => e.DutyDescription, "Reports to PSC, provides resources");


            // BM
            bm.SaveTranslation(en, e => e.Name, "Requirements Manager");
            bm.SaveTranslation(en, e => e.Description, "Business requirements (commissioned by PO)");
            bm.SaveTranslation(en, e => e.DutyDescription, "Works closely with PM");


            // PM
            pm.SaveTranslation(en, e => e.Name, "Project Manager");
            pm.SaveTranslation(en, e => e.Description, "Daily project management");
            pm.SaveTranslation(en, e => e.DutyDescription, "Reported to PSC, escalates problems");


            // BIG
            big.SaveTranslation(en, e => e.Name, "Requirements Team");
            big.SaveTranslation(en, e => e.Description, "Business Implementation");
            big.SaveTranslation(en, e => e.DutyDescription, "Plans and implements business changes");


            // PCT
            pct.SaveTranslation(en, e => e.Name, "Project Core Team");
            pct.SaveTranslation(en, e => e.Description, "Project implementation");
            pct.SaveTranslation(en, e => e.DutyDescription, "Carry out project work, daily meetings");


            // PST
            pst.SaveTranslation(en, e => e.Name, "Project Support Team");
            pst.SaveTranslation(en, e => e.Description, "Supporting functions");
            pst.SaveTranslation(en, e => e.DutyDescription, "Supports PM with Admin & Quality Assurance");

            tr.Commit();
        }
    }

    public static void AddProgramAndPortfolioResponsibilities()
    {
        using (var tr = new Transaction())
        {
            var en = CultureInfoLogic.GetCultureInfoEntity("en");

            // =====================
            // PROGRAM
            // =====================

            var pscPrg = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Program,
                Name = "Programmlenkungsausschuss",
                ShortName = "PRG-PSC",
                Description = "Programmsteuerung & strategische Freigaben",
                DutyDescription = "Genehmigt Programm-Meilensteine und Eskalationen",
                Level = DomainResponsibilityLevel.SteeringLevel,
                IconName = "check-circle"
            }.Save();

            var poPrg = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Program,
                Name = "Programmeigner/in (Program Owner)",
                ShortName = "PRG-PO",
                Description = "Business Owner & Programmsponsor",
                DutyDescription = "Verantwortlich für Programmziele und Nutzen",
                Level = DomainResponsibilityLevel.ControlLevel,
                OnlyOneMember = true,
                IconName = "check-square"
            }.Save();

            var spPrg = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Program,
                Name = "Programmlösungsanbieter/in",
                ShortName = "PRG-SP",
                Description = "Verantwortlich für Programmlieferungen",
                DutyDescription = "Stellt Ressourcen projektübergreifend bereit",
                Level = DomainResponsibilityLevel.ControlLevel,
                OnlyOneMember = true,
                IconName = "check-square"
            }.Save();

            var bmPrg = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Program,
                Name = "Programm-Anforderungsmanager/in",
                ShortName = "PRG-BM",
                Description = "Programmweite Anforderungen",
                DutyDescription = "Synchronisiert Anforderungen zwischen Projekten",
                Level = DomainResponsibilityLevel.AdministrativeLevel,
                OnlyOneMember = true,
                IconName = "user"
            }.Save();

            var pmPrg = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Program,
                Name = "Programmleitung (Program Manager)",
                ShortName = "PRG-PM",
                Description = "Operative Programmführung",
                DutyDescription = "Koordiniert Projekte und eskaliert Risiken",
                Level = DomainResponsibilityLevel.AdministrativeLevel,
                OnlyOneMember = true,
                IconName = "user"
            }.Save();

            var bigPrg = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Program,
                Name = "Programm-Implementierungsteam",
                ShortName = "PRG-BIG",
                Description = "Programmweite Umsetzung",
                DutyDescription = "Implementiert programmübergreifende Änderungen",
                Level = DomainResponsibilityLevel.ExecutionLevel,
                IconName = "users"
            }.Save();

            var pctPrg = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Program,
                Name = "Programm-Kernteam",
                ShortName = "PRG-PCT",
                Description = "Programmdurchführung",
                DutyDescription = "Koordiniert operative Programmaktivitäten",
                Level = DomainResponsibilityLevel.ExecutionLevel,
                IconName = "users"
            }.Save();

            var pstPrg = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Program,
                Name = "Programmunterstützungsteam",
                ShortName = "PRG-PST",
                Description = "Unterstützende Programmfunktionen",
                DutyDescription = "Unterstützt Programmleitung administrativ",
                Level = DomainResponsibilityLevel.ExecutionLevel,
                IconName = "gear"
            }.Save();

            pscPrg.SaveTranslation(en, e => e.Name, "Program Steering Committee");
            pscPrg.SaveTranslation(en, e => e.Description, "Program governance & strategic approvals");
            pscPrg.SaveTranslation(en, e => e.DutyDescription, "Approves program milestones and escalations");

            poPrg.SaveTranslation(en, e => e.Name, "Program Owner");
            poPrg.SaveTranslation(en, e => e.Description, "Business Owner & Program Sponsor");
            poPrg.SaveTranslation(en, e => e.DutyDescription, "Responsible for program goals and benefits");

            spPrg.SaveTranslation(en, e => e.Name, "Program Solution Provider");
            spPrg.SaveTranslation(en, e => e.Description, "Responsible for program deliveries");
            spPrg.SaveTranslation(en, e => e.DutyDescription, "Provides resources across projects");

            bmPrg.SaveTranslation(en, e => e.Name, "Program Requirements Manager");
            bmPrg.SaveTranslation(en, e => e.Description, "Program-wide requirements");
            bmPrg.SaveTranslation(en, e => e.DutyDescription, "Synchronizes requirements across projects");

            pmPrg.SaveTranslation(en, e => e.Name, "Program Manager");
            pmPrg.SaveTranslation(en, e => e.Description, "Operational program management");
            pmPrg.SaveTranslation(en, e => e.DutyDescription, "Coordinates projects and escalates risks");

            bigPrg.SaveTranslation(en, e => e.Name, "Program Implementation Team");
            bigPrg.SaveTranslation(en, e => e.Description, "Program-wide implementation");
            bigPrg.SaveTranslation(en, e => e.DutyDescription, "Implements cross-project changes");

            pctPrg.SaveTranslation(en, e => e.Name, "Program Core Team");
            pctPrg.SaveTranslation(en, e => e.Description, "Program execution");
            pctPrg.SaveTranslation(en, e => e.DutyDescription, "Coordinates operational program activities");

            pstPrg.SaveTranslation(en, e => e.Name, "Program Support Team");
            pstPrg.SaveTranslation(en, e => e.Description, "Supporting program functions");
            pstPrg.SaveTranslation(en, e => e.DutyDescription, "Supports program management administratively");

            // =====================
            // PORTFOLIO
            // =====================

            var pscPrt = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Portfolio,
                Name = "Portfoliolenkungsausschuss",
                ShortName = "PRT-PSC",
                Description = "Portfoliosteuerung & strategische Entscheidungen",
                DutyDescription = "Priorisiert Initiativen und Investitionen",
                Level = DomainResponsibilityLevel.SteeringLevel,
                IconName = "check-circle"
            }.Save();

            var poPrt = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Portfolio,
                Name = "Portfolioverantwortliche/r (Portfolio Owner)",
                ShortName = "PRT-PO",
                Description = "Gesamtverantwortung für das Portfolio",
                DutyDescription = "Definiert strategische Ausrichtung und Prioritäten",
                Level = DomainResponsibilityLevel.ControlLevel,
                OnlyOneMember = true,
                IconName = "check-square"
            }.Save();

            var spPrt = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Portfolio,
                Name = "Portfolio-Lösungsanbieter/in",
                ShortName = "PRT-SP",
                Description = "Ressourcen- & Lieferverantwortung",
                DutyDescription = "Stellt Lieferfähigkeit über Programme sicher",
                Level = DomainResponsibilityLevel.ControlLevel,
                OnlyOneMember = true,
                IconName = "check-square"
            }.Save();

            var bmPrt = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Portfolio,
                Name = "Portfolio-Anforderungsmanager/in",
                ShortName = "PRT-BM",
                Description = "Strategische Anforderungen",
                DutyDescription = "Priorisiert Anforderungen auf Portfolioebene",
                Level = DomainResponsibilityLevel.AdministrativeLevel,
                OnlyOneMember = true,
                IconName = "user"
            }.Save();

            var pmPrt = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Portfolio,
                Name = "Portfoliomanager/in",
                ShortName = "PRT-PM",
                Description = "Operatives Portfoliomanagement",
                DutyDescription = "Überwacht Portfolio-Performance und Nutzen",
                Level = DomainResponsibilityLevel.AdministrativeLevel,
                OnlyOneMember = true,
                IconName = "user"
            }.Save();

            var bigPrt = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Portfolio,
                Name = "Portfolio-Implementierungsteam",
                ShortName = "PRT-BIG",
                Description = "Strategische Umsetzung",
                DutyDescription = "Unterstützt Umsetzung von Portfolioinitiativen",
                Level = DomainResponsibilityLevel.ExecutionLevel,
                IconName = "users"
            }.Save();

            var pctPrt = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Portfolio,
                Name = "Portfolio-Kernteam",
                ShortName = "PRT-PCT",
                Description = "Portfolio-Koordination",
                DutyDescription = "Koordiniert Programme und Initiativen",
                Level = DomainResponsibilityLevel.ExecutionLevel,
                IconName = "users"
            }.Save();

            var pstPrt = new DomainResponsibilityEntity
            {
                DomainLevel = DomainLevel.Portfolio,
                Name = "Portfolio-Unterstützungsteam",
                ShortName = "PRT-PST",
                Description = "Unterstützende Portfoliofunktionen",
                DutyDescription = "Unterstützt Portfoliomanagement administrativ",
                Level = DomainResponsibilityLevel.ExecutionLevel,
                IconName = "gear"
            }.Save();

            pscPrt.SaveTranslation(en, e => e.Name, "Portfolio Steering Committee");
            pscPrt.SaveTranslation(en, e => e.Description, "Portfolio governance & strategic decisions");
            pscPrt.SaveTranslation(en, e => e.DutyDescription, "Prioritizes initiatives and investments");

            poPrt.SaveTranslation(en, e => e.Name, "Portfolio Owner");
            poPrt.SaveTranslation(en, e => e.Description, "Overall responsibility for the portfolio");
            poPrt.SaveTranslation(en, e => e.DutyDescription, "Defines strategic direction and priorities");

            spPrt.SaveTranslation(en, e => e.Name, "Portfolio Solution Provider");
            spPrt.SaveTranslation(en, e => e.Description, "Resource and delivery responsibility");
            spPrt.SaveTranslation(en, e => e.DutyDescription, "Ensures delivery capability across programs");

            bmPrt.SaveTranslation(en, e => e.Name, "Portfolio Requirements Manager");
            bmPrt.SaveTranslation(en, e => e.Description, "Strategic requirements");
            bmPrt.SaveTranslation(en, e => e.DutyDescription, "Prioritizes requirements at portfolio level");

            pmPrt.SaveTranslation(en, e => e.Name, "Portfolio Manager");
            pmPrt.SaveTranslation(en, e => e.Description, "Operational portfolio management");
            pmPrt.SaveTranslation(en, e => e.DutyDescription, "Monitors portfolio performance and benefits");

            bigPrt.SaveTranslation(en, e => e.Name, "Portfolio Implementation Team");
            bigPrt.SaveTranslation(en, e => e.Description, "Strategic implementation");
            bigPrt.SaveTranslation(en, e => e.DutyDescription, "Supports execution of portfolio initiatives");

            pctPrt.SaveTranslation(en, e => e.Name, "Portfolio Core Team");
            pctPrt.SaveTranslation(en, e => e.Description, "Portfolio coordination");
            pctPrt.SaveTranslation(en, e => e.DutyDescription, "Coordinates programs and initiatives");

            pstPrt.SaveTranslation(en, e => e.Name, "Portfolio Support Team");
            pstPrt.SaveTranslation(en, e => e.Description, "Supporting portfolio functions");
            pstPrt.SaveTranslation(en, e => e.DutyDescription, "Supports portfolio management administratively");

            tr.Commit();
        }
    }

    public static void ImportUserDashboard()
    {
        using (UserHolder.UserSession(AuthLogic.SystemUser!))
            UserAssetsImporter.ImportAll(File.ReadAllBytes(@"DashboardEntity5.xml"));
    }

    static void AddProtocolPointTypeProblem()
    {
        var charter = new ProtocolPointTypeEntity
        {
            Name = "Problem",
            Abbreviation = "P",
        }.Save();
    }

    static void UpdateProjectContacts()
    {
        var charter = Database.Query<WordTemplateEntity>().Single(a => a.Query.Is(QueryLogic.GetQueryEntity(typeof(ProjectEntity))) && a.Name.Contains("Project Contacts"));
        charter.Template = new FileEntity(Path.Combine("WordTemplates", "ProjectContacts.docx")).ToLiteFat();
        charter.Save();
    }

    static void UpdateParentDomain()
    {
        Database.Query<ProjectEntity>().Where(p => p.Parent() == null && p.Mixin<ProjectPortfolioMixin>().ParentDomain != null)
            .UnsafeInsert(p => new DomainParentEntity
            {
                Child = p.ToLite(),
                Parent = p.Mixin<ProjectPortfolioMixin>().ParentDomain!,
            }, message: "auto");
    }

    static void UpdateMeetingProtocolTemplate()
    {
        var charter = Database.Query<WordTemplateEntity>().Single(a => a.Query.Is(QueryLogic.GetQueryEntity(typeof(MeetingProtocolEntity))));
        charter.Template = new FileEntity(Path.Combine("WordTemplates", "ProtocolTemplate.docx")).ToLiteFat();
        charter.FileName = "@[Date:yyyyMMdd] @[Customer.InternalName] @[Domain] @[MeetingType] @[Entity.Version].docx";
        charter.Save();
    }


    static void UpdateProjectCharterTemplate()
    {
        var charter = Database.Query<WordTemplateEntity>().Single(a => a.Query.Is(QueryLogic.GetQueryEntity(typeof(CharterEntity))));
        charter.Template = new FileEntity(Path.Combine("WordTemplates", "Charter.docx")).ToLiteFat();
        charter.Save();
    }

    static void UpdateStatusReportTemplate()
    {
        var statusReport = Database.Query<WordTemplateEntity>().Single(a => a.Query.Is(QueryLogic.GetQueryEntity(typeof(StatusReportEntity))));
        statusReport.Template = new FileEntity(Path.Combine("WordTemplates", "Status report.pptx")).ToLiteFat();
        statusReport.Save();
    }

    public static void SyncRoles3()
    {
        using (Transaction tr = new Transaction())
        {
            var authRules = XDocument.Load("AuthRules.xml");
            AuthLogic.SynchronizeRoles(authRules, interactive: false, autoReplacement: ctx => new Replacements.Selection(ctx.OldValue, null));

            tr.Commit();
        }
    }

    public static void ImportAuthRules9()
    {
        using (Transaction tr = new Transaction())
        {
            var authRules = XDocument.Load("AuthRules.xml");
            AuthLogic.ImportAuthRules(authRules, interactive: false);

            tr.Commit();
        }
    }

    public static void ImportToolbar7()
    {
        using (UserHolder.UserSession(AuthLogic.SystemUser!))
            UserAssetsImporter.ImportAll("ToolbarEntity1.xml");
    }

    public static void ImportDashboards2()
    {
        using (UserHolder.UserSession(AuthLogic.SystemUser!))
            UserAssetsImporter.ImportAll("Dashboards.xml");
    }

    public static void CleanAPIKeysRRSP()
    {
        using (var tr = new Transaction())
        {
            using (SystemTime.Override(new SystemTime.HistoryTable()))
                Database.Query<ApplicationConfigurationEntity>().UnsafeUpdate()
                        //.Set(a => a.ActiveDirectory.AzureAD, a => null)
                        .Set(a => a.Translation.AzureCognitiveServicesAPIKey, a => null)
                        .Set(a => a.Translation.DeepLAPIKey, a => null)
                        .Set(a => a.Translation.AzureCognitiveServicesRegion, a => null)
                        .Execute();

            var logs = Database.Query<OperationLogEntity>().Where(a => a.Operation.Is(ApplicationConfigurationOperation.Save.Symbol)).ToList();

            foreach (var l in logs)
            {
                Clean(l.Mixin<DiffLogMixin>().InitialState);
                Clean(l.Mixin<DiffLogMixin>().FinalState);
                l.Save();
            }

            void Clean(BigStringEmbedded bigStr)
            {
                bigStr.Text = bigStr.Text?.Lines().Where(a => !(a.Contains("ApplicationID") || a.Contains("DirectoryId") || a.Contains("AzureCognitiveServicesAPIKey") || a.Contains("DeepLAPIKey"))).ToString("\n");
            }

            tr.Commit();
        }

    }

    internal static void CreateLabelTypes()
    {
        using (OperationLogic.AllowSave<TaskLabelTypeEntity>())
        using (Transaction tr = new Transaction())
        {
            new TaskLabelTypeEntity { Name = "TFS-Type" }.Save();
            new TaskLabelTypeEntity { Name = "Area" }.Save();
            new TaskLabelTypeEntity { Name = "Iteration" }.Save();
            new TaskLabelTypeEntity { Name = "Priority" }.Save();
            new TaskLabelTypeEntity { Name = "Impact" }.Save();
            new TaskLabelTypeEntity { Name = "Urgency" }.Save();
            new TaskLabelTypeEntity { Name = "Billable" }.Save();
            new TaskLabelTypeEntity { Name = "Category" }.Save();
            tr.Commit();
        }
    }

    internal static void CreateRoles()
    {
        using (Transaction tr = new Transaction())
        {
            var authRules = XDocument.Load(@"AuthRules.xml");
            AuthLogic.LoadRoles(authRules);

            tr.Commit();
        }
    }

    internal static void CreateAuthRules()
    {
        using (Transaction tr = new Transaction())
        {
            var authRules = XDocument.Load("AuthRules.xml");
            AuthLogic.ImportAuthRules(authRules, interactive: false);

            tr.Commit();
        }
    }

    internal static void CreateSystemUser()
    {
        using (OperationLogic.AllowSave<UserEntity>())
        using (Transaction tr = new Transaction())
        {
            CreateUser("System", "System", "Robot", "Super user");
            //CreateUser("User", "Some", "User", "User");
            //CreateUser("Billing", "Billing", "User", "Billing user");

            tr.Commit();
        }
    } //CreateSystemUser

    private static void CreateUser(string username, string firstName, string lastName, string role)
    {
        UserEntity system = new UserEntity
        {
            UserName = username,
            PasswordHash = PasswordEncoding.HashPassword(username, username),
            Role = Database.Query<RoleEntity>().Where(r => r.Name == role).SingleEx().ToLite(),
            State = UserState.Active,
        };

        system.Mixin<UserProjectMixin>().FirstName = firstName;
        system.Mixin<UserProjectMixin>().LastName = lastName;

        system.Save();
    }

    public static void CreateApplicationConfiguration()
    {
        using (Transaction tr = new Transaction())
        {
            var en = new CultureInfoEntity(CultureInfo.GetCultureInfo("en")).Save();
            var de = new CultureInfoEntity(CultureInfo.GetCultureInfo("de")).Save();
            new CultureInfoEntity(CultureInfo.GetCultureInfo("en-GB")).Save();
            new CultureInfoEntity(CultureInfo.GetCultureInfo("de-DE")).Save();

            new ApplicationConfigurationEntity
            {
                Environment = "Production",
                DatabaseName = Connector.Current.DatabaseName(),
                AuthTokens = new AuthTokenConfigurationEmbedded
                {
                }, //Auth

                Email = new EmailConfigurationEmbedded
                {
                    SendEmails = true,
                    DefaultCulture = de,
                    UrlLeft = "http://localhost/RRSP"
                },
                EmailSender = new EmailSenderConfigurationEntity
                {
                    Name = "localhost",
                    Service = new SmtpEmailServiceEntity
                    {
                        Network = new SmtpNetworkDeliveryEmbedded
                        {
                            Host = "localhost"
                        },
                    }
                }, //Email
                DefaultAccountingReceiver = Database.Query<UserEntity>().SingleEx(a => a.UserName == "System").ToLite(),
                Folders = new FoldersConfigurationEmbedded
                {
                    BillingDocumentFolder = @"billable-documents",
                    AttachmentsFolder = @"attachments",
                    CachedQueryFolder = @"cached-query",
                    ExceptionsFolder = @"exception",
                    OperationLogFolder = @"operation-log",
                    EmailMessageFolder = @"email-message",
                    ViewLogFolder = @"view-log",
                    VideosFolder = @"videos",
                    VideoThumbnailsFolder = @"video-thumbnails",
                    ProjectStatusReportFolder = @"project-status-report",
                    ProtocolReportFolder = @"protocol-report",
                    DelimitationDocumentFolder = @"delimitation-document",
                    CommunicationLetterFolder = @"communication-letter",
                    ExternalBillableAttachmentFolder = @"external-billable-attachment",
                    SkillCertificateFolder = @"skill-certificate",
                    WhatsNewDocumentFolder = @"whatsnew-document",
                    WhatsNewPreviewFolder = @"whatsnew-preview",
                    VideoInlineImagesFolder = @"video-image",
                    HelpImagesFolder = @"help-image",
                    StatementOfWorkReportFolder = @"statement-of-work",
                    ContractAttachmentsFolder = @"contract-attachment",
                    InvestmentAttachmentFolder = @"investment-attachment",
                    InvestmentReportFolder = @"investment-report",
                    ChangeRequestReportFolder = @"change-request-report",
                    ChangeRequestAttachmentFolder = @"change-request-attachment",
                    WorkPackageReportFolder = @"work-package-report",
                    WorkPackageAttachmentFolder = @"work-package-attachment",
                    ProjectChartFolder = @"project-chart-report",
                    DocumentQMAttachmentFolder = @"document-qm-attachment",
                    BusinessCaseBaseLineFolder = @"business-case-base-line",
                },
                //ActiveDirectory = new ActiveDirectoryConfigurationEmbedded
                //{
                //    AzureAD = new AzureActiveDirectoryEmbedded
                //    {
                //        ApplicationID = Guid.Parse("06f172a6-eb85-4727-bb6a-035b1e18009a"),
                //        DirectoryID = Guid.Parse("ecd106db-905e-428b-a596-a35a36d07bb0"),
                //        LoginWithAzureAD = true,
                //    },
                //    WindowsAD = null,
                //    AllowMatchUsersBySimpleUserName = true,
                //    AutoCreateUsers = true,
                //    DefaultRole = null,
                //},
                Translation = new TranslationConfigurationEmbedded
                {
                    AzureCognitiveServicesAPIKey = "b11452a5cfd44c969eb45e4d2d8f98d0",
                    DeepLAPIKey = "a854df34-67bb-7569-206b-c5b568ea7715", //ralf.schmitz@bechtle.com 
                },
                Task = new TaskConfigEmbedded()
                {
                },
            }.Save();

            tr.Commit();
        }
    }
    public static void ImportUserAssets()
    {
    }
}
