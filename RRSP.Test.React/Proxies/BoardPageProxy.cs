using Meros.Tasks;

namespace RRSP.Test.React;

public class BoardPageProxy : IDisposable
{
    public readonly WebDriver Selenium;

    public BoardPageProxy(WebDriver selenium)
    {
        this.Selenium = selenium;
    }

    public void Dispose()
    {
    }

    internal void DragAndDropTask(TaskEntity taskToDrag, Lite<ColumnEntity> done)
    {
        var taskElement = Selenium.WaitElementPresent(By.CssSelector($"div[data-rbd-draggable-id='{taskToDrag.Id}']>span"));

        var columnElement = Selenium.WaitElementPresent(By.CssSelector($"div[data-rbd-droppable-id='{done.Id}']"));

        Actions builder = new Actions(Selenium);

        builder.ClickAndHold(taskElement)
        .MoveByOffset(-10, 0)
        .MoveToElement(columnElement)
        .MoveByOffset(-10, 0)
        .Release()
        .Build()
        .Perform();
    }
}
