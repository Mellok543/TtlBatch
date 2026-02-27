namespace ElrsTtlBatchFlasher.Models;

public sealed class FlashConfig
{
    public string EsptoolPath { get; set; } = "esptool"; // или полный путь до esptool.exe/py
    public string Port { get; set; } = "COM6";
    public int Baud { get; set; } = 921600;

    public string App0Path { get; set; } = "";
    public string NvsPath { get; set; } = "";
    public string OtaDataPath { get; set; } = "";
    public string SpiffsPath { get; set; } = "";

    public int WaitBootTimeoutMs { get; set; } = 30000;
    public int BetweenDevicesDelayMs { get; set; } = 1500;
}