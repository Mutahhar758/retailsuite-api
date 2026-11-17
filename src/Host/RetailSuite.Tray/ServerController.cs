using System.Diagnostics;

namespace RetailSuite.Tray;

internal enum ServerState
{
    NotInstalled,
    Stopped,
    Starting,
    Running,
    Stopping,
}

/// <summary>Thin wrapper over <c>sc.exe</c> for the RetailSuite Windows services (no extra packages needed).</summary>
internal static class ServerController
{
    internal const string ApiService = "RetailSuiteLocalServer";
    internal const string PgService = "RetailSuitePostgres";
    internal const string DashboardUrl = "http://localhost:5050/swagger";

    internal static string DataDirectory
    {
        get
        {
            var dir = Environment.GetEnvironmentVariable("RETAILSUITE_DATA_DIR");
            return string.IsNullOrWhiteSpace(dir)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RetailSuite")
                : dir;
        }
    }

    internal static bool IsProvisioned => File.Exists(Path.Combine(DataDirectory, "store.dat"));

    internal static string HostExePath => Path.Combine(AppContext.BaseDirectory, "Retailer.Host.exe");

    internal static ServerState GetState()
    {
        var output = RunSc($"query {ApiService}", out var exit);
        if (exit == 1060 || output.Contains("1060"))
        {
            return ServerState.NotInstalled;
        }

        if (output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
        {
            return ServerState.Running;
        }

        if (output.Contains("START_PENDING", StringComparison.OrdinalIgnoreCase))
        {
            return ServerState.Starting;
        }

        if (output.Contains("STOP_PENDING", StringComparison.OrdinalIgnoreCase))
        {
            return ServerState.Stopping;
        }

        return output.Contains("STOPPED", StringComparison.OrdinalIgnoreCase) ? ServerState.Stopped : ServerState.NotInstalled;
    }

    internal static void Start()
    {
        RunSc($"start {PgService}", out _);
        RunSc($"start {ApiService}", out _);
    }

    internal static void Stop() => RunSc($"stop {ApiService}", out _);

    internal static async Task RestartAsync()
    {
        Stop();
        for (var i = 0; i < 30 && GetState() != ServerState.Stopped; i++)
        {
            await Task.Delay(500);
        }

        Start();
    }

    private static string RunSc(string args, out int exitCode)
    {
        using var proc = Process.Start(new ProcessStartInfo("sc.exe", args)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        });
        var text = proc!.StandardOutput.ReadToEnd();
        proc.WaitForExit();
        exitCode = proc.ExitCode;
        return text;
    }
}
