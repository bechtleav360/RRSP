using Meros.PortfolioExt;
using Meros.PortfolioExt.InitiationRequest;
using Signum.Authorization;

namespace RRSP.Test.Environment;

public static class PortfolioGlobalConfigLoader
{
    public static void Clean()
    {
        Database.Query<InitiationRequestGlobalConfigurationEntity>().UnsafeDelete();
    }


    public static void PortfolioGlobalConfiguration()
    {
        new InitiationRequestGlobalConfigurationEntity
        {
            EvaluationDimensions =
            {
                new PortfolioEvaluationDimensionEntity
                {
                    Name = "Strategische Bedeutung",
                    Description = "Bewertet, wie gut die Initiative mit den strategischen Zielen übereinstimmt und zum langfristigen Wert der Organisation beiträgt, einschließlich der Relevanz im politischen und sozialen Kontext.",
                    Weight = 1,
                    EvaluationCriterias =
                    {
                        new PortfolioEvaluationCriteriaEntity { SubCategory = "Strategische Kriterien", Name = "Ausrichtung an strategischen Zielen", Weight = 1, Reverse = false },
                        new PortfolioEvaluationCriteriaEntity { SubCategory = "Strategische Kriterien", Name = "Beitrag zur Organisationsstrategie", Weight = 1, Reverse = false },
                        new PortfolioEvaluationCriteriaEntity { SubCategory = "Strategische Kriterien", Name = "Politische/administrative Relevanz", Weight = 1, Reverse = false },

                        new PortfolioEvaluationCriteriaEntity { SubCategory = "Wertschöpfungskriterien", Name = "Geschäftlicher, politischer, sozialer Nutzen", Weight = 1, Reverse = false },
                        new PortfolioEvaluationCriteriaEntity { SubCategory = "Wertschöpfungskriterien", Name = "Finanzielle Kennzahlen (NPV, Amortisationsdauer)", Weight = 1, Reverse = false },
                        new PortfolioEvaluationCriteriaEntity { SubCategory = "Wertschöpfungskriterien", Name = "Nicht-finanzielle Kennzahlen (Qualität, Effizienz)", Weight = 1, Reverse = false }
                    }
                },
                new PortfolioEvaluationDimensionEntity
                {
                    Name = "Dringlichkeit",
                    Description = "Bewertet die Dringlichkeit des Vorhabens anhand von externen Anforderungen, internem Druck, gesetzlichen Fristen und zeitkritischen Faktoren.",
                    Weight = 1,
                    EvaluationCriterias =
                    {
                        new PortfolioEvaluationCriteriaEntity { SubCategory = "Grad der Notwendigkeit und Dringlichkeit", Name = "Externe Nachfrage und Anforderungen", Weight = 1, Reverse = false },
                        new PortfolioEvaluationCriteriaEntity { SubCategory = "Grad der Notwendigkeit und Dringlichkeit", Name = "Interne Dringlichkeit", Weight = 1, Reverse = false },
                        new PortfolioEvaluationCriteriaEntity { SubCategory = "Grad der Notwendigkeit und Dringlichkeit", Name = "Zeitkritische Faktoren", Weight = 1, Reverse = false },
                        new PortfolioEvaluationCriteriaEntity { SubCategory = "Grad der Notwendigkeit und Dringlichkeit", Name = "Regulatorische Vorgaben", Weight = 1, Reverse = false }
                    }
                },
                new PortfolioEvaluationDimensionEntity
                {
                    Name = "Ressourcen und Machbarkeit",
                    Description = "Berücksichtigt den Bedarf an finanziellen und personellen Ressourcen, die Verfügbarkeit der erforderlichen Fähigkeiten und die derzeitigen Kapazitäten der Organisation zur Umsetzung der Initiative.",
                    Weight = 1,
                    EvaluationCriterias =
                    {
                        new PortfolioEvaluationCriteriaEntity { Name = "Finanzieller Ressourceneinsatz", Weight = 1, Reverse = true },
                        new PortfolioEvaluationCriteriaEntity { Name = "Personeller Ressourcenbedarf", Weight = 1, Reverse = true },
                        new PortfolioEvaluationCriteriaEntity { Name = "Verfügbarkeit benötigter Kompetenzen", Weight = 1, Reverse = false },
                        new PortfolioEvaluationCriteriaEntity { Name = "Kapazitätsauslastung", Weight = 1, Reverse = true }
                    }
                },
                new PortfolioEvaluationDimensionEntity
                {
                    Name = "Risikobewertung",
                    Description = "Analysiert potenzielle Risiken in Bezug auf Strategie, Umsetzung, Komplexität und Auswirkungen auf kritische Vorgänge und hilft dabei, die Stabilität der Initiative zu beurteilen.",
                    Weight = 1,
                    EvaluationCriterias =
                    {
                        new PortfolioEvaluationCriteriaEntity { Name = "Strategisches Risiko (Risikominderung vs. erhöhtes Risiko)", Weight = 1, Reverse = true },
                        new PortfolioEvaluationCriteriaEntity { Name = "Realisierungsrisiken (neue Technologie, fehlende Erfahrung)", Weight = 1, Reverse = true },
                        new PortfolioEvaluationCriteriaEntity { Name = "Auswirkung auf kritische Geschäftsprozesse", Weight = 1, Reverse = true },
                        new PortfolioEvaluationCriteriaEntity { Name = "Umsetzungskomplexität", Weight = 1, Reverse = true }
                    }
                },
                new PortfolioEvaluationDimensionEntity
                {
                    Name = "Portfolio-Integration",
                    Description = "Untersucht, wie sich die Initiative in die bestehende Projektlandschaft einfügt, einschließlich Synergien und Abhängigkeiten mit anderen Projekten.",
                    Weight = 1,
                    EvaluationCriterias =
                    {
                        new PortfolioEvaluationCriteriaEntity { Name = "Wechselseitige Abhängigkeiten zu anderen Projekten", Weight = 1, Reverse = true },
                        new PortfolioEvaluationCriteriaEntity { Name = "Synergieeffekte mit bestehenden Bestandteilen", Weight = 1, Reverse = false },
                        new PortfolioEvaluationCriteriaEntity { Name = "Auswirkungen bei Ausschluss auf andere Projekte", Weight = 1, Reverse = true }
                    }
                }
            }
        }.Save();
    }
}
