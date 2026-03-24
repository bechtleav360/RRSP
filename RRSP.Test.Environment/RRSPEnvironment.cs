using DocumentFormat.OpenXml.VariantTypes;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Signum.API;
using Signum.Authorization;
using Signum.Engine.Maps;
using Signum.Engine.Sync;
using Signum.Security;
using System.Globalization;
using System.IO;

namespace RRSP.Test.Environment;

public static class RRSPEnvironment
{
    public static string? BroadcastSecretHash = null!;
    static bool started = false;
    public static void Start()
    {
        if (!started)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                .AddUserSecrets(typeof(RRSPEnvironment).Assembly, optional: true)
                .Build();
            var connectionString = config.GetConnectionString("ConnectionString")!;
            var azureConnectionString = config.GetConnectionString("AzureStorageConnectionString");
            var brodcasSecret = config["BroadcastSecret"]!;
            BroadcastSecretHash = brodcasSecret.HasText() ? Convert.ToBase64String(PasswordEncoding.HashPassword("", brodcasSecret)) : null;
            //if (!connectionString.Contains("Test")) //Security mechanism to avoid passing test on production
            //    throw new InvalidOperationException("ConnectionString does not contain the word 'Test'.");

            var isPostgres = config.GetValue<bool>("IsPostgres")!;
            if (isPostgres)
            {
                Administrator.PostgressTools.CreateDatabaseIfNoExists(connectionString);
            }

            Starter.Start(connectionString, isPostgres, azureConnectionString, 
                config.GetValue<string>("BroadcastSecret"), 
                config.GetValue<string>("BroadcastUrls"),
                wsb: null, 
                config.GetValue<string>("FilesRoot"));
            started = true;
        }
    }

    public static void StartAndInitialize()
    {
        Start();
        Schema.Current.Initialize();
        AuthLogic.GloballyEnabled = false;
        UserHolder.Current = new UserWithClaims(AuthLogic.RetrieveUser("System")!);
    }
}
