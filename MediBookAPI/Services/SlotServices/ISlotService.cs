using Data.Dtos;

namespace MediBookAPI.Services.SlotServices;

public interface ISlotService
{
    Task<(string Status, Guid? SlotId)> CreateSlotAsync(CreateSlotRequestDto dto);
    Task<SlotDto?> GetSlotAsync(Guid id);
    Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(Guid doctorId);
    Task<string> CancelSlotAsync(Guid id);
}