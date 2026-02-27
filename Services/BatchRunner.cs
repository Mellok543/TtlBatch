using System.IO;
using ElrsTtlBatchFlasher.Models;

namespace ElrsTtlBatchFlasher.Services;

public sealed class BatchRunner
{
    private readonly FlashConfig _cfg;
    private readonly Action<string> _log;
    private readonly Action<int> _setOk;

    public BatchRunner(FlashConfig cfg, Action<string> log, Action<int> setOk)
    {
        _cfg = cfg;
        _log = log;
        _setOk = setOk;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        ValidateFiles();

        var esptool = new EsptoolService(_cfg);
        int ok = 0;

        _log("Batch started.");
        _log("Put RX into BOOT, keep BOOT pressed.");
        _log("--------------------------------------------------");

        while (!ct.IsCancellationRequested)
        {
            _log("Waiting for bootloader...");

            // Ждём bootloader
            while (!ct.IsCancellationRequested)
            {
                var (detected, msg) = await esptool.TryChipIdVerboseAsync(ct);

                if (detected)
                {
                    _log("ESP32-C3 detected.");
                    break;
                }

                await Task.Delay(600, ct);
            }

            if (ct.IsCancellationRequested)
                break;

            _log("Flashing clone...");

            try
            {
                var output = await esptool.WriteCloneAsync(ct);

                ok++;
                _setOk(ok);

                _log($"DONE #{ok}");
                _log("Disconnect RX and connect next.");
                _log("--------------------------------------------------");

                await Task.Delay(_cfg.BetweenDevicesDelayMs, ct);
            }
            catch (Exception ex)
            {
                _log("FLASH ERROR:");
                _log(ex.Message);
                _log("Try again with RX in BOOT.");
                _log("--------------------------------------------------");
            }
        }
    }

    private void ValidateFiles()
    {
        if (!File.Exists(_cfg.App0Path)) throw new FileNotFoundException("app0.bin not found");
        if (!File.Exists(_cfg.NvsPath)) throw new FileNotFoundException("nvs.bin not found");
        if (!File.Exists(_cfg.OtaDataPath)) throw new FileNotFoundException("otadata.bin not found");
        if (!File.Exists(_cfg.SpiffsPath)) throw new FileNotFoundException("spiffs.bin not found");
    }
}