using System.IO;
using ElrsTtlBatchFlasher.Models;

namespace ElrsTtlBatchFlasher.Services;

public sealed class EsptoolService
{
    private readonly FlashConfig _cfg;

    public EsptoolService(FlashConfig cfg) => _cfg = cfg;

    private string BaseArgs() =>
        $"--chip esp32c3 --port {_cfg.Port} --baud {_cfg.Baud}";

    public async Task<(bool ok, string output)> TryChipIdVerboseAsync(CancellationToken ct)
    {
        var args = $"--chip esp32c3 --port {_cfg.Port} --baud 115200 chip_id";

        var (code, outp, err) = await ProcessRunner.RunAsync(_cfg.EsptoolPath, args, ct);
        var text = (outp + "\n" + err).Trim();

        var ok = code == 0 && text.Contains("Chip is ESP32-C3");
        return (ok, text);
    }
    
    public async Task<string> ReadCloneAsync(string outputDir, CancellationToken ct)
    {
        Directory.CreateDirectory(outputDir);

        var args =
            $"--chip esp32c3 --port {_cfg.Port} --baud {_cfg.Baud} " +
            $"--before no_reset --after no_reset read_flash " +
            $"0x010000 0x1E0000 \"{Path.Combine(outputDir, "app0.bin")}\"";

        await ProcessRunner.RunAsync(_cfg.EsptoolPath, args, ct);

        args =
            $"--chip esp32c3 --port {_cfg.Port} --baud {_cfg.Baud} " +
            $"--before no_reset --after no_reset read_flash " +
            $"0x009000 0x005000 \"{Path.Combine(outputDir, "nvs.bin")}\"";

        await ProcessRunner.RunAsync(_cfg.EsptoolPath, args, ct);

        args =
            $"--chip esp32c3 --port {_cfg.Port} --baud {_cfg.Baud} " +
            $"--before no_reset --after no_reset read_flash " +
            $"0x00E000 0x002000 \"{Path.Combine(outputDir, "otadata.bin")}\"";

        await ProcessRunner.RunAsync(_cfg.EsptoolPath, args, ct);

        args =
            $"--chip esp32c3 --port {_cfg.Port} --baud {_cfg.Baud} " +
            $"--before no_reset --after no_reset read_flash " +
            $"0x3D0000 0x020000 \"{Path.Combine(outputDir, "spiffs.bin")}\"";

        await ProcessRunner.RunAsync(_cfg.EsptoolPath, args, ct);

        return "Clone read successfully.";
    }

    public async Task<string> WriteCloneAsync(CancellationToken ct)
    {
        var args =
            $"--chip esp32c3 --port {_cfg.Port} --baud {_cfg.Baud} " +
            $"--before no_reset --after no_reset write_flash " +
            $"0x010000 \"{_cfg.App0Path}\" " +
            $"0x009000 \"{_cfg.NvsPath}\" " +
            $"0x00E000 \"{_cfg.OtaDataPath}\" " +
            $"0x3D0000 \"{_cfg.SpiffsPath}\"";

        var (code, outp, err) = await ProcessRunner.RunAsync(_cfg.EsptoolPath, args, ct);
        var all = (outp + "\n" + err).Trim();

        if (code != 0)
            throw new InvalidOperationException(all);

        return all;
    }
}