namespace Data.Dtos;

public class AppointmentDtos
{
    public Guid Id { get; set; }
    public Guid SlotId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public string? DoctorName { get; set; }
    public string? PatientName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = null!;
    public DateTimeOffset Created_At { get; set; }
}

public class BookAppointmentRequestDto
{
    public required Guid SlotId { get; set; }
    public required Guid PatientId { get; set; }
}