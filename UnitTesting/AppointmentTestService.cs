using Data.Dtos;
using Data.Enums;
using Data.Models;
using Infra.UnitOfWork;
using Infra.Utility;
using MediBookAPI.Services.AppointmentServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTesting;

public class AppointmentTestService
{
    // dbName lets two independent (context, uow, service) instances share the same
    // InMemory database — used below to simulate a second caller racing for the same
     private static (AppointmentService Service, BookingDBContext Context, FakeCacheService Cache) CreateService(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<BookingDBContext>()
                            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                            .Options;

            var context = new BookingDBContext(options);
            var uow = new BookingUnitOfWork(context);
            var cache = new FakeCacheService();
            var slotAvailabilityHelper = new SlotAvailabilityHelper(uow, cache);
            var service = new AppointmentService(uow, slotAvailabilityHelper, NullLogger<AppointmentService>.Instance!);

            return (service, context, cache);
        }

        private static tbDoctors NewDoctor()
        {
            var timestamp = DateTimeOffset.UtcNow;
            return new tbDoctors
            {
                Id = Guid.NewGuid(),
                Full_Name = "Dr. Jane Doe",
                Specialization = "Cardiology",
                Email = $"{Guid.NewGuid():N}@medibook.test",
                Is_Active = true,
                Created_At = timestamp,
                Updated_At = timestamp
            };
        }

        private static tbPatients NewPatient()
        {
            var timestamp = DateTimeOffset.UtcNow;
            return new tbPatients
            {
                Id = Guid.NewGuid(),
                Full_Name = "John Patient",
                Email = $"{Guid.NewGuid():N}@medibook.test",
                Created_At = timestamp,
                Updated_At = timestamp
            };
        }

        private static tbSlots NewSlot(Guid doctorId, SlotStatus status = SlotStatus.Available)
        {
            var timestamp = DateTimeOffset.UtcNow;
            var start = DateTime.UtcNow.AddDays(1);
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

        private static tbAppointments NewAppointment(tbSlots slot, Guid patientId, AppointmentStatus status = AppointmentStatus.Reserved)
        {
            var timestamp = DateTimeOffset.UtcNow;
            return new tbAppointments
            {
                Id = Guid.NewGuid(),
                Slot_Id = slot.Id,
                Doctor_Id = slot.Doctor_Id,
                Patient_Id = patientId,
                Status = status,
                Created_At = timestamp,
                Updated_At = timestamp
            };
        }

        [Fact]
        public async Task BookAppointmentAsync_AvailableSlot_ReturnsSuccessAndReservesSlot()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor();
            var patient = NewPatient();
            var slot = NewSlot(doctor.Id);
            context.tbDoctors.Add(doctor);
            context.tbPatients.Add(patient);
            context.tbSlots.Add(slot);
            await context.SaveChangesAsync();

            var (status, appointmentId) = await service.BookAppointmentAsync(new BookAppointmentRequestDto
            {
                SlotId = slot.Id,
                PatientId = patient.Id
            });

            Assert.Equal(ResponseStatus.Success, status);
            Assert.NotNull(appointmentId);

            var savedAppointment = await context.tbAppointments.SingleAsync(a => a.Id == appointmentId);
            Assert.Equal(AppointmentStatus.Reserved, savedAppointment.Status);
            Assert.Equal(patient.Id, savedAppointment.Patient_Id);

            var updatedSlot = await context.tbSlots.SingleAsync(s => s.Id == slot.Id);
            Assert.Equal(SlotStatus.Reserved, updatedSlot.Status);
        }

        [Fact]
        public async Task BookAppointmentAsync_Success_WritesReservedStatusHistoryRow()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor();
            var patient = NewPatient();
            var slot = NewSlot(doctor.Id);
            context.tbDoctors.Add(doctor);
            context.tbPatients.Add(patient);
            context.tbSlots.Add(slot);
            await context.SaveChangesAsync();

            var (_, appointmentId) = await service.BookAppointmentAsync(new BookAppointmentRequestDto { SlotId = slot.Id, PatientId = patient.Id });

            var history = await context.tbAppointmentStatusHistory.SingleAsync(h => h.Appointment_Id == appointmentId);
            Assert.Null(history.From_Status);
            Assert.Equal(AppointmentStatus.Reserved, history.To_Status);
        }

        [Fact]
        public async Task BookAppointmentAsync_Success_InvalidatesDoctorAvailabilityCache()
        {
            var (service, context, cache) = CreateService();
            var doctor = NewDoctor();
            var patient = NewPatient();
            var slot = NewSlot(doctor.Id);
            context.tbDoctors.Add(doctor);
            context.tbPatients.Add(patient);
            context.tbSlots.Add(slot);
            await context.SaveChangesAsync();

            await service.BookAppointmentAsync(new BookAppointmentRequestDto { SlotId = slot.Id, PatientId = patient.Id });

            Assert.Contains($"slots:availability:{doctor.Id}", cache.RemovedKeys);
        }

        [Fact]
        public async Task BookAppointmentAsync_UnknownSlot_ReturnsDoesNotExist()
        {
            var (service, context, _) = CreateService();
            var patient = NewPatient();
            context.tbPatients.Add(patient);
            await context.SaveChangesAsync();

            var (status, appointmentId) = await service.BookAppointmentAsync(new BookAppointmentRequestDto
            {
                SlotId = Guid.NewGuid(),
                PatientId = patient.Id
            });

            Assert.Equal(ResponseStatus.DoestNotExist, status);
            Assert.Null(appointmentId);
        }

        [Fact]
        public async Task BookAppointmentAsync_UnknownPatient_ReturnsDoesNotExist()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor();
            var slot = NewSlot(doctor.Id);
            context.tbDoctors.Add(doctor);
            context.tbSlots.Add(slot);
            await context.SaveChangesAsync();

            var (status, appointmentId) = await service.BookAppointmentAsync(new BookAppointmentRequestDto
            {
                SlotId = slot.Id,
                PatientId = Guid.NewGuid()
            });

            Assert.Equal(ResponseStatus.DoestNotExist, status);
            Assert.Null(appointmentId);
        }

        [Fact]
        public async Task BookAppointmentAsync_SlotAlreadyReserved_ReturnsConflict()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor();
            var patient = NewPatient();
            var slot = NewSlot(doctor.Id, SlotStatus.Reserved);
            context.tbDoctors.Add(doctor);
            context.tbPatients.Add(patient);
            context.tbSlots.Add(slot);
            await context.SaveChangesAsync();

            var (status, appointmentId) = await service.BookAppointmentAsync(new BookAppointmentRequestDto
            {
                SlotId = slot.Id,
                PatientId = patient.Id
            });

            Assert.Equal(ResponseStatus.Conflict, status);
            Assert.Null(appointmentId);
        }

        /// <summary>
        /// BookAppointmentAsync_TwoCallersRaceForSameSlot_OnlyFirstSucceeds
        /// Simulates the core double-booking race: two independent request-scoped
        /// (DbContext, UnitOfWork, Service) instances — sharing one InMemory database —
        /// both try to book the same slot.
        /// The first caller's commit flips the slot to Reserved, so the second caller's own
        /// pre-check (backed by xmin + the partial unique index against a real Postgres database)
        /// sees a non-Available slot and is turned away with Conflict.
        /// A true xmin-mismatch race additionally requires a relational provider and belongs in an integration test.
        /// </summary>
        [Fact]
        public async Task BookAppointmentAsync_TwoCallersRaceForSameSlot_OnlyFirstSucceeds()
        {
            var dbName = Guid.NewGuid().ToString();
            var (serviceA, contextA, _) = CreateService(dbName);
            var doctor = NewDoctor();
            var patientA = NewPatient();
            var patientB = NewPatient();
            var slot = NewSlot(doctor.Id);
            contextA.tbDoctors.Add(doctor);
            contextA.tbPatients.AddRange(patientA, patientB);
            contextA.tbSlots.Add(slot);
            await contextA.SaveChangesAsync();

            var (serviceB, _, _) = CreateService(dbName);

            var (statusA, appointmentIdA) = await serviceA.BookAppointmentAsync(new BookAppointmentRequestDto { SlotId = slot.Id, PatientId = patientA.Id });
            var (statusB, appointmentIdB) = await serviceB.BookAppointmentAsync(new BookAppointmentRequestDto { SlotId = slot.Id, PatientId = patientB.Id });

            Assert.Equal(ResponseStatus.Success, statusA);
            Assert.NotNull(appointmentIdA);

            Assert.Equal(ResponseStatus.Conflict, statusB);
            Assert.Null(appointmentIdB);
        }

        [Fact]
        public async Task GetAppointmentByIdAsync_ExistingId_ReturnsDtoWithJoinedDoctorAndPatientNames()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor();
            var patient = NewPatient();
            var slot = NewSlot(doctor.Id, SlotStatus.Reserved);
            var appointment = NewAppointment(slot, patient.Id);
            context.tbDoctors.Add(doctor);
            context.tbPatients.Add(patient);
            context.tbSlots.Add(slot);
            context.tbAppointments.Add(appointment);
            await context.SaveChangesAsync();

            var result = await service.GetAppointmentByIdAsync(appointment.Id);

            Assert.NotNull(result);
            Assert.Equal(doctor.Full_Name, result!.DoctorName);
            Assert.Equal(patient.Full_Name, result.PatientName);
            Assert.Equal(slot.Start_Time, result.StartTime);
            Assert.Equal("Reserved", result.Status);
        }

        [Fact]
        public async Task GetAppointmentByIdAsync_UnknownId_ReturnsNull()
        {
            var (service, _, _) = CreateService();

            var result = await service.GetAppointmentByIdAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task ConfirmAppointmentAsync_ReservedAppointment_ReturnsSuccessAndConfirmsSlotAndAppointment()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor();
            var patient = NewPatient();
            var slot = NewSlot(doctor.Id, SlotStatus.Reserved);
            var appointment = NewAppointment(slot, patient.Id);
            context.tbDoctors.Add(doctor);
            context.tbPatients.Add(patient);
            context.tbSlots.Add(slot);
            context.tbAppointments.Add(appointment);
            await context.SaveChangesAsync();

            var status = await service.ConfirmAppointmentAsync(appointment.Id, changedByUserId: null);

            Assert.Equal(ResponseStatus.Success, status);

            var updatedAppointment = await context.tbAppointments.SingleAsync(a => a.Id == appointment.Id);
            var updatedSlot = await context.tbSlots.SingleAsync(s => s.Id == slot.Id);
            Assert.Equal(AppointmentStatus.Confirmed, updatedAppointment.Status);
            Assert.Equal(SlotStatus.Confirmed, updatedSlot.Status);

            var history = await context.tbAppointmentStatusHistory.SingleAsync(h => h.Appointment_Id == appointment.Id);
            Assert.Equal(AppointmentStatus.Reserved, history.From_Status);
            Assert.Equal(AppointmentStatus.Confirmed, history.To_Status);
        }

        [Fact]
        public async Task ConfirmAppointmentAsync_AlreadyConfirmedAppointment_ReturnsConflict()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor();
            var patient = NewPatient();
            var slot = NewSlot(doctor.Id, SlotStatus.Confirmed);
            var appointment = NewAppointment(slot, patient.Id, AppointmentStatus.Confirmed);
            context.tbDoctors.Add(doctor);
            context.tbPatients.Add(patient);
            context.tbSlots.Add(slot);
            context.tbAppointments.Add(appointment);
            await context.SaveChangesAsync();

            var status = await service.ConfirmAppointmentAsync(appointment.Id, changedByUserId: null);

            Assert.Equal(ResponseStatus.Conflict, status);
        }

        [Fact]
        public async Task ConfirmAppointmentAsync_UnknownId_ReturnsDoesNotExist()
        {
            var (service, _, _) = CreateService();

            var status = await service.ConfirmAppointmentAsync(Guid.NewGuid(), changedByUserId: null);

            Assert.Equal(ResponseStatus.DoestNotExist, status);
        }

        [Fact]
        public async Task CancelAppointmentAsync_ReservedAppointment_ReturnsSuccessAndFreesSlotBackToAvailable()
        {
            var (service, context, cache) = CreateService();
            var doctor = NewDoctor();
            var patient = NewPatient();
            var slot = NewSlot(doctor.Id, SlotStatus.Reserved);
            var appointment = NewAppointment(slot, patient.Id);
            context.tbDoctors.Add(doctor);
            context.tbPatients.Add(patient);
            context.tbSlots.Add(slot);
            context.tbAppointments.Add(appointment);
            await context.SaveChangesAsync();

            var changedBy = Guid.NewGuid();
            var status = await service.CancelAppointmentAsync(appointment.Id, changedBy);

            Assert.Equal(ResponseStatus.Success, status);

            var updatedAppointment = await context.tbAppointments.SingleAsync(a => a.Id == appointment.Id);
            var updatedSlot = await context.tbSlots.SingleAsync(s => s.Id == slot.Id);
            Assert.Equal(AppointmentStatus.Cancelled, updatedAppointment.Status);
            Assert.Equal(SlotStatus.Available, updatedSlot.Status);
            Assert.Contains($"slots:availability:{doctor.Id}", cache.RemovedKeys);

            var history = await context.tbAppointmentStatusHistory.SingleAsync(h => h.Appointment_Id == appointment.Id);
            Assert.Equal(AppointmentStatus.Reserved, history.From_Status);
            Assert.Equal(AppointmentStatus.Cancelled, history.To_Status);
            Assert.Equal(changedBy, history.Changed_By_User_Id);
        }

        [Fact]
        public async Task CancelAppointmentAsync_AlreadyCancelled_ReturnsConflict()
        {
            var (service, context, _) = CreateService();
            var doctor = NewDoctor();
            var patient = NewPatient();
            var slot = NewSlot(doctor.Id, SlotStatus.Cancelled);
            var appointment = NewAppointment(slot, patient.Id, AppointmentStatus.Cancelled);
            context.tbDoctors.Add(doctor);
            context.tbPatients.Add(patient);
            context.tbSlots.Add(slot);
            context.tbAppointments.Add(appointment);
            await context.SaveChangesAsync();

            var status = await service.CancelAppointmentAsync(appointment.Id, changedByUserId: null);

            Assert.Equal(ResponseStatus.Conflict, status);
        }

        [Fact]
        public async Task CancelAppointmentAsync_UnknownId_ReturnsDoesNotExist()
        {
            var (service, _, _) = CreateService();

            var status = await service.CancelAppointmentAsync(Guid.NewGuid(), changedByUserId: null);

            Assert.Equal(ResponseStatus.DoestNotExist, status);
        }
}