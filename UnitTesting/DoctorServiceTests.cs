using Data.Dtos;
using Data.Enums;
using Data.Models;
using Infra.UnitOfWork;
using MediBookAPI.Services.DoctorServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTesting
{
    public class DoctorServiceTests
    {
        private static (DoctorService Service, BookingDBContext Context) CreateService()
        {
            var options = new DbContextOptionsBuilder<BookingDBContext>()
                            .UseInMemoryDatabase(Guid.NewGuid().ToString())
                            .Options;

            var context = new BookingDBContext(options);
            var uow = new BookingUnitOfWork(context);
            var service = new DoctorService(uow, NullLogger<DoctorService>.Instance!);

            return (service, context);
        }

        private static tbDoctors NewDoctor(
            string email = "jane.doe@medibook.test",
            string fullName = "Dr. Jane Doe",
            string specialization = "Cardiology",
            bool isActive = true,
            DateTimeOffset? createdAt = null
        )
        {
            var timestamp = createdAt ?? DateTimeOffset.UtcNow;
            return new tbDoctors
            {
                Id = Guid.NewGuid(),
                Full_Name = fullName,
                Specialization = specialization,
                Email = email,
                Phone_Number = "0912345678",
                Is_Active = isActive,
                Created_At = timestamp,
                Updated_At = timestamp
            };
        }

        [Fact]
        public async Task InsertNewDoctor_ValidDto_ReturnsSuccessAndDoctorData()
        {
            var (service, context) = CreateService();
            var dto = new CreateDoctorRequestDto
            {
                Full_Name = "Dr. John Smith",
                Specialization = "Neurology",
                Email = "john.smith@medibook.test",
                Phone_Number = "0911111111"
            };

            var result = await service.InsertNewDoctor(dto);

            Assert.Equal(ResponseStatus.Success, result);
            var saved = await context.tbDoctors.SingleAsync();

            Assert.Equal(dto.Email, saved.Email);
            Assert.True(saved.Is_Active);
        }

        [Fact]
        public async Task InsertNewDoctor_WhenSaveFails_ReturnsFail()
        {
            var (service, context) = CreateService();
            // Force the save to fail so the try/catch in InsertNewDoctor is actually
            // exercised. The InMemory provider doesn't enforce the unique-email index,
            // so a disposed context is a reliable, provider-agnostic way to trigger a
            // persistence failure instead.
            await context.DisposeAsync();

            var dto = new CreateDoctorRequestDto
            {
                Full_Name = "Dr. Copycat",
                Specialization = "Dermatology",
                Email = "dup@medibook.test"
            };

            var result = await service.InsertNewDoctor(dto);

            Assert.Equal(ResponseStatus.Fail, result);
        }

        [Fact]
        public async Task GetDoctorById_ExistingID_ReturnsDtoResult()
        {
            var (service, context) = CreateService();
            var doctor = NewDoctor();
            context.tbDoctors.Add(doctor);

            await context.SaveChangesAsync();

            var result = await service.GetDoctorById(doctor.Id);

            Assert.NotNull(result);
            Assert.Equal(doctor.Id, result!.Id);
            Assert.Equal(doctor.Full_Name, result.Full_Name);
            Assert.Equal(doctor.Email, result.Email);
        }

        [Fact]
        public async Task GetDoctorById_UnknownId_ReturnsNull()
        {
            var (service, context) = CreateService();

            var result = await service.GetDoctorById(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateDoctorData_ExistingData_UpdateFieldsAndReturnsSuccess()
        {
            var (service, context) = CreateService();

            var doctor = NewDoctor();
            context.tbDoctors.Add(doctor);

            await context.SaveChangesAsync();

            var dto = new UpdateDoctorRequestDto
            {
                Id = doctor.Id,
                Full_Name = "Dr. Jane Updated",
                Specialization = "Oncology",
                Email = doctor.Email,
                Phone_Number = "0999999999"
            };

            var result = await service.UpdateDoctorData(dto);

            Assert.Equal(ResponseStatus.Success, result);
            var updated = await context.tbDoctors.SingleAsync(d => d.Id == doctor.Id);

            Assert.NotNull(updated);
            Assert.Equal(dto.Full_Name, updated.Full_Name);
            Assert.Equal(dto.Specialization, updated.Specialization);
            Assert.Equal(dto.Phone_Number, updated.Phone_Number);
        }

        [Fact]
        public async Task UpdateDoctorData_UnknownData_NotUpdateFieldsAndReturnFail()
        {
            var (service, context) = CreateService();

            var doctor = NewDoctor();
            context.tbDoctors.Add(doctor);

            await context.SaveChangesAsync();

            var dto = new UpdateDoctorRequestDto
            {
                Id = Guid.NewGuid(),
                Full_Name = "Dr. Jane Updated",
                Specialization = "Oncology",
                Email = doctor.Email,
                Phone_Number = "0999999999"
            };

            var result = await service.UpdateDoctorData(dto);

            Assert.Equal(ResponseStatus.DoestNotExist, result);
        }

        [Fact]
        public async Task SoftDeleteDoctorData_ExistingId_ReturnSuccess()
        {
            var (service, context) = CreateService();

            var doctor = NewDoctor();
            context.tbDoctors.Add(doctor);

            await context.SaveChangesAsync();

            var result = await service.SoftDeleteDoctor(doctor.Id);

            Assert.Equal(ResponseStatus.Success, result);
        }

        [Fact]
        public async Task HardDeleteDoctorData_UnknownId_ReturnsDoesNotExist()
        {
            var (service, _) = CreateService();

            var result = await service.HardDeleteDoctor(Guid.NewGuid());

            Assert.Equal(ResponseStatus.DoestNotExist, result);
        }

        [Fact]
        public async Task HardDeleteDoctorData_ExistingId_ReturnSuccess()
        {
            var (service, context) = CreateService();

            var doctor = NewDoctor();
            context.tbDoctors.Add(doctor);

            await context.SaveChangesAsync();

            var result = await service.HardDeleteDoctor(doctor.Id);

            Assert.Equal(ResponseStatus.Success, result);
        }

        [Fact]
        public async Task SoftDeleteDoctorData_UnknownId_ReturnsDoesNotExist()
        {
            var (service, _) = CreateService();

            var result = await service.SoftDeleteDoctor(Guid.NewGuid());

            Assert.Equal(ResponseStatus.DoestNotExist, result);
        }
    }
}
