namespace TinMI.Models;

public class BookingSlotViewModel
{
    public string Time { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Session { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}
