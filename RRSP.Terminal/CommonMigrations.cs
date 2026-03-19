using Meros.PlanningProject.PSC;
using Meros.PlanningProject.TaskManagement;
using Meros.PlanningProject.WorkPackage;
using Meros.PortfolioExt.InitiationRequest;
using Meros.Project;
using Meros.Project.Portfolio;
using Meros.Project.Program;
using Meros.Protocol;
using Meros.Risk;
using Meros.StatusReport;
using Signum.Authorization;
using Signum.Basics;
using Signum.Engine.Sync;
using Signum.Translation.Instances;
using System.IO;

namespace RRSP.Terminal;

public static class CommonMigrations
{

    public static void MoveGermanTranslations()
    {
        using (var tr = new Transaction())
        {
            using (CultureInfoUtils.ChangeBothCultures("de"))
            {
                {
                    var prName = PropertyRoute.Construct((PortfolioEvaluationCriteriaEntity a) => a.Name)!;
                    var prDescription = PropertyRoute.Construct((PortfolioEvaluationCriteriaEntity a) => a.Description)!;
                    var prSubCategory = PropertyRoute.Construct((PortfolioEvaluationCriteriaEntity a) => a.SubCategory)!;

                    Database.Query<PortfolioEvaluationCriteriaEntity>().UnsafeUpdate()
                        .Set(a => a.Name, a => PropertyRouteTranslationLogic.TranslatedField(a.ToLite(), prName, null))
                        .Set(a => a.Description, a => PropertyRouteTranslationLogic.TranslatedField(a.ToLite(), prDescription, null))
                        .Set(a => a.SubCategory, a => PropertyRouteTranslationLogic.TranslatedField(a.ToLite(), prSubCategory, null))
                        .Execute();
                }

                {
                    var prName = PropertyRoute.Construct((PortfolioEvaluationDimensionEntity a) => a.Name)!;
                    var prDescription = PropertyRoute.Construct((PortfolioEvaluationDimensionEntity a) => a.Description)!;
                    Database.Query<PortfolioEvaluationDimensionEntity>().UnsafeUpdate()
                        .Set(a => a.Name, a => PropertyRouteTranslationLogic.TranslatedField(a.ToLite(), prName, null))
                        .Set(a => a.Description, a => PropertyRouteTranslationLogic.TranslatedField(a.ToLite(), prDescription, null))
                        .Execute();
                }
            }

            tr.Commit();
        }
    }

    public static void LoadDomainParent()
    {
        Database.Query<ProjectEntity>().Where(p => p.Mixin<ProjectPortfolioMixin>().ParentDomain != null)
            .Select(proj => new DomainParentEntity
            {
                Child = proj.ToLite(),
                Parent = proj.Mixin<ProjectPortfolioMixin>().ParentDomain!
            }).BulkInsert();

        Database.Query<ProgramEntity>().Where(prog => prog.Portfolio != null).Select(prog => new DomainParentEntity
        {
            Child = prog.ToLite(),
            Parent = (Lite<IDomainEntity>)prog.Portfolio!
        }).BulkInsert();

        Database.Query<PortfolioEntity>().Where(port => port.ParentPortfolio != null).Select(port => new DomainParentEntity
        {
            Child = port.ToLite(),
            Parent = (Lite<IDomainEntity>)port.ParentPortfolio!
        }).BulkInsert();
    }

    public static void MapProjectMemberRole()
    {
    }

    public static void ImportTranslations()
    {
        TranslatedInstanceLogic.ImportExcelFile(Path.Combine(@"InstanceTranslations", "PortfolioEvaluationCriteria.en.View.xlsx"), MatchTranslatedInstances.ByOriginalText);
        TranslatedInstanceLogic.ImportExcelFile(Path.Combine(@"InstanceTranslations", "PortfolioEvaluationDimension.en.View.xlsx"), MatchTranslatedInstances.ByOriginalText);
    }


    public static void CreateRiskCategory()
    {
        using (var tr = new Transaction())
        {
            var risk = new RiskCategoryTableEntity
            {
                Name = "Default risk",
                ProbabilityRanges =
                [
                    new RiskRangeEmbedded { MinValue = 0, MaxValue = 0.05m, },
                    new RiskRangeEmbedded { MinValue = 0.06m, MaxValue = 0.2m, },
                    new RiskRangeEmbedded { MinValue = 0.21m, MaxValue = 0.55m, },
                    new RiskRangeEmbedded { MinValue = 0.56m, MaxValue = 0.85m, },
                    new RiskRangeEmbedded { MinValue = 0.86m, MaxValue = 1, },
                ],
                ImpactRanges =
                [
                    new RiskRangeEmbedded { MinValue = 0, MaxValue = 0.05m, },
                    new RiskRangeEmbedded { MinValue = 0.06m, MaxValue = 0.1m, },
                    new RiskRangeEmbedded { MinValue = 0.11m, MaxValue = 0.30m, },
                    new RiskRangeEmbedded { MinValue = 0.31m, MaxValue = 0.6m, },
                    new RiskRangeEmbedded { MinValue = 0.61m, MaxValue = 1, },
                ],
                Categories =
                [
                    new RiskCategoryEntity { Name = "Kleines Risiko", Color = "green" },
                    new RiskCategoryEntity { Name = "Mittleres Risiko", Color = "yellow" },
                    new RiskCategoryEntity { Name = "Großes Risiko", Color = "orange" },
                    new RiskCategoryEntity { Name = "Extremes Risiko", Color = "red" },
                ]
            }.Save();

            CalculateCells(risk);


            var chance = new RiskCategoryTableEntity
            {
                Name = "Default chance",
                ProbabilityRanges =
                [
                    new RiskRangeEmbedded { MinValue = 0, MaxValue = 0.05m, },
                    new RiskRangeEmbedded { MinValue = 0.06m, MaxValue = 0.2m, },
                    new RiskRangeEmbedded { MinValue = 0.21m, MaxValue = 0.55m, },
                    new RiskRangeEmbedded { MinValue = 0.56m, MaxValue = 0.85m, },
                    new RiskRangeEmbedded { MinValue = 0.86m, MaxValue = 1, },
                ],
                ImpactRanges =
                [
                    new RiskRangeEmbedded { MinValue = 0, MaxValue = 0.05m, },
                    new RiskRangeEmbedded { MinValue = 0.06m, MaxValue = 0.1m, },
                    new RiskRangeEmbedded { MinValue = 0.11m, MaxValue = 0.30m, },
                    new RiskRangeEmbedded { MinValue = 0.31m, MaxValue = 0.6m, },
                    new RiskRangeEmbedded { MinValue = 0.61m, MaxValue = 1, },
                ],
                Categories =
                [
                    new RiskCategoryEntity { Name = "Kleines Chance", Color = "red" },
                    new RiskCategoryEntity { Name = "Mittleres Chance", Color = "orange" },
                    new RiskCategoryEntity { Name = "Großes Chance", Color = "yellow" },
                    new RiskCategoryEntity { Name = "Extremes Chance", Color = "green" },
                ]
            }.Save();

            CalculateCells(chance);

            var projects = Database.Query<ProjectEntity>().ToList();
            var user = Database.Query<UserEntity>().First();
            foreach (var project in projects)
            {
                project.Mixin<DomainRiskMixin>().RiskManagement = new RiskManagementEmbedded
                {
                    RiskManager = project.Manager ?? user.ToLite(),
                    OverallValue = project.Budget(),
                    RiskCategoryTable = risk.ToLite(),
                    ChanceCategoryTable = chance.ToLite()
                };
                project.Save();
            }
            tr.Commit();
        }

        static void CalculateCells(RiskCategoryTableEntity riskCategory)
        {
            var maxPro = riskCategory.ProbabilityRanges.Count - 1;
            var maxImp = riskCategory.ImpactRanges.Count - 1;
            var maxCat = riskCategory.Categories.Count - 1;

            riskCategory.Cells = [];
            riskCategory.ProbabilityRanges.OrderByDescending(prob => prob.MaxValue).ForEach((pro, pi) =>
                  riskCategory.ImpactRanges.ForEach((ir, ii) =>
                  {
                      var reverseIndex = riskCategory.ProbabilityRanges.Count - 1 - pi;

                      riskCategory.Cells.Add(new RiskCellEmbedded
                      {
                          Probability = reverseIndex,
                          Impact = ii,
                          Category = (int)Math.Floor((reverseIndex + ii) / (decimal)(maxPro + maxImp) * maxCat),
                      });
                  }));
            riskCategory.Save();
        }

    }

    public static void CreateProtocolMasterData()
    {
        new ProtocolPointTypeEntity { Abbreviation = "A", Name = "Aufgabe" }.SetMixin((ProtocolPointTypeTaskMixin pptm) => pptm.CreateTask, true).Save();
        new ProtocolPointTypeEntity { Abbreviation = "D", Name = "Dokumentation" }.Save();
        new ProtocolPointTypeEntity { Abbreviation = "E", Name = "Entscheidung" }.Save();
        new ProtocolPointTypeEntity { Abbreviation = "I", Name = "Information", IsDefault = true }.Save();
        new ProtocolPointTypeEntity { Abbreviation = "F", Name = "Frage" }.Save();
        new ProtocolPointTypeEntity { Abbreviation = "T", Name = "Termin" }.Save();

        new MeetingTypeEntity { Name = "Auftraggeber - Jourfixe" }.Save();
        new MeetingTypeEntity { Name = "Projektbesprechung" }.Save();
        new MeetingTypeEntity { Name = "Lenkungsausschuss" }.Save();
        new MeetingTypeEntity { Name = "Technische Besprechung" }.Save();
        new MeetingTypeEntity { Name = "Sonstige Besprechung" }.Save();
        new MeetingTypeEntity { Name = "Telefongespräch", ForProtocolPoint = true }.Save();
        new MeetingTypeEntity { Name = "Team - Chat", ForProtocolPoint = true }.Save();
        new MeetingTypeEntity { Name = "E - Mail", ForProtocolPoint = true }.Save();

        new ProtocolPointDecisionEntity { Name = "Keine" }.Save();
        new ProtocolPointDecisionEntity { Name = "Entschieden" }.Save();
        new ProtocolPointDecisionEntity { Name = "Ausstehend Kunde" }.Save();
        new ProtocolPointDecisionEntity { Name = "Ausstehend Intern" }.Save();
    }

    public static void CreateEstimationSizes()
    {
        new EstimationSizeEntity { Name = "XS", Min = 1, Max = 3 }.Save();
        new EstimationSizeEntity { Name = "S", Min = 4, Max = 10 }.Save();
        new EstimationSizeEntity { Name = "M", Min = 11, Max = 25 }.Save();
        new EstimationSizeEntity { Name = "L", Min = 26, Max = 50 }.Save();
        new EstimationSizeEntity { Name = "XL", Min = 51, Max = 100 }.Save();
        new EstimationSizeEntity { Name = "XXL", Min = 101, Max = 200 }.Save();
    }

    public static void CreateArtifacts()
    {
        using (var tr = new Transaction())
        {
            var initiierung = new ProjectArtifactGroupEntity { Name = "Initiierung" }.Save();
            var planung = new ProjectArtifactGroupEntity { Name = "Planung" }.Save();
            var durchfuehrung = new ProjectArtifactGroupEntity { Name = "Durchführung" }.Save();
            var ueberwachungSteuerung = new ProjectArtifactGroupEntity { Name = "Überwachung & Steuerung" }.Save();
            var abschluss = new ProjectArtifactGroupEntity { Name = "Abschluss" }.Save();
            var zusaetzlicheArtefakte = new ProjectArtifactGroupEntity { Name = "Zusätzliche/erweiterte Artefakte" }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = initiierung.ToLite(),
                Artifact = "Projektinitiierungsantrag",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium, ProjectClassificationType.Small]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = initiierung.ToLite(),
                Artifact = "Business Case",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = initiierung.ToLite(),
                Artifact = "Projektauftrag",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium, ProjectClassificationType.Small]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = planung.ToLite(),
                Artifact = "Projekthandbuch",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Small, ProjectClassificationType.Medium]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = planung.ToLite(),
                Artifact = "Projektstakeholder-Matrix",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = planung.ToLite(),
                Artifact = "Projektarbeitsplan",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = planung.ToLite(),
                Artifact = "Outsourcing-Plan",
                ClassificationTypes = [ProjectClassificationType.Medium, ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = planung.ToLite(),
                Artifact = "Liefergegenstandabnahmeplan",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = planung.ToLite(),
                Artifact = "Einführungsplan",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = planung.ToLite(),
                Artifact = "Geschäftsimplementierungsplan",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = planung.ToLite(),
                Artifact = "Anforderungsmanagementplan",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = planung.ToLite(),
                Artifact = "Änderungsmanagementplan",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = planung.ToLite(),
                Artifact = "Risikomanagementplan",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = planung.ToLite(),
                Artifact = "Problemmanagementplan",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = planung.ToLite(),
                Artifact = "Qualitätsmanagementplan",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = planung.ToLite(),
                Artifact = "Kommunikationsmanagementplan",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = durchfuehrung.ToLite(),
                Artifact = "Sitzungsagenda",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = durchfuehrung.ToLite(),
                Artifact = "Sitzungsprotokoll",
                ClassificationTypes = [ProjectClassificationType.Medium, ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = durchfuehrung.ToLite(),
                Artifact = "Projektstatusbericht",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Small, ProjectClassificationType.Medium]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = durchfuehrung.ToLite(),
                Artifact = "Änderungsantrag",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium, ProjectClassificationType.Small]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = durchfuehrung.ToLite(),
                Artifact = "Projektfortschrittsbericht",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = durchfuehrung.ToLite(),
                Artifact = "Liefergegenstandsabnahmevermerk",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = ueberwachungSteuerung.ToLite(),
                Artifact = "Risikoprotokoll",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = ueberwachungSteuerung.ToLite(),
                Artifact = "Problemprotokoll",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = ueberwachungSteuerung.ToLite(),
                Artifact = "Entscheidungsprotokoll",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = ueberwachungSteuerung.ToLite(),
                Artifact = "Änderungsprotokoll",
                ClassificationTypes = [ProjectClassificationType.Medium, ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = ueberwachungSteuerung.ToLite(),
                Artifact = "RAID-Protokoll",
                ClassificationTypes = [ProjectClassificationType.Small]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = ueberwachungSteuerung.ToLite(),
                Artifact = "Qualitätsprüfungsprotokoll",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = ueberwachungSteuerung.ToLite(),
                Artifact = "Liefergegenstandsabnahmeprotokoll",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = ueberwachungSteuerung.ToLite(),
                Artifact = "Checkliste Phasenabschlussprüfung",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium, ProjectClassificationType.Small]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = ueberwachungSteuerung.ToLite(),
                Artifact = "S-O-S-Projektkompass",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium, ProjectClassificationType.Small]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = abschluss.ToLite(),
                Artifact = "Projektabschlussbericht",
                ClassificationTypes = [ProjectClassificationType.Large, ProjectClassificationType.Medium, ProjectClassificationType.Small]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = zusaetzlicheArtefakte.ToLite(),
                Artifact = "Erweiterte Stakeholder-Analyse",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = zusaetzlicheArtefakte.ToLite(),
                Artifact = "Detaillierte Risikobewertung",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = zusaetzlicheArtefakte.ToLite(),
                Artifact = "Umfassende Kommunikationspläne",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = zusaetzlicheArtefakte.ToLite(),
                Artifact = "Spezielle Change Management Dokumentation",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = zusaetzlicheArtefakte.ToLite(),
                Artifact = "Projektbüro-Dokumentation",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = zusaetzlicheArtefakte.ToLite(),
                Artifact = "Teilprojekt-Koordinationspläne",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            new ProjectArtifactEntity
            {
                ArtifactGroup = zusaetzlicheArtefakte.ToLite(),
                Artifact = "Erweiterte Qualitätssicherungsprozesse",
                ClassificationTypes = [ProjectClassificationType.Large]
            }.Save();

            tr.Commit();
        }

    }

    public static void CreatePlanLevelColor()
    {
        string GetDefaultColor(PlanLevel level) => level switch
        {
            PlanLevel.OrganizationalPortfolio => "#1F77B4",
            PlanLevel.Portfolio => "#17BECF",
            PlanLevel.Program => "#2CA02C",
            PlanLevel.Project => "#98DF8A",
            PlanLevel.Epic => "#FF7F0E",
            PlanLevel.UseCase => "#FFBB78",
            PlanLevel.Task => "#9467BD",
            PlanLevel.SubTask => "#C5B0D5",
            _ => "#CCCCCC"
        };

        foreach (PlanLevel level in Enum.GetValues(typeof(PlanLevel)))
        {
            if (!Database.Query<PlanLevelColorConfigEntity>().Any(a => a.Level == level))
            {
                new PlanLevelColorConfigEntity
                {
                    ColorHex = GetDefaultColor(level)
                }.SetReadonly(a => a.Level, level).Save();
            }
        }
    }
    public static void UpdatePlanLevelColor()
    {
        string GetDefaultColor(PlanLevel level) => level switch
        {
            PlanLevel.OrganizationalPortfolio => "#007194",
            PlanLevel.Portfolio => "#66AABF",
            PlanLevel.Program => "#00854A",
            PlanLevel.Project => "#C1CA31",
            PlanLevel.Epic => "#F7BB3D",
            PlanLevel.UseCase => "#F9E03A",
            PlanLevel.Task => "#576164",
            PlanLevel.SubTask => "#BEC5C9",
            _ => "#CCCCCC"
        };

        foreach (PlanLevel level in Enum.GetValues(typeof(PlanLevel)))
        {
            if (!Database.Query<PlanLevelColorConfigEntity>().Any(a => a.Level == level))
            {
                new PlanLevelColorConfigEntity
                {
                    ColorHex = GetDefaultColor(level)
                }.SetReadonly(a => a.Level, level).Save();
            }
        }
    }
}
