namespace Data.Dtos;

public record AvailableSlotDto(Guid SlotId, DateTime StartTime, DateTime EndTime);

public class SlotDto
{
    public Guid Id { get; set; }
    public Guid Doctor_Id { get; set; }
    public DateTime Start_Time { get; set; }
    public DateTime End_Time { get; set; }
    public string Status { get; set; } = null!;
    public DateTimeOffset Created_At { get; set; }
}

public class CreateSlotRequestDto
{
    public required Guid Doctor_Id { get; set; }
    public required DateTime Start_Time { get; set; }
    public required DateTime End_Time { get; set; }
}