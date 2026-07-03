using Data.Dtos;
using Data.Enums;
using Data.Models;
using Infra.UnitOfWork;
using Infra.Utility;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MediBookAPI.Services.DoctorServices
{
    public class DoctorService : IDoctorService
    {
        private readonly IBookingUnitOfWork _uow;
        private readonly ILogger<DoctorService> _logger;

        private static readonly HashSet<string> SortableFields = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(DoctorDto.Full_Name),
            nameof(DoctorDto.Specialization),
            nameof(DoctorDto.Email),
            nameof(DoctorDto.Created_At)
        };

        public DoctorService(IBookingUnitOfWork uow, ILogger<DoctorService> logger)
        {
            _uow = uow;
            _logger = logger;
        }
        public async Task<string> HardDeleteDoctor(Guid id)
        {
            var doctor = await _uow.Doctors.GetByIdAsync(id);

            if (doctor is null) return ResponseStatus.DoestNotExist;

            try {
                await _uow.Doctors.RemoveAsync(doctor);
                await _uow.SaveChangeAsync();
                return ResponseStatus.Success;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error occurred while deleting doctor with ID {DoctorId}", id);
                return ResponseStatus.Fail;
            }
        }

        public async Task<string> SoftDeleteDoctor(Guid id)
        {
            var doctor = await _uow.Doctors.GetByIdAsync(id);

            if (doctor is null) return ResponseStatus.DoestNotExist;

            try {
                doctor.Is_Active = false;
                doctor.Updated_At = DateTime.Now;
                await _uow.Doctors.UpdateAsync(doctor);
                await _uow.SaveChangeAsync();
                return ResponseStatus.Success;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error occurred while deleting doctor with ID {DoctorId}", id);
                return ResponseStatus.Fail;
            }
        }

        public async Task<DoctorDto?> GetDoctorById(Guid id)
        {
            if (id == Guid.Empty)
                return null;

            DoctorDto? result = await _uow.Doctors.GetWithoutTracking().Where(a => a.Id == id)
                                .Select(ConvertToDoctorDto).FirstOrDefaultAsync();

            if (result == null)
                return null;

            return result;
        }

        public Task<Model<DoctorDto>> GetDoctorByPaging(int page = 1, int pageSize = 10, string sort = "Created_At", 
                                                            string sortBy = "desc", string? search = null)
        {
            var query = _uow.Doctors.GetWithoutTracking().Where(a => a.Is_Active == true).Select(ConvertToDoctorDto);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.Full_Name.Contains(search) || a.Specialization.Contains(search));
            }

            var sortField = SortableFields.Contains(sort) ? sort : "Created_At";

            query = SORTLIT<DoctorDto>.Sort(query, sortField, sortBy);

            var result = PagingService<DoctorDto>.getPaging(page, pageSize, query);
            return result;
        }

        public async Task<string> InsertNewDoctor(CreateDoctorRequestDto dto)
        {
            tbDoctors newDoctor = new tbDoctors
            {
                Id = Guid.NewGuid(),
                Full_Name = dto.Full_Name,
                Specialization = dto.Specialization,
                Email = dto.Email,
                Phone_Number = dto.Phone_Number,
                Is_Active = true,
                Created_At = DateTimeOffset.Now,
                Updated_At = DateTimeOffset.Now
            };

            try {
                await _uow.Doctors.AddAsync(newDoctor);
                await _uow.SaveChangeAsync();
                return ResponseStatus.Success;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to create new doctor with name {DoctorName}", dto.Full_Name);
                return ResponseStatus.Fail;
            }
        }

        public async Task<string> UpdateDoctorData(UpdateDoctorRequestDto dto)
        {
            var doctor = await _uow.Doctors.GetByIdAsync(dto.Id);

            if (doctor is null) return ResponseStatus.DoestNotExist;

            doctor.Full_Name = dto.Full_Name;
            doctor.Specialization = dto.Specialization;
            doctor.Email = dto.Email;
            doctor.Phone_Number = dto.Phone_Number;
            doctor.Updated_At = DateTimeOffset.Now;

            try {
                await _uow.Doctors.UpdateAsync(doctor);
                await _uow.SaveChangeAsync();

                return ResponseStatus.Success;
            }
            catch(Exception ex) { 
                _logger.LogError(ex, "Failed to update doctor with ID {DoctorId}", dto.Id);
                return ResponseStatus.Fail;
            }
        }

        private static readonly Expression<Func<tbDoctors, DoctorDto>> ConvertToDoctorDto = 
            a => new DoctorDto
            {
                Id = a.Id,
                Full_Name = a.Full_Name,
                Specialization = a.Specialization,
                Email = a.Email,
                Phone_Number = a.Phone_Number,
                Is_Active = a.Is_Active,
                Created_At = a.Created_At
            };
    }
}
