using RRSP.Test.React;
using Meros.Project;
using Meros.StatusReport;

namespace RRSP.Test.Entities;

public class ProjectStatusReportTest : RRSPTestClass
{

    [Fact]
    public void CreateProjectStatusReportTest()
    {
        Browse("System", b =>
        {
            b.SearchPage(typeof(StatusReportEntity)).EndUsing(psrs =>
            {
                psrs.SearchControl.CreateButton.Find().CaptureOnClick().AsSearchModal().Using(projects =>
                {
                    return Selenium.CapturePopup(() => projects.SelectByPosition(0)).AsFrameModal<StatusReportEntity>();
                }).EndUsing(psr =>
                {
                    var op = psr.SubContainer(p => p.OverallProgress);                    
                    op.AutoLineValue(m => m.Progress, (decimal)0.5);
                    op.EnumLineValue(m => m.Status, ProgressType.Warning);
                    op.AutoLineValue(m => m.Comment, "comment for overall progress");
                    psr.Execute(StatusReportOperation.Save);
                });
            });
        });
    }
}
