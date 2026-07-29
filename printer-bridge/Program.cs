using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Linq;

namespace PrinterRelay
{
    class Program
    {
        private static readonly string Prefix = "http://localhost:5000/";

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            RegisterForStartup();

            using var listener = new HttpListener();
            listener.Prefixes.Add(Prefix);
            
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Failed to start listener: {ex.Message}\nNote: You may need to run this tool as Administrator or allow the port in URLACL.", 
                    "Thermal Printer Bridge Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new PrinterBridgeContext(listener));
        }

        internal static async Task HandleRequestAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // Handle CORS Preflight
            if (request.HttpMethod == "OPTIONS")
            {
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                
                string requestedHeaders = request.Headers["Access-Control-Request-Headers"];
                if (!string.IsNullOrEmpty(requestedHeaders))
                {
                    response.AddHeader("Access-Control-Allow-Headers", requestedHeaders);
                }
                else
                {
                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, Authorization");
                }
                
                response.StatusCode = (int)HttpStatusCode.OK;
                response.Close();
                return;
            }

            // Always add CORS header for actual requests
            response.AddHeader("Access-Control-Allow-Origin", "*");

            if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/printers")
            {
                try
                {
                    var printers = RawPrinterHelper.GetInstalledPrinters();
                    string json = JsonSerializer.Serialize(printers, SourceGenerationContext.Default.ListString);
                    SendResponse(response, HttpStatusCode.OK, json);
                }
                catch (Exception ex)
                {
                    SendResponse(response, HttpStatusCode.InternalServerError, $"{{\"success\":false,\"message\":\"{ex.Message}\"}}");
                }
                return;
            }

            if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/print")
            {
                try
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    string body = await reader.ReadToEndAsync();

                    var printRequest = JsonSerializer.Deserialize(body, SourceGenerationContext.Default.PrintRequest);

                    if (printRequest == null || printRequest.TextLines == null)
                    {
                        SendResponse(response, HttpStatusCode.BadRequest, "Invalid request. Missing printerName or textLines.");
                        return;
                    }

                    string printerName = printRequest.PrinterName ?? "XP-80";
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🖨️ Received print job for printer: '{printerName}'");

                    // Construct ESC/POS bytes
                    using var ms = new MemoryStream();
                    
                    // ESC @ (Initialize printer)
                    ms.Write(new byte[] { 0x1B, 0x40 }, 0, 2);

                    // Convert text lines to bytes
                    string joinedLines = string.Join("\n", printRequest.TextLines) + "\n";
                    byte[] textBytes = Encoding.UTF8.GetBytes(joinedLines);
                    ms.Write(textBytes, 0, textBytes.Length);

                    // Cash drawer open command: ESC p m t1 t2 -> [0x1B, 0x70, 0x00, 0x19, 0xFA]
                    if (printRequest.OpenDrawer)
                    {
                        ms.Write(new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA }, 0, 5);
                    }

                    // Paper cut command: GS V A n -> [0x1D, 0x56, 0x41, 0x10]
                    if (printRequest.CutPaper)
                    {
                        ms.Write(new byte[] { 0x1D, 0x56, 0x41, 0x10 }, 0, 4);
                    }

                    byte[] rawBytes = ms.ToArray();

                    // Print raw bytes using Win32 Spooler API
                    bool success = RawPrinterHelper.SendBytesToPrinter(printerName, rawBytes);

                    if (success)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ Successfully spooled job to '{printerName}'");
                        Console.ResetColor();
                        SendResponse(response, HttpStatusCode.OK, "{\"success\":true,\"message\":\"Print job spooled successfully.\"}");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"❌ Win32 Spooler failed to write print job to '{printerName}'");
                        Console.ResetColor();
                        SendResponse(response, HttpStatusCode.InternalServerError, "{\"success\":false,\"message\":\"Spooler failed to write raw data to specified printer.\"}");
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ Error processing request: {ex.Message}");
                    Console.ResetColor();
                    SendResponse(response, HttpStatusCode.InternalServerError, $"{{\"success\":false,\"message\":\"Internal error: {ex.Message}\"}}");
                }
            }
            else
            {
                SendResponse(response, HttpStatusCode.NotFound, "Not Found. Use POST to /print.");
            }
        }

        private static void SendResponse(HttpListenerResponse response, HttpStatusCode statusCode, string content)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(content);
                response.StatusCode = (int)statusCode;
                response.ContentType = content.StartsWith("{") ? "application/json" : "text/plain";
                response.ContentLength64 = buffer.Length;
                using Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending response: {ex.Message}");
            }
            finally
            {
                response.Close();
            }
        }

        private static void RegisterForStartup()
        {
            string? exePath = Environment.ProcessPath;
            string? logPath = null;
            if (!string.IsNullOrEmpty(exePath))
            {
                string? dir = Path.GetDirectoryName(exePath);
                if (dir != null) logPath = Path.Combine(dir, "startup-log.txt");
            }

            try
            {
                if (logPath != null) File.WriteAllText(logPath, $"Startup registration started. exePath: {exePath}\n");
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
                    string appName = "PrinterBridge";
                    
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(runKey, true);
                        if (key != null)
                        {
                            object? existingValue = key.GetValue(appName);
                            string expectedValue = $"\"{exePath}\"";

                            if (logPath != null) File.AppendAllText(logPath, $"Existing value: {existingValue}, Expected value: {expectedValue}\n");

                            if (existingValue == null || existingValue.ToString() != expectedValue)
                            {
                                key.SetValue(appName, expectedValue);
                                if (logPath != null) File.AppendAllText(logPath, "Registry key updated successfully!\n");
                            }
                            else
                            {
                                if (logPath != null) File.AppendAllText(logPath, "Registry key already matches.\n");
                            }
                        }
                        else
                        {
                            if (logPath != null) File.AppendAllText(logPath, "Registry key was null!\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (logPath != null) File.AppendAllText(logPath, $"Error: {ex.Message}\n{ex.StackTrace}\n");
            }
        }
    }

    public class PrinterBridgeContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public PrinterBridgeContext(HttpListener listener)
        {
            _listener = listener;

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            
            var titleItem = new ToolStripMenuItem("Thermal Printer Bridge (Running)") { Enabled = false };
            contextMenu.Items.Add(titleItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            var showPrintersItem = new ToolStripMenuItem("List Installed Printers", null, ShowPrinters_Click);
            contextMenu.Items.Add(showPrintersItem);

            var restartItem = new ToolStripMenuItem("Restart Server", null, RestartServer_Click);
            contextMenu.Items.Add(restartItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit", null, Exit_Click);
            contextMenu.Items.Add(exitItem);

            // Initialize Tray Icon
            _trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = contextMenu,
                Text = "Thermal Printer Bridge",
                Visible = true
            };

            _trayIcon.DoubleClick += ShowPrinters_Click;

            // Start background listening task
            _ = Task.Run(() => RunListenerAsync(_cts.Token));
        }

        private async Task RunListenerAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    _ = Task.Run(() => Program.HandleRequestAsync(context), cancellationToken);
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Log silently in background thread
                    System.Diagnostics.Debug.WriteLine($"Error retrieving context: {ex.Message}");
                }
            }
        }

        private void ShowPrinters_Click(object? sender, EventArgs e)
        {
            try
            {
                var printers = RawPrinterHelper.GetInstalledPrinters();
                string printerList = printers.Count > 0 
                    ? string.Join(Environment.NewLine, printers.Select(p => $"• {p}"))
                    : "No printers found.";
                
                MessageBox.Show($"Installed Printers:{Environment.NewLine}{Environment.NewLine}{printerList}", 
                    "Thermal Printer Bridge - Installed Printers", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving printers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestartServer_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }

                _listener.Start();
                
                _trayIcon.ShowBalloonTip(3000, "Printer Bridge", "HTTP Listener restarted successfully.", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restart server: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            _cts.Cancel();
            try
            {
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }
            }
            catch { }

            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            
            Application.Exit();
        }
    }

    public class PrintRequest
    {
        public string? PrinterName { get; set; }
        public System.Collections.Generic.List<string>? TextLines { get; set; }
        public bool OpenDrawer { get; set; }
        public bool CutPaper { get; set; }
    }

    public static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class DOCINFOW
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? pDocName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? pDataType;
        }

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, EntryPoint = "EnumPrintersW", SetLastError = true)]
        private static extern bool EnumPrinters(int flags, string? name, int level, IntPtr pPrinterEnum, int cbBuf, out int pcbNeeded, out int pcReturned);

        private const int PRINTER_ENUM_LOCAL = 0x00000002;
        private const int PRINTER_ENUM_CONNECTIONS = 0x00000004;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PRINTER_INFO_4
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pPrinterName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pServerName;
            public int Attributes;
        }

        public static System.Collections.Generic.List<string> GetInstalledPrinters()
        {
            var printers = new System.Collections.Generic.List<string>();
            int flags = PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS;
            int level = 4;
            int pcbNeeded = 0;
            int pcReturned = 0;

            EnumPrinters(flags, null, level, IntPtr.Zero, 0, out pcbNeeded, out pcReturned);
            if (pcbNeeded <= 0) return printers;

            IntPtr pAddr = Marshal.AllocHGlobal(pcbNeeded);
            try
            {
                if (EnumPrinters(flags, null, level, pAddr, pcbNeeded, out pcbNeeded, out pcReturned))
                {
                    int structSize = Marshal.SizeOf(typeof(PRINTER_INFO_4));
                    IntPtr currentAddr = pAddr;
                    for (int i = 0; i < pcReturned; i++)
                    {
                        var info = Marshal.PtrToStructure<PRINTER_INFO_4>(currentAddr);
                        if (!string.IsNullOrEmpty(info.pPrinterName))
                        {
                            printers.Add(info.pPrinterName);
                        }
                        currentAddr = IntPtr.Add(currentAddr, structSize);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pAddr);
            }
            return printers;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPWStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, Int32 level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOW di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, Int32 dwCount, out Int32 dwWritten);

        public static bool SendBytesToPrinter(string szPrinterName, byte[] bytes)
        {
            IntPtr hPrinter = IntPtr.Zero;
            DOCINFOW di = new DOCINFOW();
            bool bSuccess = false;

            di.pDocName = "Thermal Receipt POS Print";
            di.pDataType = "RAW";

            int errorCode = 0;

            if (OpenPrinter(szPrinterName, out hPrinter, IntPtr.Zero))
            {
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                        Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);
                        
                        Int32 dwWritten = 0;
                        bSuccess = WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out dwWritten);
                        if (!bSuccess)
                        {
                            errorCode = Marshal.GetLastWin32Error();
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[Spooler Error] WritePrinter failed. Error code: {errorCode}");
                            Console.ResetColor();
                        }
                        
                        Marshal.FreeCoTaskMem(pUnmanagedBytes);
                        EndPagePrinter(hPrinter);
                    }
                    else
                    {
                        errorCode = Marshal.GetLastWin32Error();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[Spooler Error] StartPagePrinter failed. Error code: {errorCode}");
                        Console.ResetColor();
                    }
                    EndDocPrinter(hPrinter);
                }
                else
                {
                    errorCode = Marshal.GetLastWin32Error();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Spooler Error] StartDocPrinter failed. Error code: {errorCode}");
                    Console.ResetColor();
                }
                ClosePrinter(hPrinter);
            }
            else
            {
                errorCode = Marshal.GetLastWin32Error();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Spooler Error] OpenPrinter failed for printer name '{szPrinterName}'. Error code: {errorCode}");
                Console.ResetColor();
                
                if (errorCode == 1801) // ERROR_INVALID_PRINTER_NAME
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("💡 Tip: The printer name is invalid. Please make sure the printer is installed in Windows and the name matches exactly.");
                    Console.ResetColor();
                }
            }
            return bSuccess;
        }
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(PrintRequest))]
    [JsonSerializable(typeof(System.Collections.Generic.List<string>))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
