using System.ComponentModel;
using System.Diagnostics;

namespace RetailSuite.Tray;

/// <summary>First-run "Store License &amp; Activation" window. Verifies the license key against the cloud and provisions the store.</summary>
internal sealed class ActivationForm : Form
{
    private const string DefaultCloudUrl = "https://localhost:5001";
    private readonly TextBox _licenseKey = new();
    private readonly Button _activate = new() { Text = "Activate && Install", Width = 150, Height = 32 };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.DimGray };

    internal ActivationForm()
    {
        Text = "RetailSuite Local Server - Store Activation";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(460, 200);
        Font = new Font("Segoe UI", 9.5f);

        var title = new Label
        {
            Text = "Store License & Activation",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 15),
        };
        var info = new Label
        {
            Text = "Enter your Store License Key to verify your store and configure this On-Premises server.",
            AutoSize = false,
            Size = new Size(420, 36),
            Location = new Point(20, 50),
        };
        var label = new Label { Text = "Store License Key:", AutoSize = true, Location = new Point(20, 100) };
        _licenseKey.Location = new Point(160, 96);
        _licenseKey.Width = 275;

        _activate.Location = new Point(285, 145);
        _activate.Click += async (_, _) => await ActivateAsync();
        _status.Location = new Point(20, 152);
        Controls.AddRange(new Control[] { title, info, label, _licenseKey, _activate, _status });
        AcceptButton = _activate;
    }

    private async Task ActivateAsync()
    {
        if (string.IsNullOrWhiteSpace(_licenseKey.Text))
        {
            MessageBox.Show(this, "Please enter the Store License Key.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _activate.Enabled = false;
        _status.Text = "Verifying license & installing...";
        var passwordFile = Path.Combine(Path.GetTempPath(), $"retailsuite_{Guid.NewGuid():N}.pw");

        try
        {
            var args = string.Join(
                ' ',
                "--onprem-install",
                $"--activation-key=\"{_licenseKey.Text.Trim()}\"",
                $"--cloud-relay-url=\"{DefaultCloudUrl}\"",
                $"--admin-password-file=\"{passwordFile}\"");

            var exitCode = await Task.Run(() =>
            {
                using var proc = Process.Start(new ProcessStartInfo(ServerController.HostExePath, args)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden,
                });
                proc!.WaitForExit();
                return proc.ExitCode;
            });

            if (exitCode != 0 || !ServerController.IsProvisioned)
            {
                throw new InvalidOperationException($"License verification or installation failed (exit code {exitCode}).");
            }

            var password = File.Exists(passwordFile) ? File.ReadAllText(passwordFile).Trim() : null;
            var message = "Store verified and installed. RetailSuite Local Server is starting on http://localhost:5050." +
                (password is null
                    ? string.Empty
                    : $"{Environment.NewLine}{Environment.NewLine}Initial admin password: {password}{Environment.NewLine}Please save this password now.");
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Win32Exception)
        {
            _status.Text = "Administrator permission was declined.";
            _activate.Enabled = true;
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            _activate.Enabled = true;
        }
        finally
        {
            if (File.Exists(passwordFile))
            {
                File.Delete(passwordFile);
            }
        }
    }
}
