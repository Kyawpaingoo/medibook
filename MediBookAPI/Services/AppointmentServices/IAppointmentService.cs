using Data.Dtos;

namespace MediBookAPI.Services.AppointmentServices;

public interface IAppointmentService
{
    Task<(string Status, Guid? AppointmentId)> BookAppointmentAsync(BookAppointmentRequestDto dto);
    Task<AppointmentDtos?> GetAppointmentByIdAsync(Guid id);
    Task<string> ConfirmAppointmentAsync(Guid id, Guid? changedByUserId);
    Task<string> CancelAppointmentAsync(Guid id, Guid? changedByUserId);
}