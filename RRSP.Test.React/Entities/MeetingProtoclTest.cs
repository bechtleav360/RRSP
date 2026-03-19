using RRSP.Globals;
using RRSP.Project;
using RRSP.Test.React;
using Meros.Project;
using Meros.Protocol;
using Meros.Tasks;
using Microsoft.AspNetCore.Routing;
using Signum.Basics;
using Signum.Cache;
using Signum.Utilities.Reflection;
using System.Threading;


namespace RRSP.Test.Entities;

public class MeetingProtocolTest : RRSPTestClass
{

    [Fact]
    public void CreateMeetingProtocolTest()
    {
        Browse("System", b =>
        {
            b.SearchPage(typeof(MeetingProtocolEntity)).EndUsing(mps =>
            {
                mps.SearchControl.CreateButton.Find().CaptureOnClick().AsSearchModal().Using(projects =>
                {
                    return Selenium.CapturePopup(() => projects.SelectByPosition(0)).AsSelectorModal();
                }).Using(selector =>
                {
                    var btn = selector.ButtonNames().FirstEx();
                    return selector.SelectAndCapture(btn).AsFrameModal<MeetingProtocolEntity>();
                }).EndUsing(mp =>
                {
                    mp.AutoLineValue(m => m.Title, "Test meeting protocol");
                    mp.TimeLineValue(m => m.StartTime, new TimeOnly(9, 0));
                    mp.TimeLineValue(m => m.EndTime, new TimeOnly(10, 0));

                    mp.EntityTable(m => m.Agenda).CreateRow<AgendaPointEmbedded>().Do(row =>
                    {
                        row.HtmlLineValue(a => a.Description, "first agenda description");
                    });

                    mp.Execute(MeetingProtocolOperation.Save);

                    var ppTable = new EntityTableProxy(mp.Element.WaitElementPresent(By.CssSelector("[data-property-path='ProtocolPoints']")), PropertyRoute.Construct((ProtocolModel p) => p.ProtocolPoints));
                    
                    ppTable.CreateRow<ProtocolPointEntity>().Do(row =>
                    {
                        row.NumberLineValue(m => m.AgendaIndex, 1);
                        row.HtmlLineValue(pp => pp.Description, "first pp description", true);
                    });
                });
            });
        });
    }

    [Fact]
    public void CreateProtocolPointTest()
    {
        Browse("System", b =>
        {
            b.SearchPage(typeof(ProtocolPointEntity)).EndUsing(mps =>
            {
                mps.SearchControl.CreateButton.Find().CaptureOnClick().AsSearchModal().Using(projects =>
                {
                    return Selenium.CapturePopup(() => projects.SelectByPosition(0)).AsFrameModal<ProtocolPointEntity>();
                }).EndUsing(pp =>
                {
                    pp.EntityCombo(m => m.MeetingType).SelectLabel("Team-Chat");
                    pp.HtmlLineValue(m => m.Description, "New Protocol Point from test");

                    pp.Execute(ProtocolPointOperation.Save);
                });
            });
        });
    }
}
