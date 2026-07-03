using Data.Dtos;
using Infra.Utility;

namespace MediBookAPI.Services.DoctorServices
{
    public interface IDoctorService
    {
        Task<string> InsertNewDoctor(CreateDoctorRequestDto dto);
        Task<DoctorDto?> GetDoctorById(Guid id);

        Task<Model<DoctorDto>> GetDoctorByPaging(int page = 1, int pageSize = 10, string sort = "Created_At", 
                                                    string sortBy = "desc", string? search = null);
        Task<string> UpdateDoctorData(UpdateDoctorRequestDto dto);
        Task<string> SoftDeleteDoctor(Guid id);
        Task<string> HardDeleteDoctor(Guid id);
    }
}
