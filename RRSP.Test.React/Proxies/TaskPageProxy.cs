using Meros.Tasks;

namespace RRSP.Test.React;


public class TaskPageProxy : IDisposable
{
    public readonly WebDriver Selenium;

    public TaskPageProxy(WebDriver selenium)
    {
        this.Selenium = selenium;
    }


    public void Dispose()
    {
    }


    internal void deleteTask()
    {
        var deleteButtonDropDown = Selenium.WaitElementPresent(By.CssSelector("div[data-key='Delete']"));

        deleteButtonDropDown.Click();

        var deleteButton = Selenium.WaitElementPresent(By.CssSelector($"a[data-operation='{TaskOperation.Delete.Symbol}']"));

        deleteButton.Click();
    }


    internal void archiveTask()
    {
        var archiveButton = Selenium.WaitElementPresent(By.CssSelector("button[data-operation='TaskOperation.ArchiveDiscard']"));

        archiveButton.Click();
    }


    internal void migrateTask()
    {
        var migrateButton = Selenium.WaitElementPresent(By.CssSelector("div[data-key='Migrate']"));

        migrateButton.Click();

        var existenLink = Selenium.WaitElementPresent(By.CssSelector("a[data-operation='TaskOperation.MigrateToExistingProject']"));

        existenLink.Click();

        var checkbox = Selenium.WaitElementPresent(By.CssSelector("input[data-index='0']"));
        checkbox.SafeClick();

        var okButton = Selenium.WaitElementPresent(By.CssSelector("button[class='btn btn-light sf-entity-button sf-close-button sf-cancel-button']"));

        okButton.Click();
    }
}
