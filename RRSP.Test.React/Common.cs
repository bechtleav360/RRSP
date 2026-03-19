using System.Globalization;
using System.Threading;
using OpenQA.Selenium.Chrome;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using Meros.Tasks;
using Meros.Project;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;
using Signum.Cache;
using OpenQA.Selenium.Support.Extensions;
using Signum.Entities.Reflection;
using Signum.Authorization;
using System.Net.Http.Json;
using System.Net.Http;
using Signum.Utilities.Synchronization;
using Microsoft.SqlServer.Types;

namespace RRSP.Test.React;

public class RRSPTestClass
{
    public static string BaseUrl { get; private set; }

    public static WebDriver Selenium { get; set; } = null!;

    static RRSPTestClass()
    {
        var config = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.json")
             .AddJsonFile($"appsettings.{System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
             .AddUserSecrets(typeof(RRSPTestClass).Assembly)
             .Build();

        BaseUrl = config["Url"]!;
        RRSPEnvironment.StartAndInitialize();

        AuthLogic.GloballyEnabled = false;

        Administrator.RestoreSnapshotOrDatabase();

        using (var c = new HttpClient())
        {
            var response = c.PostAsync(RRSPTestClass.BaseUrl + "/api/clearAllBlocks", null).ResultSafe();
            response.EnsureSuccessStatusCode();

            if (!CacheReseted && RRSPEnvironment.BroadcastSecretHash != null)
            {
                var response2 = c.PostAsync(RRSPTestClass.BaseUrl + "/api/cache/invalidateAll", JsonContent.Create(new
                {
                    SecretHash = RRSPEnvironment.BroadcastSecretHash,
                })).ResultSafe();
                response2.EnsureSuccessStatusCode();
                CacheReseted = true;
            }
        };
    }

    static bool CacheReseted = false;

    public static void Browse(string username, Action<RRSPBrowser> action)
    {
        var options = new ChromeOptions();
        options.AddExtension(@"..\..\..\React-Developer-Tools.crx");
        options.AddArgument("--start-maximized");
        options.AddArgument("--disable-search-engine-choice-screen");
        options.AddUserProfilePreference("profile.default_content_setting_values.automatic_downloads", 1);
        options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);

        options.AddArgument("--no-first-run");
        options.AddArgument("--no-default-browser-check");
        options.AddArgument("--disable-popup-blocking");
        options.AddArgument("--disable-infobars");

        var driverManager = new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);

        var selenium = new ChromeDriver(options);

        Selenium = selenium;

        var browser = new RRSPBrowser(selenium);

        try
        {
            browser.Login(username, username);
            action(browser);
        }
        catch (UnhandledAlertException)
        {
            selenium.SwitchTo().Alert();
        }
        catch (WebDriverException e)
        {
            var screenShot = selenium.TakeScreenshot();

            string screenshotName = browser.Selenium.Url.Replace("\\", "-").Replace("https:", "").Replace("http:", "").Replace("/", "-").RemovePrefix("--") + DateTime.Now.ToIsoString().Replace(":","-") + " Fail.jpg";

            string path = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots", screenshotName);

            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Screenshots"));

            screenShot.SaveAsFile(path);

            Console.WriteLine("Exception: " + e.Message);
            Console.WriteLine("Screenshot under: " + path);

            throw;
        }
        finally
        {
            selenium.Close();
        }
    }

    public ProjectEntity CreateProject(string name, bool autoDelete = true)
    {
        var project = new ProjectEntity()
        {
            Name = name,
        }
        .SetMixin((DomainTaskMixin m) => m.Prefix, "TP")
        .Execute(ProjectOperation.Save);

        CacheLogic.ServerBroadcast!.Send("DomainChanged", project.ToLite().Key());

        return project;
    }

    public void DeleteProject(ProjectEntity project)
    {
        project.Members().UnsafeDelete();

        var boards = project.Boards().ToList();
        foreach (var board in boards)
        {
            board.Columns.Clear();
            using (OperationLogic.AllowSave<BoardEntity>())
                board.Save();
            board.Execute(BoardOperation.Discard);
            board.Delete();
        }
        project.Delete();
    }

    public TaskEntity CreateTask(string name, bool autoDelete = true)
    {
        var board = Database.Query<BoardEntity>().Where(bd => bd.Name == "Maverick Backlog").SingleEx();

        var backlog = board.Columns.SingleEx(t => t.ToString() == "Backlog").ToLite();
        var task = backlog.ConstructFromLite(TaskOperation.CreateTaskFromColumn, name);
        task.Order = (Database.Query<TaskEntity>().Where(t => t.Column.Is(task.Column)).Select(t => t.Order).Max() ?? -1 ) + 1;
        task.Execute(TaskOperation.Save);

        return task;
    }

}

public class RRSPBrowser : BrowserProxy
{
    public override string Url(string url)
    {
        return RRSPTestClass.BaseUrl + url;
    }

    public RRSPBrowser(WebDriver driver)
        : base(driver)
    {
    }

    public override void Login(string username, string password)
    {
        base.Login(username, password);

        string culture = Selenium.FindElement(By.ClassName("sf-culture-dropdown")).GetDomAttribute("data-culture")!;

        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
    }

    public BoardPageProxy Board(BoardEntity board)
    {
        var url = Url($"board/" + board.Id + "/" + Regex.Replace(board.ToString(), @"[^a-zA-Z0-9-_]", ""));

        Selenium.Url = url;

        return new BoardPageProxy(this.Selenium);
    }

    public BoardPageProxy ViewBoard(BoardEntity board)
    {
        var url = Url($"view/board/" + board.Id);

        Selenium.Url = url;

        return new BoardPageProxy(this.Selenium);
    }


    internal CalendarPageProxy Calendar()
    {
        Selenium.Url = Url("calendar");

        return new CalendarPageProxy(this.Selenium);
    }

    public TaskPageProxy Task(TaskEntity task)
    {
        var url = Url($"view/task/" + task.Id);

        Selenium.Url = url;

        return new TaskPageProxy(this.Selenium);
    }
}
