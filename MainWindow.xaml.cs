using System.IO.Ports;
using System.Windows;
using Microsoft.Win32;
using ElrsTtlBatchFlasher.Models;
using ElrsTtlBatchFlasher.Services;
using System.Text.Json;
using ElrsTtlBatchFlasher.Models;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ElrsTtlBatchFlasher;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        RefreshPorts();
        SetStatus("Idle");
    }

    // ===============================
    // UI Helpers
    // ===============================

    private void Log(string text)
    {
        Dispatcher.Invoke(() =>
        {
            LogBox.AppendText(text + Environment.NewLine);
            LogBox.ScrollToEnd();
        });
    }

    private void SetStatus(string text, bool isError = false, bool isSuccess = false)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = text;

            if (isError)
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(239, 68, 68));
            else if (isSuccess)
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(34, 197, 94));
            else
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(100, 116, 139));
        });
    }

    private void SetProgress(double value)
    {
        Dispatcher.Invoke(() =>
        {
            Progress.Value = Math.Max(0, Math.Min(100, value));
        });
    }

    private void SetBusy(bool busy)
    {
        Dispatcher.Invoke(() =>
        {
            StartBtn.IsEnabled = !busy;
            StopBtn.IsEnabled = busy;
        });
    }

    // ===============================
    // COM Ports
    // ===============================

    private void RefreshPorts()
    {
        PortCombo.ItemsSource = SerialPort.GetPortNames().OrderBy(x => x).ToList();
        if (PortCombo.Items.Count > 0 && PortCombo.SelectedIndex < 0)
            PortCombo.SelectedIndex = 0;
    }

    private void RefreshCom_Click(object sender, RoutedEventArgs e)
    {
        RefreshPorts();
        Log("COM list refreshed.");
    }

    // ===============================
    // File Pickers
    // ===============================

    private void PickInto(System.Windows.Controls.TextBox box)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "BIN files (*.bin)|*.bin|All files (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
            box.Text = dlg.FileName;
    }

    private void PickApp0_Click(object sender, RoutedEventArgs e) => PickInto(App0Box);
    private void PickNvs_Click(object sender, RoutedEventArgs e) => PickInto(NvsBox);
    private void PickOta_Click(object sender, RoutedEventArgs e) => PickInto(OtaBox);
    private void PickSpiffs_Click(object sender, RoutedEventArgs e) => PickInto(SpiffsBox);

    // ===============================
    // Build Config
    // ===============================

    private FlashConfig BuildConfig()
    {
        if (PortCombo.SelectedItem is not string port)
            throw new InvalidOperationException("Select COM port.");

        if (!int.TryParse(BaudBox.Text.Trim(), out var baud))
            baud = 921600;

        return new FlashConfig
        {
            EsptoolPath = EsptoolPathBox.Text.Trim(),
            Port = port,
            Baud = baud,
            App0Path = App0Box.Text.Trim(),
            NvsPath = NvsBox.Text.Trim(),
            OtaDataPath = OtaBox.Text.Trim(),
            SpiffsPath = SpiffsBox.Text.Trim()
        };
    }

    // ===============================
    // Start / Stop
    // ===============================

    private async void StartBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_cts != null)
            return;

        FlashConfig config;

        try
        {
            config = BuildConfig();
        }
        catch (Exception ex)
        {
            Log("ERROR: " + ex.Message);
            return;
        }

        _cts = new CancellationTokenSource();
        SetBusy(true);
        SetProgress(5);
        SetStatus("Waiting for bootloader...");
        Log("=== START ===");

        try
        {
            var runner = new BatchRunner(
                config,
                log: Log,
                setOk: n => Dispatcher.Invoke(() => OkCountText.Text = n.ToString())
            );

            await runner.RunAsync(_cts.Token);

            SetStatus("Finished", isSuccess: true);
            SetProgress(100);
        }
        catch (OperationCanceledException)
        {
            SetStatus("Stopped");
            Log("Canceled.");
        }
        catch (Exception ex)
        {
            SetStatus("Error", isError: true);
            SetProgress(0);
            Log("ERROR: " + ex.Message);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            SetBusy(false);
            Log("=== STOP ===");
        }
    }
    private async void ReadCloneBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var config = BuildConfig();
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            SetStatus("Reading clone...");
            SetProgress(10);

            var service = new EsptoolService(config);

            var result = await service.ReadCloneAsync(folderDialog.SelectedPath, CancellationToken.None);

            SetProgress(100);
            SetStatus("Clone saved", isSuccess: true);
            Log(result);
        }
        catch (Exception ex)
        {
            SetStatus("Read error", isError: true);
            Log("ERROR: " + ex.Message);
        }
    }

    private void StopBtn_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
    }
}