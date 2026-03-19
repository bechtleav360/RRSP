using System.ComponentModel;

namespace RRSP.Project;

public enum DashboardTemplateMessage
{
    //GoalPart
    NumberOfProjects,
    Progress,

    //ProjectMainPart
    ProjectOwner,
    Runtime,
    [Description("Monitoring & Control")]
    MonitoringAndControl,
    ContinuousProjectMonitoringActive,
    NoMonitoring,
    [Description("Project progress (weighted)")]
    ProjectProgressWeighted,
    NoStandardPlan,
    [Description("{0} of {1} work packages completed")]
    _0Of1WorkPackagesCompleted,
    KanbanBoard,
    [Description("of {0} total budget")]
    Of0TotalBudget,
    BudgetExhausted,
    Available,
}
