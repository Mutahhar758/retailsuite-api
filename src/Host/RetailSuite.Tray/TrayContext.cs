using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace RetailSuite.Tray;

/// <summary>System-tray manager: shows server status and offers Start / Stop / Restart / Open dashboard.</summary>
internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly ToolStripMenuItem _statusItem = new("Checking...") { Enabled = false };
    private readonly ToolStripMenuItem _start = new("Start Server");
    private readonly ToolStripMenuItem _stop = new("Stop Server");
    private readonly ToolStripMenuItem _restart = new("Restart Server");
    private readonly ToolStripMenuItem _activate = new("Activate Store...");
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 3000 };
    private ServerState? _last;

    internal TrayContext()
    {
        var menu = new ContextMenuStrip();
        _start.Click += (_, _) => RunAction(ServerController.Start);
        _stop.Click += (_, _) => RunAction(ServerController.Stop);
        _restart.Click += async (_, _) => await ServerController.RestartAsync();
        _activate.Click += (_, _) => ShowActivation();

        var dashboard = new ToolStripMenuItem("Open API / Swagger");
        dashboard.Click += (_, _) => Process.Start(new ProcessStartInfo(ServerController.DashboardUrl) { UseShellExecute = true });
        var dataFolder = new ToolStripMenuItem("Open Data Folder");
        dataFolder.Click += (_, _) =>
        {
            Directory.CreateDirectory(ServerController.DataDirectory);
            Process.Start(new ProcessStartInfo(ServerController.DataDirectory) { UseShellExecute = true });
        };
        var exit = new ToolStripMenuItem("Exit Tray (server keeps running)");
        exit.Click += (_, _) => ExitThread();

        menu.Items.AddRange(new ToolStripItem[]
        {
            _statusItem, new ToolStripSeparator(), _start, _stop, _restart, _activate,
            new ToolStripSeparator(), dashboard, dataFolder, new ToolStripSeparator(), exit,
        });

        _tray = new NotifyIcon { ContextMenuStrip = menu, Visible = true, Icon = BuildIcon(Color.Gray), Text = "RetailSuite Local Server" };
        _tray.DoubleClick += (_, _) => Process.Start(new ProcessStartInfo(ServerController.DashboardUrl) { UseShellExecute = true });

        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
        Refresh();

        // First run: no store configured yet -> show the license / activation screen.
        if (!ServerController.IsProvisioned)
        {
            ShowActivation();
        }
    }

    private void ShowActivation()
    {
        using var form = new ActivationForm();
        form.ShowDialog();
        Refresh();
    }

    private static void RunAction(Action action) => Task.Run(action);

    private void Refresh()
    {
        var state = ServerController.IsProvisioned ? ServerController.GetState() : ServerState.NotInstalled;
        _activate.Visible = !ServerController.IsProvisioned;
        _start.Enabled = state == ServerState.Stopped;
        _stop.Enabled = state is ServerState.Running or ServerState.Starting;
        _restart.Enabled = state == ServerState.Running;

        var (text, color) = state switch
        {
            ServerState.Running => ("Server is running (port 5050)", Color.LimeGreen),
            ServerState.Starting => ("Server is starting...", Color.Orange),
            ServerState.Stopping => ("Server is stopping...", Color.Orange),
            ServerState.Stopped => ("Server is stopped", Color.IndianRed),
            _ => ("Not activated - open Activate Store", Color.Gray),
        };

        _statusItem.Text = text;
        _tray.Text = $"RetailSuite: {text}"[..Math.Min(63, $"RetailSuite: {text}".Length)];

        if (_last != state)
        {
            _last = state;
            var old = _tray.Icon;
            _tray.Icon = BuildIcon(color);
            old?.Dispose();
        }
    }

    private static Icon BuildIcon(Color color)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, 2, 2, 28, 28);
            using var pen = new Pen(Color.White, 2);
            g.DrawEllipse(pen, 2, 2, 28, 28);
            using var font = new Font("Segoe UI", 13f, FontStyle.Bold, GraphicsUnit.Pixel);
            g.DrawString("R", font, Brushes.White, 8, 6);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _tray.Visible = false;
        _tray.Dispose();
        base.ExitThreadCore();
    }
}
