using RRSP.Globals;
using RRSP.Project;
using RRSP.Test.React;
using Meros.Project;
using Meros.Tasks;
using Signum.Authorization;
using Signum.Basics;
using Signum.Cache;
using Signum.DynamicQuery;


namespace RRSP.Test.Entities;

public class ProjectTest : RRSPTestClass
{
   


    [Fact]
    public void EditProjectTest()
    {
        var project = CreateProject("Edit project test");

        Browse("System", b =>
        {
            b.SearchPage(typeof(ProjectEntity)).EndUsing(prjs =>
            {
                prjs.Results.EntityClick(project.ToLite()).EndUsing(prj =>
                {
                    prj.AutoLineValue(m => m.Name, "Test 333");

                    prj.SelectTab(ProjectTab.Execution.ToString());
                    prj.Element.FindElement(By.LinkText(typeof(MemberEntity).NicePluralName())).Click();
                    prj.GetSearchControl(typeof(MemberEntity)).CreateButton.Find().CaptureOnClick().AsSearchModal().Using(us =>
                    {
                        us.Results.SelectRow(0);
                        return us.OkButton.Find().CaptureOnClick().AsFrameModal<MemberEntity>();
                    }).EndUsing(pm => 
                    {
                        pm.Execute(MemberOperation.Save);                    
                    });
                    prj.SelectTab(ProjectTab.Execution.ToString());
                    prj.Element.FindElement(By.LinkText("Basic")).Click();
                    prj.EntityStrip(m => m.Mixin<DomainTaskMixin>().LabelTypes).AutoComplete(Database.Query<TaskLabelTypeEntity>().Select(u => u.ToLite()).Take(1).SingleEx());
                    prj.Execute(ProjectOperation.Save);
                });
            });
        });
    }


    [Fact]
    public void AddBoard()
    {
        var project = CreateProject("Add board project");

        Browse("System", b =>
        {
            b.SearchPage(typeof(ProjectEntity)).EndUsing(prjs =>
            {
                prjs.Results.EntityClick(project.ToLite()).EndUsing(prj =>
                {
                    prj.SelectTab(ProjectTab.Execution.ToString());
                    prj.Element.WaitElementVisible(By.LinkText("Basic")).Click();
                    prj.GetSearchValueLine(typeof(BoardEntity)).Create<BoardEntity>().EndUsing(board =>
                    {
                        board.AutoLineValue(b => b.Name, "New Board created during the Test");
                        board.EntityStrip(b => b.Columns).CreateModal<ColumnEntity>().EndUsing(c =>
                        {
                            c.AutoLineValue(cl => cl.Name, "New Column for Test");
                            c.OperationClick(ColumnOperation.Save);
                        });
                        board.OperationClick(BoardOperation.Save);
                    });
                    prj.Close();
                });
            });
        });
    }



    [Fact]
    public void DeactivateActivateProject()
    {
        var maverick = Database.Query<ProjectEntity>().Where(p => p.Name == "Maverick").SingleEx();
        Browse("System", b =>
        {
            b.FramePage(maverick.ToLite()).EndUsing(prj =>
            {
                prj.Execute(ProjectOperation.Archive, consumeAlert: true);
                prj.Execute(ProjectOperation.Reactivate);                
            });
        });
    }
}

public static class TestMethods
{
    public static DoubleButtonProxy DoubleButton(this ILineContainer lineContainer, object queryName)
    {
        var doubleButton = lineContainer.Element.WaitElementVisible(By.CssSelector("div.double-counter[data-query=" + QueryLogic.GetQueryEntity(queryName).Key + "]"));

        return new DoubleButtonProxy(doubleButton);
    }
}

public class DoubleButtonProxy
{
    public readonly IWebElement Element;

    public DoubleButtonProxy(IWebElement doubleButton)
    {
        this.Element = doubleButton;
    }

    internal void CreateClick()
    {
        this.Element.FindElement(By.CssSelector("a.sf-create")).Click();
    }
}
