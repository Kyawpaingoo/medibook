using Data.Dtos;
using Data.Enums;
using Data.Models;
using Infra.UnitOfWork;
using Infra.Utility;
using MediBookAPI.Services.SlotServices;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MediBookAPI.Services.AppointmentServices;

public class AppointmentService : IAppointmentService
{
    private readonly IBookingUnitOfWork _uow;
    private readonly SlotAvailabilityHelper _slotHelper;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(IBookingUnitOfWork uow, SlotAvailabilityHelper slotHelper,
        ILogger<AppointmentService> logger)
    {
        _uow = uow;
        _slotHelper = slotHelper;
        _logger = logger;
    }
    
    public async Task<(string Status, Guid? AppointmentId)> BookAppointmentAsync(BookAppointmentRequestDto dto)
    {
        var slot = await _uow.Slots.GetByIdAsync(dto.SlotId);

        if (slot is null) return (ResponseStatus.DoestNotExist, null);
        
        if (slot.Status != SlotStatus.Available)
            return (ResponseStatus.Conflict, null);
        
        var patient = await _uow.Patients.GetByIdAsync(dto.PatientId);
        
        if (patient is null) return (ResponseStatus.DoestNotExist, null);

        await _uow.BeginTransactionAsync();
        
        var appointment = new tbAppointments
        {
            Id = Guid.NewGuid(),
            Slot_Id = slot.Id,
            Doctor_Id = slot.Doctor_Id,
            Patient_Id = patient.Id,
            Status = AppointmentStatus.Reserved,
            Created_At = DateTimeOffset.UtcNow,
            Updated_At = DateTimeOffset.UtcNow
        };

        try
        {
            slot.Status = SlotStatus.Reserved;
            slot.Updated_At = DateTimeOffset.UtcNow;
            await _uow.Slots.UpdateAsync(slot);
            
            await _uow.Appointments.AddAsync(appointment);

            await _uow.AppointmentStatusHistory.AddAsync(new tbAppointmentStatusHistory
            {
                Appointment_Id = appointment.Id,
                From_Status = null,
                To_Status = AppointmentStatus.Reserved,
                Changed_At = DateTimeOffset.UtcNow,
                Changed_By_User_Id = null
            });

            await _uow.SaveChangeAsync();
            await _uow.CommitTransactionAsync();

            await _slotHelper.InvalidateAsync(slot.Doctor_Id);
            
            return (ResponseStatus.Success, appointment.Id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await _uow.RollbackTransactionAsync();
            _logger.LogWarning(ex, "Slot {SlotId} was booked by a concurrent request (xmin mismatch)", dto.SlotId);
            return (ResponseStatus.Conflict, null);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await _uow.RollbackTransactionAsync();
            _logger.LogWarning(ex, "Slot {SlotId} already has an active appointment", dto.SlotId);
            return (ResponseStatus.Conflict, null);
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to book slot {SlotId}", dto.SlotId);
            return (ResponseStatus.Fail, null);
        }
    }

    public async Task<AppointmentDtos?> GetAppointmentByIdAsync(Guid id)
    {
        return await _uow.Appointments.GetWithoutTracking()
            .Where(a => a.Id == id)
            .Select(a => new AppointmentDtos
            {
                Id = a.Id,
                SlotId = a.Slot_Id,
                DoctorId = a.Doctor_Id,
                PatientId = a.Patient_Id,
                DoctorName = a.Doctor.Full_Name,
                PatientName = a.Patient.Full_Name,
                StartTime = a.Slot.Start_Time,
                EndTime = a.Slot.End_Time,
                Status = a.Status.ToString(),
                Created_At = a.Created_At
            })
            .FirstOrDefaultAsync();
    }

    public async Task<string> ConfirmAppointmentAsync(Guid id, Guid? changedByUserId)
    {
        var appointment = await _uow.Appointments.GetByIdAsync(id);

        if (appointment is null) return ResponseStatus.DoestNotExist;
        
        if(appointment.Status != AppointmentStatus.Reserved)
            return ResponseStatus.Conflict;
        
        var slot = await _uow.Slots.GetByIdAsync(appointment.Slot_Id);
        await _uow.BeginTransactionAsync();
        
        try
        {
            appointment.Status = AppointmentStatus.Confirmed;
            appointment.Updated_At = DateTimeOffset.UtcNow;
            await _uow.Appointments.UpdateAsync(appointment);

            if (slot is not null)
            {
                slot.Status = SlotStatus.Confirmed;
                slot.Updated_At = DateTimeOffset.UtcNow;
                await _uow.Slots.UpdateAsync(slot);
            }

            await _uow.AppointmentStatusHistory.AddAsync(new tbAppointmentStatusHistory
            {
                Appointment_Id = appointment.Id,
                From_Status = AppointmentStatus.Reserved,
                To_Status = AppointmentStatus.Confirmed,
                Changed_At = DateTimeOffset.UtcNow,
                Changed_By_User_Id = changedByUserId
            });

            await _uow.SaveChangeAsync();
            await _uow.CommitTransactionAsync();
            
            return ResponseStatus.Success;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await _uow.RollbackTransactionAsync();
            _logger.LogWarning(ex, "Appointment {AppointmentId} was modified concurrently while confirming", id);
            return ResponseStatus.Conflict;
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to confirm appointment {AppointmentId}", id);
            return ResponseStatus.Fail;
        }
    }

    public async Task<string> CancelAppointmentAsync(Guid id, Guid? changedByUserId)
    {
        var appointment = await _uow.Appointments.GetByIdAsync(id);
        if (appointment is null) return ResponseStatus.DoestNotExist;
        
        if (appointment.Status == AppointmentStatus.Cancelled)
            return ResponseStatus.Conflict;

        var fromStatus = appointment.Status;
        var slot = await _uow.Slots.GetByIdAsync(appointment.Slot_Id);
        
        await _uow.BeginTransactionAsync();
        
        try
        {
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.Updated_At = DateTimeOffset.UtcNow;
            await _uow.Appointments.UpdateAsync(appointment);

            if (slot is not null)
            {
                slot.Status = SlotStatus.Available;
                slot.Updated_At = DateTimeOffset.UtcNow;
                await _uow.Slots.UpdateAsync(slot);
            }
            
            await _uow.AppointmentStatusHistory.AddAsync(new tbAppointmentStatusHistory
            {
                Appointment_Id = appointment.Id,
                From_Status = fromStatus,
                To_Status = AppointmentStatus.Cancelled,
                Changed_At = DateTimeOffset.UtcNow,
                Changed_By_User_Id = changedByUserId
            });
            
            await _uow.SaveChangeAsync();
            await _uow.CommitTransactionAsync();

            if (slot is not null)
                await _slotHelper.InvalidateAsync(slot.Doctor_Id);
            
            return ResponseStatus.Success;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await _uow.RollbackTransactionAsync();
            _logger.LogWarning(ex, "Appointment {AppointmentId} was modified concurrently while cancelling", id);
            return ResponseStatus.Conflict;
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to cancel appointment {AppointmentId}", id);
            return ResponseStatus.Fail;
        }
    }
}