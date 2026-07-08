using System.Linq.Expressions;
using Data.Dtos;
using Data.Enums;
using Data.Models;
using Infra.Services;
using Infra.UnitOfWork;
using Infra.Utility;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MediBookAPI.Services.SlotServices;

public class SlotService: ISlotService
{
    private readonly IBookingUnitOfWork _uow;
    private readonly SlotAvailabilityHelper _slotAvailabilityHelper;
    private readonly ILogger<SlotService> _logger;

    public SlotService(IBookingUnitOfWork uow, SlotAvailabilityHelper slotAvailabilityHelper,
        ILogger<SlotService> logger)
    {
        _uow = uow;
        _slotAvailabilityHelper = slotAvailabilityHelper;
        _logger = logger;
    }
    
    public async Task<(string Status, Guid? SlotId)> CreateSlotAsync(CreateSlotRequestDto dto)
    {
        if (dto.End_Time <= dto.Start_Time)
            return (ResponseStatus.Fail, null);

        var doctor = await _uow.Doctors.GetByIdAsync(dto.Doctor_Id);
        if(doctor is null || !doctor.Is_Active)
            return (ResponseStatus.DoestNotExist, null);

        var newSlot = new tbSlots
        {
            Id = Guid.NewGuid(),
            Doctor_Id = dto.Doctor_Id,
            Start_Time = dto.Start_Time,
            End_Time = dto.End_Time,
            Status = SlotStatus.Available,
            Created_At = DateTimeOffset.UtcNow,
            Updated_At = DateTimeOffset.UtcNow
        };

        try
        {
            await _uow.Slots.AddAsync(newSlot);
            await _uow.SaveChangeAsync();

            await _slotAvailabilityHelper.InvalidateAsync(dto.Doctor_Id);

            return (ResponseStatus.Success, newSlot.Id);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.UniqueViolation
                                           })
        {
            _logger.LogWarning(ex, "Doctor {DoctorId} already has a slot starting at {StartTime}", dto.Doctor_Id, dto.Start_Time);
            return (ResponseStatus.Conflict, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create slot for doctor {DoctorId}", dto.Doctor_Id);
            return (ResponseStatus.Fail, null);
        }
    }

    public async Task<SlotDto?> GetSlotAsync(Guid id)
    {
        return await _uow.Slots.GetWithoutTracking().Where(s => s.Id == id)
                            .Select(ConvertToSlotDto).FirstOrDefaultAsync();
    }

    public Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(Guid doctorId)
    {
        return _slotAvailabilityHelper.GetAvailableSlotAsync(doctorId); 
    }

    public async Task<string> CancelSlotAsync(Guid id)
    {
        var slot = await _uow.Slots.GetByIdAsync(id);
        
        if(slot is null) return ResponseStatus.DoestNotExist;
        
        if (slot.Status != SlotStatus.Available)
            return ResponseStatus.Conflict;

        slot.Status = SlotStatus.Cancelled;
        slot.Updated_At = DateTimeOffset.UtcNow;

        try
        {
            await _uow.Slots.UpdateAsync(slot);
            await _uow.SaveChangeAsync();

            await _slotAvailabilityHelper.InvalidateAsync(slot.Doctor_Id);

            return ResponseStatus.Success;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Slot {SlotId} was modified concurrently while cancelling", id);
            return ResponseStatus.Conflict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel slot {SlotId}", id);
            return ResponseStatus.Fail;
        }
    }
    
    private static readonly Expression<Func<tbSlots, SlotDto>> ConvertToSlotDto =
        s => new SlotDto
        {
            Id = s.Id,
            Doctor_Id = s.Doctor_Id,
            Start_Time = s.Start_Time,
            End_Time = s.End_Time,
            Status = s.Status.ToString(),
            Created_At = s.Created_At
        };
}