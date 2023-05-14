using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Reflection;

namespace GithubReleaseUpgrader.Sample
{
    internal static class Program
    {
        public class UpgradeProgressImpl : UpgradeProgress
        {
            public override string UpgradeTempFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GithubReleaseUpgrader");

            public override string GithubUrl { get; } = "https://github.com/jim-jiang-github/GithubReleaseUpgrader";

            public override string UpgradeResourceName { get; } = "windows-x64.zip";

            public override string UpgradeInfoName { get; } = "upgradeInfo.json";

            public override string ExecutableName { get; } = "GithubReleaseUpgrader.Sample.exe";

            public override void Force(Version currentVersion, Version newtVersion, string? releaseLogMarkDown)
            {
            }

            public override UpgradeOption Notify(Version currentVersion, Version newtVersion, string? releaseLogMarkDown)
            {
                Log.Information("Notify old version is:{currentVersion}, new version is:{newtVersion}", currentVersion, newtVersion);
                Log.Information("Enter upgrade mode: c:Cancel/r:RemindAfter30Minutes/i:IgnoreCurrentVersion/d:ConfirmDownload");
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.C:
                    default:
                        return UpgradeOption.Cancel;
                    case ConsoleKey.I:
                        return UpgradeOption.IgnoreCurrentVersion;
                    case ConsoleKey.R:
                        return UpgradeOption.RemindAfter30Minutes;
                    case ConsoleKey.D:
                        return UpgradeOption.ConfirmDownload;
                }
            }

            public override void Tip(Version currentVersion, Version newtVersion, string? releaseLogMarkDown)
            {
            }
        }
        public class LogEventSink : ILogEventSink
        {
            private readonly Form1 _form;
            public LogEventSink(Form1 form)
            {
                _form = form;
            }
            public void Emit(LogEvent logEvent)
            {
                _form.SetLog(logEvent.MessageTemplate.ToString());
            }
        }
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Form1 form1 = new Form1();
            LogEventSink logEventSink = new LogEventSink(form1);
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
                .WriteTo.Console()
#else
                .MinimumLevel.Information()
#endif
                .WriteTo.Sink(logEventSink)
                .CreateLogger();

            Assembly assembly = Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;
            Log.Debug("Current version is: " + version);

            var needUpgrade = Upgrader.CheckForUpgrade(new UpgradeProgressImpl());
            if (!needUpgrade)
            {
                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                Application.Run(form1);
            }
            Upgrader.PerformUpgradeIfNeeded();
        }
    }
}