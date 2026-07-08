using Data.Dtos;
using Data.Enums;
using Data.Models;
using Infra.UnitOfWork;
using Infra.Utility;
using MediBookAPI.Services.SlotServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTesting;

public class SlotServiceTest
{
    private static (SlotService Service, BookingDBContext Context, FakeCacheService Cache) CreateService(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<BookingDBContext>()
                            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                            .Options;

            var context = new BookingDBContext(options);
            var uow = new BookingUnitOfWork(context);
            var cache = new FakeCacheService();
            var slotAvailabilityHelper = new SlotAvailabilityHelper(uow, cache);
            var service = new SlotService(uow, slotAvailabilityHelper, NullLogger<SlotService>.Instance!);

            return (service, context, cache);
        }

        private static tbDoctors NewDoctor(bool isActive = true)
        {
            var timestamp = DateTimeOffset.UtcNow;
            return new tbDoctors
            {
                Id = Guid.NewGuid(),
                Full_Name = "Dr. Jane Doe",
                Specialization = "Cardiology",
                Email = $"{Guid.NewGuid():N}@medibook.test",
                Is_Active = isActive,
                Created_At = timestamp,
                Updated_At = timestamp
            };
        }

        private static tbSlots NewSlot(Guid doctorId, SlotStatus status = SlotStatus.Available, DateTime? startTime = null)
        {
            var timestamp = DateTimeOffset.UtcNow;
            var start = startTime ?? DateTime.UtcNow.AddDays(1);
            return new tbSlots
            {
                Id = Guid.NewGuid(),
                Doctor_Id = doctorId,
                Start_Time = start,
                End_Time = start.AddMinutes(30),
                Status = status,
                Created_At = timestamp,
                Updated_At = timestamp
            };
        }

        [Fact]
        public async Task CreateSlotAsync_ActiveDoctor_ReturnsSuccessAndPersistsAvailableSlot()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor();
            context.tbDoctors.Add(doctor);
            await context.SaveChangesAsync();

            var start = DateTime.UtcNow.AddDays(1);
            var dto = new CreateSlotRequestDto { Doctor_Id = doctor.Id, Start_Time = start, End_Time = start.AddMinutes(30) };

            var (status, slotId) = await service.CreateSlotAsync(dto);

            Assert.Equal(ResponseStatus.Success, status);
            Assert.NotNull(slotId);

            var saved = await context.tbSlots.SingleAsync(s => s.Id == slotId);
            Assert.Equal(SlotStatus.Available, saved.Status);
            Assert.Equal(doctor.Id, saved.Doctor_Id);
        }

        [Fact]
        public async Task CreateSlotAsync_UnknownDoctor_ReturnsDoesNotExist()
        {
            var (service, _, _) = CreateService();
            var start = DateTime.UtcNow.AddDays(1);

            var (status, slotId) = await service.CreateSlotAsync(new CreateSlotRequestDto
            {
                Doctor_Id = Guid.NewGuid(),
                Start_Time = start,
                End_Time = start.AddMinutes(30)
            });

            Assert.Equal(ResponseStatus.DoestNotExist, status);
            Assert.Null(slotId);
        }

        [Fact]
        public async Task CreateSlotAsync_InactiveDoctor_ReturnsDoesNotExist()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor(isActive: false);
            context.tbDoctors.Add(doctor);
            await context.SaveChangesAsync();

            var start = DateTime.UtcNow.AddDays(1);
            var (status, slotId) = await service.CreateSlotAsync(new CreateSlotRequestDto
            {
                Doctor_Id = doctor.Id,
                Start_Time = start,
                End_Time = start.AddMinutes(30)
            });

            Assert.Equal(ResponseStatus.DoestNotExist, status);
            Assert.Null(slotId);
        }

        [Fact]
        public async Task CreateSlotAsync_EndTimeNotAfterStartTime_ReturnsFail()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor();
            context.tbDoctors.Add(doctor);
            await context.SaveChangesAsync();

            var start = DateTime.UtcNow.AddDays(1);
            var (status, slotId) = await service.CreateSlotAsync(new CreateSlotRequestDto
            {
                Doctor_Id = doctor.Id,
                Start_Time = start,
                End_Time = start // same instant — invalid
            });

            Assert.Equal(ResponseStatus.Fail, status);
            Assert.Null(slotId);
        }

        [Fact]
        public async Task CreateSlotAsync_Success_InvalidatesDoctorAvailabilityCache()
        {
            var (service, context, cache) = CreateService();
            var doctor = NewDoctor();
            context.tbDoctors.Add(doctor);
            await context.SaveChangesAsync();

            var start = DateTime.UtcNow.AddDays(1);
            await service.CreateSlotAsync(new CreateSlotRequestDto { Doctor_Id = doctor.Id, Start_Time = start, End_Time = start.AddMinutes(30) });

            Assert.Contains($"slots:availability:{doctor.Id}", cache.RemovedKeys);
        }

        [Fact]
        public async Task GetSlotByIdAsync_ExistingId_ReturnsDto()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor();
            var slot = NewSlot(doctor.Id);
            context.tbDoctors.Add(doctor);
            context.tbSlots.Add(slot);
            await context.SaveChangesAsync();

            var result = await service.GetSlotAsync(slot.Id);

            Assert.NotNull(result);
            Assert.Equal(slot.Id, result!.Id);
            Assert.Equal("Available", result.Status);
        }

        [Fact]
        public async Task GetSlotByIdAsync_UnknownId_ReturnsNull()
        {
            var (service, _, _) = CreateService();

            var result = await service.GetSlotAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAvailableSlotsAsync_ReturnsOnlyAvailableSlotsOrderedByStartTime()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor();
            var later = NewSlot(doctor.Id, startTime: DateTime.UtcNow.AddDays(3));
            var sooner = NewSlot(doctor.Id, startTime: DateTime.UtcNow.AddDays(1));
            var reserved = NewSlot(doctor.Id, SlotStatus.Reserved, startTime: DateTime.UtcNow.AddDays(2));
            context.tbDoctors.Add(doctor);
            context.tbSlots.AddRange(later, sooner, reserved);
            await context.SaveChangesAsync();

            var result = await service.GetAvailableSlotsAsync(doctor.Id);

            Assert.Equal(2, result.Count);
            Assert.Equal(sooner.Id, result[0].SlotId);
            Assert.Equal(later.Id, result[1].SlotId);
        }

        [Fact]
        public async Task CancelSlotAsync_AvailableSlot_ReturnsSuccessAndMarksCancelled()
        {
            var (service, context, cache) = CreateService();
            var doctor = NewDoctor();
            var slot = NewSlot(doctor.Id);
            context.tbDoctors.Add(doctor);
            context.tbSlots.Add(slot);
            await context.SaveChangesAsync();

            var status = await service.CancelSlotAsync(slot.Id);

            Assert.Equal(ResponseStatus.Success, status);
            var updated = await context.tbSlots.SingleAsync(s => s.Id == slot.Id);
            Assert.Equal(SlotStatus.Cancelled, updated.Status);
            Assert.Contains($"slots:availability:{doctor.Id}", cache.RemovedKeys);
        }

        [Fact]
        public async Task CancelSlotAsync_AlreadyReservedSlot_ReturnsConflictAndLeavesSlotUntouched()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor();
            var slot = NewSlot(doctor.Id, SlotStatus.Reserved);
            context.tbDoctors.Add(doctor);
            context.tbSlots.Add(slot);
            await context.SaveChangesAsync();

            var status = await service.CancelSlotAsync(slot.Id);

            Assert.Equal(ResponseStatus.Conflict, status);
            var unchanged = await context.tbSlots.SingleAsync(s => s.Id == slot.Id);
            Assert.Equal(SlotStatus.Reserved, unchanged.Status);
        }

        [Fact]
        public async Task CancelSlotAsync_UnknownId_ReturnsDoesNotExist()
        {
            var (service, _, _) = CreateService();

            var status = await service.CancelSlotAsync(Guid.NewGuid());

            Assert.Equal(ResponseStatus.DoestNotExist, status);
        }
}