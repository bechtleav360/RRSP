namespace RRSP.Test.React;


public class WorklogPageProxy : IDisposable
{
    public readonly WebDriver Selenium;

    public WorklogPageProxy(WebDriver selenium)
    {
        this.Selenium = selenium;
    }


    public void Dispose()
    {
    }


    public void DeleteWorklog()
    {
        var archiveButton = Selenium.WaitElementPresent(By.CssSelector("button[data-operation='WorkLogOperation.Delete']"));

        archiveButton.Click();

        var yesButton = Selenium.WaitElementPresent(By.CssSelector("button[name='yes']"));

        yesButton.Click();
    }
}
