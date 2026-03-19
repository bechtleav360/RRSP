using Meros.PlanningProject.PSC;
using Meros.PortfolioExt;
using Meros.PortfolioExt.InitiationRequest;
using Meros.PortfolioExt.PortfolioRelationship;
using Meros.Project;
using Meros.Project.Customers;
using Meros.Project.Orders;
using Meros.Project.Portfolio;
using Meros.Project.Program;
using Meros.Protocol;
using Meros.Risk;
using Meros.Stakeholder;
using Meros.StatusReport;
using Meros.Tasks;
using RRSP.Project;
using Signum.Authorization;
using Signum.Security;
using System.Text.RegularExpressions;

namespace RRSP.Test.Environment;

public static class Tools
{
    public static Lite<RiskCategoryTableEntity> RiskTable = null!;
    public static Lite<RiskCategoryTableEntity> ChanceTable = null!;

    static RoleEntity superUser = null!;

    static Regex regex = new Regex(@"[\[_\-()\]]");

    public static UserEntity GetOrCreateUser(string name, RoleEntity? role = null)
    {
        var userName = regex.Replace(name.ToLower().Replace(" ", ""), ".").Trim('.');

        var email = userName + "@" + userName + ".de";

        var already = Database.Query<UserEntity>().SingleOrDefault(a => a.UserName == email);
        if (already != null)
            return already;

        if (superUser == null)
            superUser = Database.Query<RoleEntity>().SingleEx(a => a.Name == "Super user");

        using (OperationLogic.AllowSave<UserEntity>())
        {
            UserEntity user = new UserEntity
            {
                UserName = email,
                Email = email,
                PasswordHash = PasswordEncoding.EncodePassword(userName, userName),
                Role = (role ?? superUser).ToLite(),
                State = UserState.Active,
            };

            user.Mixin<UserProjectMixin>().FirstName = name;
            user.Mixin<UserProjectMixin>().LastName = "benutzer";
            user.Save();

            return user.Save();
        }
    }

    public static PortfolioEntity WithMembers(this PortfolioEntity portfolio, string[] names, bool isChair = false, bool isManager = false)
    {
        portfolio.SteeringCommittee.AddRange(names.Select(n => new PortfolioSteeringCommitteeMemberEmbedded
        {
            Member = GetOrCreateUser(n).ToLite(),
            IsChair = isChair,
            IsManager = isManager
        }));

        return portfolio;
    }

    public static PortfolioEntity WithBudget(this PortfolioEntity portfolio, decimal amount)
    {
        portfolio.Mixin<DomainRiskMixin>().RiskManagement = new RiskManagementEmbedded
        {
            RiskCategoryTable = RiskTable,
            ChanceCategoryTable = ChanceTable,
            OverallValue = amount,
            RiskManager = portfolio.Responsible!,
        };

        portfolio.Execute(PortfolioOperation.Save);

        return portfolio;
    }

    public static InitiationRequestEntity RandomEvaluation(this InitiationRequestEntity ir)
    {
        ir.SyncEvaluations();
        var r = Random.Shared;
        var values = Enum.GetValues<EvaluationValue>();
        foreach (var item in ir.EvaluationCriteriaValues)
        {
            item.Value = r.NextElement(values);
        }
        ir.SyncEvaluations();
        ir.Save();
        return ir;
    }

    public static T WithGoal<T>(this T domain, string title, string subGoals)
        where T : Entity, IDomainEntity
    {
        var g = new GoalEntity
        {
            Domain = domain.ToLite(),
            Title = title,
            Description = subGoals,
        }.Execute(GoalOperation.Save);



        return domain;
    }

    public static ProgramEntity CreatProgram(string title, string ziel, string kpis, decimal budget, int yearStart, int yearEnd, string leitung, Lite<PortfolioEntity> portfolio)
    {
        using (new EntityCache())
        {
            var manager = GetOrCreateUser(leitung.TryBefore(",") ?? leitung).ToLite();
            var program = new ProgramEntity
            {
                Name = title,
                Manager = manager,
            }
            .SetMixin((DomainTaskMixin a) => a.Prefix, DomainTaskMixin.CalculatePrefix(title))
            .SetMixin((DomainRiskMixin dr) => dr.RiskManagement, new RiskManagementEmbedded
            {
                RiskCategoryTable = RiskTable,
                ChanceCategoryTable = ChanceTable,
                OverallValue = budget,
                RiskManager = manager!,
            })
            .Execute(ProgramOperation.Save)
            .WithGoal(title, kpis);

            var pir = new InitiationRequestEntity
            {
                FromDomain = program.ToLite(),
                ParentDomain = portfolio,
                Title = title,
                Initiator = leitung,
                Owner = leitung,
                Manager = manager,
                SolutionProvider = "Firma AG",
                OrganizationUnit = "OU",
                ApprovingAuthority = "Genehmigende Bundersamt",
                EstimatedEffort = Random.Shared.Next(1, 100) * 5,
                EstimatedEffortComment = "Kommentar für " + title,
                TargetDeliveryDate = new DateOnly(yearEnd, 1, 1),
                ContextSituation = "Kontext für " + title,
                LegalBasis = "Geschätliche grundlage für " + title,
                Outcomes = ziel,
                Impact = "Impakt für " + title,
                SuccessCriteria = kpis,
                Assumptions = "Annahmen für " + title,
                Constraints = "Restriktionen für " + title,
                Risks = "Risiko für " + title,
                Version = "1.0",
                State = InitiationRequestState.EvaluationFinished,
            }
            .RandomEvaluation()
            .Execute(InitiationRequestOperation.Approve);

            return program;
        }
    }


    public static ProjectEntity CreateProject(string title, string ziel, string kpis, string status, decimal budget, string leitung, string resources, string milestones, string risks, string umfanganderungen,
        ProgramEntity? program = null, PortfolioEntity? portfolio = null)
    {
        using (new EntityCache())
        {
            var project = OperationLogic.Construct(ProjectExpandedOperation.Create, new NewProjectModel
            {
                ProjectName = title.Etc(100),
                ProjectPrefix = DomainTaskMixin.CalculatePrefix(title),
                CreateNewBoard = new NewBoardEmbedded
                {
                    Columns =
                    {
                        new NewColumnEmbedded { Name = "To Do", TaskState = TaskState.Open },
                        new NewColumnEmbedded { Name = "In Progress", TaskState = TaskState.InProgress },
                        new NewColumnEmbedded { Name = "Done", TaskState = TaskState.Done, ArchiveTasksOlderThan = 10 },
                    },
                },
            })
            .Do(p =>
            {
                p.Mixin<DomainRiskMixin>().RiskManagement = new RiskManagementEmbedded
                {
                    RiskCategoryTable = RiskTable,
                    ChanceCategoryTable = ChanceTable,
                    OverallValue = budget,
                    RiskManager = AuthLogic.SystemUser!.ToLite(),
                };

                p.Execute(ProjectOperation.Save);
            })
            .WithGoal(title, kpis);

            var pir = new InitiationRequestEntity
            {
                FromDomain = project.ToLite(),
                ParentDomain = portfolio?.ToLite() ?? ((IDomainEntity?)program)?.ToLite(),
                Title = title,
                Initiator = leitung,
                Owner = leitung,
                SolutionProvider = "Firma AG",
                OrganizationUnit = "OU",
                ApprovingAuthority = "Genehmigende",
                EstimatedEffort = Random.Shared.Next(1, 100) * 10,
                EstimatedEffortComment = "Kommentar für " + title,
                TargetDeliveryDate = Clock.Today.AddMonths(12),
                ContextSituation = "Kontext für " + title,
                LegalBasis = "Geschätliche grundlage für " + title,
                Outcomes = ziel,
                Impact = umfanganderungen,
                SuccessCriteria = kpis,
                Assumptions = "Ressourcen: " + resources,
                Constraints = milestones,
                Risks = risks,
                Version = "1.0",
                State = InitiationRequestState.EvaluationFinished
            }.RandomEvaluation()
            .Execute(InitiationRequestOperation.Approve);

            if (status == "Abgeschlossen")
                project.Execute(ProjectOperation.Archive);

            return project;
        }
    }
}

public class RRSPLoader
{
    static void DeleteAll<T>() where T : Entity
    {
        using (Administrator.DisableHistoryTable<T>(true))
        {
            Database.Query<T>().UnsafeDelete("auto");
            using (SystemTime.Override(new SystemTime.HistoryTable()))
                Database.Query<T>().UnsafeDelete("auto");
        }
    }

    public static void CleanDummy()
    {
        DeleteAll<BoardEntity>();
        DeleteAll<DynamicTaskScriptEntity>();
        DeleteAll<NotificationConfigEntity>();
        DeleteAll<ProtocolTemplateEntity>();
        DeleteAll<StatusReportEntity>();
        DeleteAll<StatusReportTemplateEntity>();
        DeleteAll<PlanningGroupEntity>();
        DeleteAll<RiskTemplateEntity>();
        DeleteAll<StakeholderTemplateEntity>();
        DeleteAll<ServiceEntity>();
        DeleteAll<ServiceTemplateEntity>();
        DeleteAll<ChangeRequestTemplateEntity>();
        DeleteAll<PortfolioRelationshipEntity>();
        DeleteAll<TeamMemberEntity>();
        DeleteAll<TaskTransitionEntity>();
        DeleteAll<TaskAssignedLogEntity>();
        DeleteAll<TaskRelationshipEntity>();
        DeleteAll<AttachmentEntity>();
        DeleteAll<ExternalBillableConceptEntity>();
        DeleteAll<WatchedProtocolPointEntity>();
        DeleteAll<StakeholderLogEntity>();
        DeleteAll<ChangeRequestCommentEntity>();
        using (CustomerProjectLogic.Disable())
            DeleteAll<OrderPositionEntity>();
        DeleteAll<RiskEntity>();
        DeleteAll<RequirementEntity>();
        DeleteAll<DeliverableEntity>();
        DeleteAll<ConstraintEntity>();
        DeleteAll<StakeholderEntity>();
        DeleteAll<ChangeRequestEntity>();
        DeleteAll<GoalEntity>();
        DeleteAll<CommentEntity>();
        DeleteAll<OrderTimeCategoryEntity>();
        DeleteAll<TaskEntity>();
        DeleteAll<ProtocolPointEntity>();
        DeleteAll<GoalEntity>();
        DeleteAll<AssumptionEntity>();
        DeleteAll<TaskLabelEntity>();
        DeleteAll<ColumnEntity>();
        DeleteAll<OrderProjectEntity>();
        DeleteAll<MeetingProtocolEntity>();
        DeleteAll<MilestoneEntity>();
        DeleteAll<MemberEntity>();
        DeleteAll<ExternalMemberEntity>();
        DeleteAll<ProjectPrioritizationEntity>();
        DeleteAll<InitiationRequestEntity>();
        DeleteAll<ProjectClassificationEntity>();
        DeleteAll<ProjectEntity>();
        DeleteAll<PortfolioEntity>();
    }

    public static void LoadDumyAndAll()
    {
        using (AuthLogic.UnsafeUserSession("System"))
        {
            DummyPortfolio();
            FillWeights();
        }
    }

    static RiskCategoryTableEntity CreateCategoryTable(bool reverse)
    {
        var colors = new[] { "#00FF00", "#7FFF00", "#FFFF00", "#FF7F00", "#FF0000" };

        if (reverse)
        {
            Array.Reverse(colors);
        }   

        return new RiskCategoryTableEntity
        {
            Name = reverse ? "Chancen-Kategorie-Tabelle" : "Risiko-Kategorie-Tabelle",
            ProbabilityRanges = 
            {
                new RiskRangeEmbedded { Name = "Sehr gering", MinValue = 0, MaxValue = 0.1m },
                new RiskRangeEmbedded { Name = "Gering", MinValue = 0.11m, MaxValue = 0.3m },
                new RiskRangeEmbedded { Name = "Mittel", MinValue = 0.31m, MaxValue = 0.6m },
                new RiskRangeEmbedded { Name = "Hoch", MinValue = 0.61m, MaxValue = 0.8m },
                new RiskRangeEmbedded { Name = "Sehr hoch", MinValue = 0.81m, MaxValue = 1m },
            },
            ImpactRanges = 
            {
                new RiskRangeEmbedded { Name = "Sehr gering", MinValue = 0, MaxValue = 0.1m },
                new RiskRangeEmbedded { Name = "Gering", MinValue = 0.11m, MaxValue = 0.3m },
                new RiskRangeEmbedded { Name = "Mittel", MinValue = 0.31m, MaxValue = 0.6m },
                new RiskRangeEmbedded { Name = "Hoch", MinValue = 0.61m, MaxValue = 0.8m },
                new RiskRangeEmbedded { Name = "Sehr hoch", MinValue = 0.81m, MaxValue = 1m },
            },
            Categories = 
            {
                new RiskCategoryEntity { Name = "Sehr gering", Color = colors[0]},
                new RiskCategoryEntity { Name = "Gering", Color = colors[1] },
                new RiskCategoryEntity { Name = "Mittel", Color = colors[2]},
                new RiskCategoryEntity { Name = "Hoch", Color = colors[3] },
                new RiskCategoryEntity { Name = "Sehr hoch", Color = colors[4] },
            },
        }
        .FillCells()
        .Save();
    }


    public static void DummyPortfolio()
    {
        Tools.RiskTable = CreateCategoryTable(false).ToLite();
        Tools.ChanceTable = CreateCategoryTable(true).ToLite();

        using (var tr = new Transaction())
        {
            var mainPortfolio = new PortfolioEntity
            {
                Name = "Digital Connect 2030",
                StartYear = 2022,
                EndYear = 2030,
                Type = PortfolioType.OrganizationalPortfolio,
                Responsible = UserEntity.Current
            }
               .SetMixin((DomainTaskMixin a) => a.Prefix, DomainTaskMixin.CalculatePrefix("PRT"))
               .WithMembers("BMDV (XY 10)".Split("; "), isChair: true)
               .WithMembers("Referatsleitung Susanne Tulpe".Split("; "), isManager: true)
               .WithMembers("BMDV, BKAmt, BMAS, BMBF, BMI, BMUV, BMWK".Split(", "))
               .Save()
               .WithBudget(2100)
               .WithGoal("Aufbau einer zukunftsfähigen und flächendeckenden digitalen Infrastruktur sowie einer umfassenden Verwaltungsdigitalisierung",
                       """
                Glasfaserabdeckung (%) bis 2025 (50%) bis 2030 (100%)
                5G-Abdeckung bis 2030 (%)
                Beschleunigte Genehmigungsverfahren (Dauer der Genehmigungsverfahren)
                Förderung innovativer Ansätze (ausgezahlter Förderbetrag)
                Digitalisierungsgrad der Verwaltungsprozesse (60%)
                Reduzierung der Bearbeitungszeit für Verwaltungsanträge (50%)
                Nutzer finden die digitalen Verwaltungsdienste einfach zu bedienen (90%)
                """);

            {
                var subPortfolio = new PortfolioEntity
                {
                    ParentPortfolio = mainPortfolio.ToLite(),
                    Name = "Netzausbau",
                    StartYear = 2022,
                    EndYear = 2030,
                    Responsible = UserEntity.Current
                }
                .SetMixin((DomainTaskMixin a) => a.Prefix, DomainTaskMixin.CalculatePrefix("PRT"))
                .WithMembers("BMDV (SU I)".Split(", "), isChair: true)
                .WithMembers("Ursina Lardos".Split(", "), isManager: true)
                .WithMembers("BMDV, BKAmt, BMAS, BMBF, BMI, BMUV, BMWK".Split(", "))
                .Save()
                .WithBudget(1045)
                .WithGoal("Förderung innovativer Ansätze",
                        """
                Glasfaserabdeckung (%) bis 2025 (50%) bis 2030 (100%)
                5G-Abdeckung bis 2030 (%)
                Dauer der Genehmigungsverfahren: Reduktion um 50% bis 2025
                """);

                {
                    var program = Tools.CreatProgram(
                        title: "Glasfaser für alle",
                        ziel: "Flächendeckender Glasfaserausbau bis 2030",
                        kpis: "Glasfaserabdeckung (%) bis 2025 (50%) bis 2030 (100%)",
                        budget: 660,
                        yearStart: 2022,
                        yearEnd: 2030,
                        leitung: "BMDV (GH 2), Alexandra Hartnagel",
                        portfolio: subPortfolio.ToLite()
                        );


                    var project1 = Tools.CreateProject("Durchführung einer bundesweiten Potenzialanalyse",
                        ziel: "Identifikation der Gebiete mit dem höchsten Bedarf und Potenzial für Glasfaserausbau",
                        kpis: """
            Anzahl der analysierten Gebiete: 100 bis Q3 2023
            Genauigkeit der Daten: 95% bis Q1 2024
            """,
                        status: "In Umsetzung",
                        budget: 20,
                        leitung: "Dr. Michael Müller",
                        resources: "Externe Berater, GIS-Software, Datenanalysten",
                        milestones: """
            Startdatum: Januar 2023
            Q1 2023: Ausschreibung und Auswahl der Berater
            Q3 2023: Datensammlung und Analyse
            Q1 2024: Abschlussbericht und Präsentation
            Enddatum: März 2024
            """,
                        risks: "Verzögerungen bei der Datensammlung, ungenaue Daten",
                        umfanganderungen: "Anpassungen basierend auf neuen Datenquellen",
                        program: program
                        );

                    var project3 = Tools.CreateProject("Informationskampagnen zur Anregung der Nachfrage nach Glasfaseranschlüssen",
                       ziel: "Steigerung der Nachfrage nach Glasfaseranschlüssen",
                       kpis: """
            Anzahl der durchgeführten Kampagnen: 10 bis 2030
            Erhöhung der Nachfrage: 30% bis 2025
            """,
                       status: "In Umsetzung",
                       budget: 50,
                       leitung: "Julia Becker",
                       resources: "Marketingagenturen, Medienpartnerschaften",
                       milestones: """
            Startdatum: Januar 2023
            Q1 2023: Kampagnenplanung und -entwicklung
            Q2 2023: Start der ersten Kampagne
            Jährlich: Evaluierung und Anpassung der Kampagnen
            Enddatum: Dezember 2030
            """,
                       risks: "Geringe Resonanz, Budgetüberschreitungen",
                       umfanganderungen: "Anpassungen basierend auf Kampagnenergebnissen",
                       program: program
                   );

                    var project4 = Tools.CreateProject("Roll-out Schließung \"Weiße Flecken\" Gebiete mit Downloadgeschwindigkeit von weniger 30 Mbit/s (oft ländlichen Regionen)",
                        ziel: "Versorgung ländlicher Regionen mit Glasfaseranschlüssen",
                        kpis: """
            Anzahl der angeschlossenen Haushalte: 500.000 bis 2027
            Reduktion der weißen Flecken: 90% bis 2027
            """,
                        status: "In Umsetzung",
                        budget: 300,
                        leitung: "Thomas Wagner",
                        resources: "Bauunternehmen, Ingenieure, lokale Behörden (Kommunal-Ebene)",
                        milestones: """
            Startdatum: Januar 2022
            Q1 2022: Identifikation der Gebiete
            Q3 2024: Beginn der Bauarbeiten
            Q4 2027: Abschluss der Bauarbeiten
            Enddatum: Dezember 2027
            """,
                        risks: "Genehmigungsverzögerungen, technische Herausforderungen",
                        umfanganderungen: "Anpassungen basierend auf Baufortschritt und Feedback",
                        program: program
                    );

                    var project5 = Tools.CreateProject("Roll-out Schließung \"Graue Flecken\" Gebiete mit Downloadgeschwindigkeit von weniger als 100 Mbit/s",
                         ziel: "Verbesserung der Internetgeschwindigkeit in unterversorgten Gebieten",
                         kpis: """
            Anzahl der angeschlossenen Haushalte: 300.000 bis 2029
            Reduktion der grauen Flecken: 80% bis 2029
            """,
                         status: "In Planung",
                         budget: 250,
                         leitung: "Markus Fischer",
                         resources: "Bauunternehmen, Ingenieure, lokale Behörden",
                         milestones: """
            Startdatum: Januar 2025
            Q1 2025: Identifikation der Gebiete
            Q3 2025: Beginn der Bauarbeiten
            Q4 2029: Abschluss der Bauarbeiten
            Enddatum: Dezember 2029
            """,
                         risks: "Genehmigungsverzögerungen, technische Herausforderungen, Budgetüberschreitung durch Verteuerung Baumaßnahmen",
                         umfanganderungen: "Anpassungen basierend auf Baufortschritt und Feedback",
                         program: program
                     );

                    var project6 = Tools.CreateProject("Evaluierung der „Überbauproblematik\"",
                        ziel: "Vermeidung wettbewerbswidriger Überbauformen beim Glasfaserausbau",
                        kpis: """
            Anzahl der durchgeführten Evaluierungen: 3 bis 2026
            """,
                        status: "In Umsetzung",
                        budget: 30,
                        leitung: "Dr. Anna Weber",
                        resources: "Regulierungsbehörden, Marktforscher, Juristen",
                        milestones: """
            Startdatum: Januar 2023
            Q1 2023: Beginn der Evaluierung
            Q4 2024: Zwischenbericht
            Q4 2026: Abschlussbericht und Empfehlungen
            Enddatum: Dezember 2026
            """,
                        risks: "Komplexität der Marktanalyse, rechtliche Herausforderungen",
                        umfanganderungen: "Anpassungen basierend auf Evaluierungsergebnissen",
                        program: program
                    );

                }


                {
                    var program = Tools.CreatProgram(
                        title: "Mobilfunk für morgen - Mobilfunkförderung",
                        ziel: "Flächendeckende 5G-Versorgung und Abdeckung der Mobilfunk-Lücken",
                        kpis: "5G-Abdeckung bis 2030 (%)",
                        budget: 220,
                        yearStart: 2022,
                        yearEnd: 2030,
                        leitung: "BMDV (ST 7), Jan-Heinrich Pfeiffer",
                        portfolio: subPortfolio.ToLite()
                        );

                    var project1 = Tools.CreateProject("Erarbeitung eines frequenzregulatorischen Gesamtkonzeptes zur Bereitstellung von Frequenzen bei 800 MHz, 1,8 GHz und 2,6 GHz",
                        ziel: "Sicherstellung der Verfügbarkeit und optimalen Nutzung von Frequenzen für 5G",
                        kpis: """
                Anzahl der bereitgestellten Frequenzen: 3 bis Q4 2023
                Abschluss des Gesamtkonzeptes: Dezember 2023
                """,
                        status: "In Umsetzung",
                        budget: 30,
                        leitung: "Bundesnetzagentur, Orkan Özgün",
                        resources: "Frequenzexperten, Juristen, technische Berater",
                        milestones: """
                Startdatum: April 2022
                Q2 2022: Beginn der Konzeptentwicklung
                Q4 2023: Abschluss des Gesamtkonzeptes
                Enddatum: Dezember 2023
                """,
                        risks: "Regulatorische Herausforderungen, technische Komplexität",
                        umfanganderungen: "Anpassungen basierend auf regulatorischen Anforderungen",
                        program: program
                    );

                    var project2 = Tools.CreateProject("Roll-Out Mobilfunkförderung Schließung von unversorgten „weißen Flecken\"",
                        ziel: "Schließung von Mobilfunklücken in unterversorgten Gebieten",
                        kpis: """
                Anzahl der geschlossenen weißen Flecken: 500 bis 2027
                Erhöhung der Mobilfunkabdeckung: 90% bis 2027
                """,
                        status: "In Bearbeitung",
                        budget: 120,
                        leitung: "MIG (Mobilfunkinfrastrukturgesellschaft)",
                        resources: "Bauunternehmen, Ingenieure, lokale Behörden",
                        milestones: """
                Startdatum: Januar 2023
                Q1 2023: Markterkundungsverfahren
                Q3 2023: Beginn des Roll-Outs
                Q4 2025: Betrieb erster geförderter Standorte
                Enddatum: Dezember 2027
                """,
                        risks: "Genehmigungsverzögerungen, technische Herausforderungen",
                        umfanganderungen: "Anpassungen basierend auf Baufortschritt und Feedback",
                        program: program
                    );

                    var project3 = Tools.CreateProject("Entwicklung Ökosystem für Campus-Netze",
                        ziel: "Förderung der Entwicklung und Nutzung von Campus-Netzen",
                        kpis: """
                Anzahl der entwickelten Campus-Netze: 10 bis 2028
                Erfolgreiche Pilotprojekte: 5 bis 2026
                """,
                        status: "In Planung",
                        budget: 40,
                        leitung: "Dr. Lisa Meier",
                        resources: "Technische Berater, Unternehmen, Forschungseinrichtungen",
                        milestones: """
                Startdatum: Januar 2024
                Q1 2024: Konzeptentwicklung
                Q3 2024: Pilotprojekte starten
                Q4 2026: Evaluierung der Pilotprojekte
                Enddatum: Dezember 2028
                """,
                        risks: "Technologische Herausforderungen, Akzeptanzprobleme",
                        umfanganderungen: "Anpassungen basierend auf Pilotprojektergebnissen",
                        program: program
                    );

                    var project4 = Tools.CreateProject("Verbesserung Mobilfunkversorgung an Bahnstrecken und Zügen",
                        ziel: "Verbesserung der Mobilfunkversorgung entlang von Bahnstrecken und in Zügen",
                        kpis: """
                Anzahl der verbesserten Strecken: 50 bis 2030
                Erhöhung der Mobilfunkabdeckung: 80% bis 2030
                """,
                        status: "In Planung",
                        budget: 30,
                        leitung: "Deutsche Bahn AG, Luise Lampe",
                        resources: "Technische Berater, Bauunternehmen, Zugbetreiber",
                        milestones: """
                Startdatum: Januar 2025
                Q1 2025: Identifikation der kritischen Strecken
                Q3 2025: Beginn der Bauarbeiten
                Q4 2029: Abschluss der Bauarbeiten
                Enddatum: Dezember 2030
                """,
                        risks: "Genehmigungsverzögerungen, technische Herausforderungen",
                        umfanganderungen: "Anpassungen basierend auf Baufortschritt und Feedback",
                        program: program
                    );

                }

                {
                    var program = Tools.CreatProgram(
                        title: "Schneller Genehmigen - Gigabitgrundbuch",
                        ziel: "Das Gigabitgrundbuch schafft mehr Transparenz hinsichtlich der für den Gigabit-Ausbau relevanten Informationen. Genehmigungsverfahren für den Bau von Telekommunikationsinfrastrukturen beschleunigt und digitalisiert. die Nutzung alternativer Verlegetechniken deutlich gestärkt",
                        kpis: "Dauer der Genehmigungsverfahren: Reduktion um 50% bis 2025",
                        budget: 165,
                        yearStart: 2022,
                        yearEnd: 2030,
                        leitung: "BMDV (WI 6), Rolf Registermann",
                        portfolio: subPortfolio.ToLite()
                        );

                    var project1 = Tools.CreateProject("Start Internetauftritt",
                        ziel: "Bereitstellung einer zentralen Informationsplattform für den Gigabit-Ausbau",
                        kpis: """
                Anzahl der Website-Besucher: 100.000 bis Q1 2023
                Zufriedenheit der Nutzer: 90% positive Rückmeldungen bis Q1 2023
                """,
                        status: "Abgeschlossen",
                        budget: 10,
                        leitung: "Kim Krakow",
                        resources: "Webentwickler, UX/UI-Designer, Content-Manager",
                        milestones: """
                Startdatum: April 2022
                Q2 2022: Konzeptentwicklung und Design
                Q4 2022: Entwicklung der Website
                Q1 2023: Launch der Website
                Enddatum: März 2023
                """,
                        risks: "Technische Herausforderungen, Verzögerungen bei der Entwicklung",
                        umfanganderungen: "Anpassungen basierend auf Nutzerfeedback",
                        program: program
                    );

                    var project2 = Tools.CreateProject("Einrichtung und Erweiterung Geo-Datendienste",
                        ziel: "Bereitstellung und Optimierung von Geo-Datendiensten zur Unterstützung des Gigabit-Ausbaus",
                        kpis: """
    Anzahl der implementierten Geo-Datendienste: 5 bis 2025
    Zufriedenheit der Nutzer: 85% positive Rückmeldungen bis 2025
    """,
                        status: "In Umsetzung",
                        budget: 80,
                        leitung: "Dr. Laura Schmidt",
                        resources: "GIS-Experten, Datenanalysten, IT-Infrastruktur",
                        milestones: """
    Startdatum: Januar 2023
    Q1 2023: Bedarfsermittlung und Planung
    Q3 2023: Implementierung der ersten Geo-Datendienste
    Q4 2025: Erweiterung und Optimierung der Dienste
    Enddatum: Dezember 2025
    """,
                        risks: "Datenintegrationsprobleme, technische Herausforderungen",
                        umfanganderungen: "Anpassungen basierend auf neuen Anforderungen und Feedback",
                        program: program
                    );

                    var project3 = Tools.CreateProject("Schaffung einer DIN-Norm für moderne Verlegemethoden zum beschleunigten Gigabitausbau",
                        ziel: "Standardisierung und Beschleunigung des Gigabitausbaus durch moderne Verlegemethoden",
                        kpis: """
                Zeitersparnis durch neue Normen: 20% Reduktion der Verlegezeit bis 2028
                Zufriedenheit der beteiligten Stakeholder: 85% positive Rückmeldungen bis 2028
                """,
                        status: "In Planung",
                        budget: 75,
                        leitung: "Rolf Registermann",
                        resources: "Normungsexperten, Ingenieure, Bauunternehmen",
                        milestones: """
                Startdatum: Juli 2025
                Q3 2024: Ein Expertengremium wird gebildet, um die Anforderungen und Rahmenbedingungen für die neue DIN-Norm zu definieren.
                Q1 2025: Der Entwurf der DIN-Norm wird erstellt und zur internen Überprüfung und Abstimmung vorgelegt.
                Q2 2027: Eine öffentliche Konsultation wird durchgeführt, um Feedback von Stakeholdern und der Öffentlichkeit zu sammeln. Basierend auf diesem Feedback werden Anpassungen vorgenommen.
                Q1 2028: Die endgültige Version der DIN-Norm wird veröffentlicht und tritt in Kraft
                Enddatum: März 2028
                """,
                        risks: "Widerstand von Interessengruppen, technische Herausforderungen",
                        umfanganderungen: "Anpassungen basierend auf Feedback während der Konsultation",
                        program: program
                    );

                }

            }

            {
                var subPortfolio = new PortfolioEntity
                {
                    ParentPortfolio = mainPortfolio.ToLite(),
                    Name = "Verwaltungsdigitalisierung",
                    StartYear = 2022,
                    EndYear = 2030,
                    Responsible = UserEntity.Current
                }
                .SetMixin((DomainTaskMixin a) => a.Prefix, DomainTaskMixin.CalculatePrefix("PRT"))
                .WithMembers("BMDV (XY 10)".Split(", "), isChair: true)
                .WithMembers("Referatsleitung Susanne Tulpe".Split(", "), isManager: true)
                .WithMembers("BMI, BMWK, BMBF, BMF".Split(", "))
                .Save()
                .WithBudget(1000)
                .WithGoal("Umsetzung einer umfassenden Verwaltungsdigitalisierung",
                        """
                Digitalisierungsgrad der Verwaltungsprozesse (60%)
                Reduzierung der Bearbeitungszeit für Verwaltungsanträge (50%)
                Nutzer finden die digitalen Verwaltungsdienste einfach zu bedienen (90%)
                """);

                {
                    var program = Tools.CreatProgram(
                        title: "Verwaltung der Bildung",
                        ziel: "Verbesserung der Effizienz und Nutzerfreundlichkeit von Bildungsverwaltungsdiensten durch umfassende Digitalisierung und Automatisierung von Prozessen.",
                        kpis: """
                    80% der vollständig digitalisierten Prozesse im Vergleich zu den Gesamtprozessen bis 2030
                    Anzahl der Nutzer, die die digitalen Verwaltungsdienste regelmäßig nutzen auf 70% erhöht
                    """,
                        budget: 100,
                        yearStart: 2022,
                        yearEnd: 2030,
                        leitung: "BMBF, (GH I) Dr. Anna Müller",
                        portfolio: subPortfolio.ToLite()
                        );

                    var project1 = Tools.CreateProject("Anerkennung Ausländischer Berufsqualifikation",
                        ziel: "Erleichterung des Zugangs zum deutschen Arbeitsmarkt für Fachkräfte mit ausländischen Berufsqualifikationen.",
                        kpis: """
                    Bearbeitungszeit für Anträge: Reduktion um 60% bis 2030
                    Anzahl der Nutzer, die die digitalen Verwaltungsdienste regelmäßig nutzen auf 70% erhöht
                    """,
                        status: "In Umsetzung",
                        budget: 10,
                        leitung: "Simone Salzmann",
                        resources: "Externe Berater, IT-Systeme, Schulungsprogramme",
                        milestones: """
                    Startdatum: Januar 2024
                    Q1 2024: Ausschreibung und Auswahl der externen Berater abgeschlossen.
                    Q2 2024: Beginn der Analyse der bestehenden Anerkennungsverfahren.
                    Q4 2024: Implementierung der ersten IT-Systeme zur Digitalisierung der Antragsprozesse.
                    Q1 2025: Schulung der Mitarbeiter im Umgang mit den neuen Systemen.
                    Q3 2025: Vollständige Implementierung der IT-Systeme.
                    Q1 2026: Beginn der Pilotphase mit ausgewählten Anträgen.
                    Q2 2026: Auswertung der Pilotphase und Anpassung der Systeme basierend auf Feedback.
                    Q1 2027: Offizieller Start des digitalen Anerkennungsverfahrens.
                    Enddatum: März 2024
                    """,
                        risks: "Verzögerungen bei der Implementierung, unzureichende Schulungen",
                        umfanganderungen: "Anpassungen basierend auf Feedback und neuen Anforderungen",
                        program: program
                    );

                    var project2 = Tools.CreateProject("Digitalisierung des Rechtsreferendariat",
                        ziel: "Verbesserung der praktischen Ausbildung von Rechtsreferendaren durch Digitalisierung der Ausbildungs- und Verwaltungsprozesse.",
                        kpis: """
                    Anzahl der aufgenommenen Rechtsreferendare pro Jahr Steigerung 20%
                    Erfolgsquote der Rechtsreferendare bei den Abschlussprüfungen auf 80%
                    Zufriedenheit der Rechtsreferendare mit der Ausbildung und Betreuung auf 90%
                    """,
                        status: "In Umsetzung",
                        budget: 15,
                        leitung: "Julia Schmidt",
                        resources: "",
                        milestones: """
                    Startdatum: Januar 2023
                    Q1 2023: Ausschreibung und Auswahl der Ausbildungsplätze
                    Q3 2023: Veröffentlichung der Ausschreibung für Ausbildungsplätze und digitale Systeme.
                    Q4 2023: Auswahl geeigneter Ausbildungsplätze und Anbieter für digitale Systeme.
                    Q2 2024: Vorbereitung und Planung: Erstellung eines detaillierten Implementierungsplans. Schulung der Mentoren und Ausbilder im Umgang mit den neuen digitalen Systemen.
                    Q3 2024: Implementierung der digitalen Systeme: Installation und Test der digitalen Systeme in den ausgewählten Ausbildungsplätzen. Beginn der Nutzung der digitalen Systeme durch die Rechtsreferendare.
                    Q1 2025: Pilotphase und Feedback: Durchführung einer Pilotphase mit einer ausgewählten Gruppe von Rechtsreferendaren.
                    Q3 2025: Sammlung und Auswertung von Feedback zur Nutzung der digitalen Systeme.
                    Q1 2026: Abschluss der Digitalisierung und Schulungen
                    Q2 2026: Anpassung der digitalen Systeme basierend auf dem Feedback aus der Pilotphase. Abschluss der Schulungen für alle Rechtsreferendare und Ausbilder.
                    Enddatum: Juni 2026
                    """,
                        risks: "Engpässe bei der Verfügbarkeit geeigneter Ausbildungsplätze könnten die Implementierung verzögern. Mangelnde Betreuung und Unterstützung der Rechtsreferendare während der Umstellung auf digitale Systeme könnte die Zufriedenheit und den Erfolg beeinträchtigen.",
                        umfanganderungen: "Regelmäßige Überprüfung und Anpassung der digitalen Systeme und Prozesse basierend auf dem Feedback der Rechtsreferendare und Ausbilder.",
                        program: program
                    );
                }


                {
                    var program = Tools.CreatProgram(
                        title: "Verwaltung der Wirtschaft",
                        ziel: "Verbesserung der Effizienz und Nutzerfreundlichkeit von Wirtschaftsverwaltungsdiensten durch umfassende Digitalisierung und Automatisierung von Prozessen.",
                        kpis: """
                    80% der vollständig digitalisierten Prozesse im Vergleich zu den Gesamtprozessen bis 2030
                    Anzahl der Nutzer, die die digitalen Verwaltungsdienste regelmäßig nutzen auf 70% erhöht
                    """,
                        budget: 600,
                        yearStart: 2022,
                        yearEnd: 2030,
                        leitung: "BMWK",
                        portfolio: subPortfolio.ToLite()
                        );

                    var project1 = Tools.CreateProject("Anbindung des Handelsregisters an das Wirtschafts-Service-Portal",
                        ziel: "Integration des Handelsregisters in das Wirtschafts-Service-Portal zur Vereinfachung und Beschleunigung von Unternehmensregistrierungen.",
                        kpis: """
                    Anzahl der erfolgreich integrierten Handelsregistereinträge bis 2025.
                    Reduktion der Bearbeitungszeit für Handelsregistereinträge um 50% bis 2025
                    """,
                        status: "In Umsetzung",
                        budget: 30,
                        leitung: "Andrea Valenti",
                        resources: "Externe Berater, IT-Systeme",
                        milestones: """
                    Startdatum: Januar 2025
                    Q1 2025: Ausschreibung und Auswahl der IT-Systeme und Berater.
                    Q2 2026: Beginn der Implementierung der IT-Systeme.
                    Q4 2026: Abschluss der ersten Integrationsphase und Beginn der Pilotphase.
                    Q2 2027: Auswertung der Pilotphase und Anpassung der Systeme.
                    Q4 2027: Vollständige Integration und Schulung der Mitarbeiter.
                    Enddatum: Dezember 2028
                    """,
                        risks: "Verzögerungen bei der Implementierung der IT-Systeme, Unzureichende Schulung der Mitarbeiter.",
                        umfanganderungen: "Anpassungen basierend auf Feedback aus der Pilotphase. Erweiterung der IT-Systeme bei Bedarf.",
                        program: program
                    );

                    var project2 = Tools.CreateProject("Grenzüberschreitende Unternehmens- und Gewerbeanmeldung",
                         ziel: "Ermöglichung der grenzüberschreitenden Unternehmens- und Gewerbeanmeldung durch Integration in das Wirtschafts-Service-Portal",
                         kpis: """
                    Erhöhung der Anzahl grenzüberschreitenden Anmeldungen auf 30% bis 2027.
                    Reduktion der Bearbeitungszeit für grenzüberschreitende Anmeldungen um 40% bis 2027.
                    """,
                         status: "In Umsetzung",
                         budget: 40,
                         leitung: "Hannes Glück",
                         resources: "Juristische Beratung, IT-Systeme",
                         milestones: """
                    Startdatum: Januar 2025
                    Q1 2025: Ausschreibung und Auswahl der IT-Systeme und Berater.
                    Q3 2025: Beginn der Implementierung der IT-Systeme.
                    Q1 2026: Abschluss der ersten Integrationsphase und Beginn der Pilotphase.
                    Q3 2026: Auswertung der Pilotphase und Anpassung der Systeme.
                    Q1 2027: Vollständige Integration und Schulung der Mitarbeiter.
                    Enddatum: März 2027
                    """,
                         risks: "Rechtliche Herausforderungen bei der grenzüberschreitenden Integration. Verzögerungen bei der Implementierung der IT-Systeme.",
                         umfanganderungen: "Anpassungen basierend auf Feedback aus der Pilotphase. Erweiterung der IT-Systeme bei Bedarf.",
                         program: program
                     );


                }
            }

            {
                var subPortfolio = new PortfolioEntity
                {
                    ParentPortfolio = mainPortfolio.ToLite(),
                    Name = "Innovationstreiber (Innovative Netztechnologien)",
                    StartYear = 2022,
                    EndYear = 2030,
                    Responsible = UserEntity.Current
                }
                .SetMixin((DomainTaskMixin a) => a.Prefix, DomainTaskMixin.CalculatePrefix("PRT"))
                .WithMembers("BMDV (LG I)".Split(", "), isChair: true)
                .WithMembers("Alex Arcade".Split(", "), isManager: true)
                .Save()
                .WithBudget(55)
                .WithGoal("Förderung innovativer Ansätze",
                        """
                Ausgezahlter Förderbetrag: 55 Mio. € bis 2030
                Ansätze die auf KPIs des Gesamtportfolio einzahlen: 10 bis 2030
                """);


                var project1 = Tools.CreateProject("Entwicklung alternativer Verlege Techniken",
                    ziel: "Entwicklung und Implementierung neuer Verlegetechniken zur Beschleunigung des Netzausbaus",
                    kpis: """
                Anzahl der entwickelten Verlegetechniken: 3 bis 2026
                Reduktion der Verlegekosten: 20% bis 2026
                Zeitersparnis durch neue Techniken: 25% bis 2026
                """,
                    status: "In Umsetzung",
                    budget: 30,
                    leitung: "Dr. Sergej Blinow",
                    resources: "Ingenieure, Bauunternehmen, Forschungseinrichtungen",
                    milestones: """
                Startdatum: Januar 2023
                Q1 2023: Konzeptentwicklung und Planung abgeschlossen
                Q2 2023: Auswahl der Pilotregionen und Partnerunternehmen erfolgt
                Q4 2023: Beginn der ersten Pilotprojekte
                Q2 2024: Zwischenbericht und Anpassung der Methoden basierend auf ersten Ergebnissen
                Q4 2025: Abschluss der Pilotprojekte und umfassende Evaluierung erfolgt
                Q2 2026: Veröffentlichung der Ergebnisse und Empfehlungen für den breiten Einsatz durchgeführt
                Enddatum: Dezember 2026
                """,
                    risks: "Technologische Herausforderungen, Akzeptanzprobleme",
                    umfanganderungen: "Anpassungen basierend auf Pilotprojektergebnissen",
                    portfolio: subPortfolio
                );

                var project2 = Tools.CreateProject("KI Unterstützung Datendienste",
                    ziel: "Verbesserung der Datendienste durch den Einsatz von KI",
                    kpis: """
                Anzahl der implementierten KI-Modelle: 5 bis 2028
                Genauigkeit der KI-Modelle: 95% bis 2028
                Reduktion der Datenverarbeitungszeit: 30% bis 2028
                """,
                    status: "In Umsetzung",
                    budget: 25,
                    leitung: "Dr. Minh Nguyen",
                    resources: "KI-Experten, Datenanalysten, IT-Infrastruktur",
                    milestones: """
                Startdatum: Januar 2024
                Q1 2024: Bedarfsermittlung und Planung abgeschlossen
                Q3 2024: Entwicklung der ersten KI-Modelle und Algorithmen erfolgt
                Q1 2025: Implementierung der KI-Modelle in Testumgebungen durchgeführt
                Q4 2025: Erste Testphase und Evaluierung der Ergebnisse durchgeführt
                Q2 2026: Anpassung und Optimierung der KI-Modelle basierend auf Testergebnissen durchgeführt
                Q4 2026: Roll-out der optimierten KI-Modelle in Echtzeitumgebungen erfolgt
                Q4 2028: Abschlussbericht und Veröffentlichung der Ergebnisse
                Enddatum: Dezember 2028
                """,
                    risks: "Datenintegrationsprobleme, technische Herausforderungen",
                    umfanganderungen: "Anpassungen basierend auf Testergebnissen und Feedback",
                      portfolio: subPortfolio
                );


            }

            tr.Commit();
        }


    }



    public static void FillWeights()
    {
        var objectives = Database.Query<GoalEntity>().Select(a => new
        {
            a,
            Scope = a.Domain!,
            Parent =
            a.Domain.Entity is PortfolioEntity? ((PortfolioEntity)a.Domain.Entity).ParentPortfolio :
            a.Domain.Entity is ProgramEntity? ((ProgramEntity)a.Domain.Entity).Portfolio : 
            a.Domain.Entity is ProjectEntity? ((ProjectEntity)a.Domain.Entity).Mixin<ProjectPortfolioMixin>().ParentDomain : null
        }).ToList();
        var dic = objectives.ToDictionary(a => a.Scope);

        var tree = TreeHelper.ToTreeC(dic.Values, a => a.Parent == null ? null : dic.GetOrThrow(a.Parent));

        var tree2 = TreeHelper.SelectTree(tree, a => a.a).SingleEx();

        StringDistance sd = new StringDistance();

        AssignWeights(tree2, sd);
    }

    private static void AssignWeights(Node<GoalEntity> node, StringDistance sd)
    {
        if (node.Children.IsEmpty())
        {
            node.Value.Progress = Random.Shared.NextDecimal(0, 1).RoundTo(4);
            node.Value.Execute(GoalOperation.Save);
        }
        else
        {

            foreach (var ch in node.Children)
            {
                ch.Value.AppliesTo = node.Value.ToLite();
                ch.Value.Save();
                AssignWeights(ch, sd);
            }
        }
    }

    public static void GenerateStatusReportHistory()
    {

        var r = Random.Shared;
        foreach (var project in Database.Query<ProjectEntity>().Select(a => a.ToLite()))
        {  
            var budget = project.InDB(a => a.Budget());
          
            GenerateStatusReportHistory(r, project, budget);
        }

        foreach (var program in Database.Query<ProgramEntity>().Select(a => a.ToLite()))
        {
            var budget = program.InDB(a => a!.Budget());

            GenerateStatusReportHistory(r, program, budget);
        }

        foreach (var portfolio in Database.Query<PortfolioEntity>().Select(a => a.ToLite()))
        {
            var budget = portfolio.InDB(a => a.Budget());

            GenerateStatusReportHistory(r, portfolio, budget);
        }
    }

    private static void GenerateStatusReportHistory(Random r, Lite<IDomainEntity> domain, decimal budget)
    {
        var date = Clock.Today.AddMonths(-9);
        var budgetTime = budget / 1000 * 8;
        StatusReportEntity? psr = null;
        while (date <= Clock.Today && (psr == null || psr.OverallProgress.Progress < 1))
        {
            psr = new StatusReportEntity
            {
                Domain = domain,
                ReportDate = date,
                LastOverallProgress = new ProjectOverallProgressEmbedded
                {
                    Status = r.NextElement(Enum.GetValues<ProgressType>()),
                    Progress = psr?.OverallProgress.Progress ?? 0,
                    Comment = "Kommentar"
                },
                OverallProgress = new ProjectOverallProgressEmbedded
                {
                    Status = r.NextElement(Enum.GetValues<ProgressType>()),
                    Progress = Math.Min(psr?.OverallProgress.Progress ?? 0 + r.NextDecimal(0, 0.1m), 1),
                    Comment = "Kommentar"
                },
                Budget = GetKPI(r),
                Scope = GetKPI(r),
                TimeLine = GetKPI(r),
                Resources = GetKPI(r),
                RisksAndIssues = GetKPI(r),
                Dependencies = GetKPI(r),
                ConsumedTime = GetRandomTime(r, budgetTime, psr?.ConsumedTime),
                ConsumedCost = GetRandomCost(r, budget, psr?.ConsumedCost),
                TopRisksAndIssues =
                """
                    Verzögerung bei der Rückmeldung einiger Stakeholder
                    Unklare Anforderungen in der frühen Projektphase
                    Mangelnde Verfügbarkeit von Schlüsselressourcen
                    Technische Abhängigkeiten zwischen Modulen führen zu Blockaden
                    Fehlende oder verspätete Entscheidungen durch das Management
                    Änderungen im Projektumfang ohne ausreichende Abstimmung(Scope Creep)
                    Unzureichende Testabdeckung vor dem Go - Live
                    Kommunikationsprobleme zwischen den Projektteams
                    Lieferverzögerungen durch externe Dienstleister
                    Nicht berücksichtigte rechtliche oder regulatorische Anforderungen
                    """.Lines().Where(a => r.NextDouble() < 0.2)
                .Select(a => new RiskOrIssueEmbedded { RiskOrIssue = a, Type = RiskOrIssueType.Issue, MitigationMeasure = "Kommunikationsintervalle verkürzen" })
                .ToMList(),

                LastImportantActivities = (psr?.NextImportantActivities).EmptyIfNull().Select(a => new CompletedActivityEmbedded
                {
                    Activity = a.Activity,
                    CompletedOn = date.AddDays(-r.Next(0, 10))
                }).ToMList(),

                NextImportantActivities = """
                    Abstimmung mit den wichtigsten Stakeholdern zur finalen Anforderungsdefinition
                    Erstellung des Projektzeitplans mit allen Meilensteinen
                    Durchführung eines Kick-off-Meetings mit dem gesamten Projektteam
                    Analyse und Dokumentation technischer Abhängigkeiten
                    Festlegung der Teststrategie und Testumgebung
                    Vorbereitung und Durchführung eines Proof of Concept (PoC)
                    Einholung von Angeboten externer Dienstleister
                    Definition von Qualitätskennzahlen und Erfolgskriterien
                    Durchführung eines Risiko-Workshops zur Bewertung und Priorisierung
                    Erstellung und Abstimmung eines Kommunikationsplans
                    """.Lines().Where(a => r.NextDouble() < 0.2)
                .Select(a => new NextActivityEmbedded { Activity = a, TargetDate = date.AddDays(r.Next(0, 20)) }).ToMList(),

                RecentMilestones =
                """
                    Abnahme der finalen Anforderungen durch alle Stakeholder
                    Fertigstellung des technischen Architekturkonzepts
                    Abschluss der Entwicklungsphase des MVP (Minimum Viable Product)
                    Durchführung und Abschluss der Systemintegrationstests (SIT)
                    Go-Live der Testumgebung
                    Erfolgreiche Durchführung der Benutzerakzeptanztests (UAT)
                    Bereitstellung der finalen Produktivumgebung
                    Go-Live der Anwendung in der Produktion
                    Übergabe an den Betrieb / Support-Team
                    Projektabschluss und Lessons-Learned-Workshop
                    """.Lines().Where(a => r.NextDouble() < 0.2)
                .Select(a => new MilestoneEmbedded { Milestone = a, TargetDate = date.AddDays(r.Next(0, 20)), ActualDate = date.AddDays(r.Next(0, 20)) }).ToMList(),
            }.Save();

            date = date.AddDays(r.Next(25, 35));
        }

        psr!.SetReadonly(a => a.IsLast, true);
        psr!.Save();

        static ProjectProgressStatusEmbedded GetKPI(Random r)
        {
            return new ProjectProgressStatusEmbedded
            {
                LastStatus = r.NextElement(Enum.GetValues<ProgressType>()),
                Status = r.NextElement(Enum.GetValues<ProgressType>()),
                Comment = "Kommentar",
            };
        }


        ConsumedCostStatisticsEmbedded GetRandomCost(Random r, decimal maxValue, ConsumedCostStatisticsEmbedded? prev)
        {
            var consumed = (decimal)r.NextDecimal(0, 0.1m) * maxValue;
            return new ConsumedCostStatisticsEmbedded
            {
                MaxCost = maxValue,
                ConsumedCost = (prev?.ConsumedCost ?? 0) + consumed,
                BilledCost = (prev?.BilledCost ?? 0) + consumed,
            };
        }

        ConsumedTimeStatisticsEmbedded GetRandomTime(Random r, decimal maxValue, ConsumedTimeStatisticsEmbedded? prev)
        {
            var consumed = (decimal)r.NextDecimal(0, 0.1m) * maxValue;
            return new ConsumedTimeStatisticsEmbedded
            {
                MaxTime = maxValue,
                ConsumedTime = (prev?.ConsumedTime ?? 0) + consumed,
                BilledTime = (prev?.BilledTime ?? 0) + consumed,
            };
        }

    }


}
