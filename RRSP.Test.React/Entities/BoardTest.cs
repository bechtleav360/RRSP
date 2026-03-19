using RRSP.Test.React;
using Meros.Tasks;

namespace RRSP.Test.Entities;

public class BoardTest : RRSPTestClass
{
    public string Url(string url)
    {
        return RRSPTestClass.BaseUrl + url;
    }

    [Fact]
    public void MoveTaskInBoardColumns()
    {
        var board = Database.Query<BoardEntity>().Where(bd => bd.Name == "Maverick Backlog").SingleEx();
        var taskToDrag = CreateTask("Task to move in board");

        var done = board.Columns.SingleEx(t => t.ToString() == "Done").ToLite();

        Browse("System", b =>
        {
            b.Board(board)
             .EndUsing(b =>
             {
                 b.DragAndDropTask(taskToDrag, done);

                 Selenium.Wait(() => taskToDrag.InDB(a => a.Column.Is(done)));
             });
        });
    }

    [Fact]
    public void DeleteTask()
    {
        var taskToDelete = CreateTask("Test Task to delete", false);

        Browse("System", b =>
        {
            b.Task(taskToDelete).EndUsing(t => t.deleteTask());
        });
    }


    [Fact]
    public void ArchiveTask()
    {
        var taskToArchive = CreateTask("Test Task to archive");

        Browse("System", b =>
        {
            b.Task(taskToArchive)
            .EndUsing(b =>
            {
                b.archiveTask();
            });
        });
    }


    [Fact]
    public void MigrateTask()
    {       
        var taskToMigrate = CreateTask("Test Task to migrate");

        Browse("System", b =>
        {
            b.Task(taskToMigrate)
            .EndUsing(b =>
            {
                b.migrateTask();
            });
        });
    }


    [Fact]
    public void DeactivateReactivate()
    {
        var board = Database.Query<BoardEntity>().Where(bd => bd.Name == "Maverick Backlog").SingleEx();

        Browse("System", b =>
        {
            b.ViewBoard(board).EndUsing(b =>
            {
                var button = Selenium.WaitElementPresent(By.CssSelector($"button[data-operation='{BoardOperation.Discard.Symbol}']"));

                button.Click();

                button = Selenium.WaitElementPresent(By.CssSelector($"button[data-operation='{BoardOperation.Reactivate.Symbol}']"));

                button.Click();
            });
        });
    }
}
