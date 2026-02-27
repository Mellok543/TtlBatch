namespace ElrsTtlBatchFlasher.Models;

public sealed class ReceiverProfile
{
    public string Name { get; set; } = "";
    public string Chip { get; set; } = "";

    public string AppOffset { get; set; } = "";
    public string AppSize { get; set; } = "";

    public string NvsOffset { get; set; } = "";
    public string NvsSize { get; set; } = "";

    public string OtadataOffset { get; set; } = "";
    public string OtadataSize { get; set; } = "";

    public string SpiffsOffset { get; set; } = "";
    public string SpiffsSize { get; set; } = "";

    public override string ToString() => Name;
}