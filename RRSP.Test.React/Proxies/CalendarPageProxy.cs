using Signum.Authorization;

namespace RRSP.Test.React;

public class CalendarPageProxy : IDisposable
{
    public readonly WebDriver Selenium;

    public CalendarPageProxy(WebDriver selenium)
    {
        this.Selenium = selenium;
    }

    public void Dispose()
    {
    }

    public DayCellProxy GetDay(DateOnly dt)
    {
        var tableElement = Selenium.WaitElementPresent(By.ClassName("calendar-table"));

        var day = dt.ToString("o");
        return new DayCellProxy(tableElement.FindElements(By.CssSelector("td.calendar-table-cell")).SingleEx(a => a.GetDomAttribute("data-day") == day));
    }
}

public class DayCellProxy
{
    public readonly IWebElement WebElement;

    public DayCellProxy(IWebElement webElement)
    {
        this.WebElement = webElement;
    }

    internal IWebElement CreateWorkLogButton()
    {
        return WebElement.FindElement(By.CssSelector(".btn.btn-xs.btn-primary.calendar-create-hour"));
    }

    internal WebElementLocator GetLockIcon(bool isOpen)
    {
        return new WebElementLocator(WebElement, By.CssSelector(@$"svg[data-icon='{(isOpen? "lock-open" : "lock")}']"));
    }
}
