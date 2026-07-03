using Data.Dtos;
using Data.Enums;
using MediBookAPI.Services.DoctorServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MediBookAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorController : ControllerBase
    {
        private readonly IDoctorService _doctorService;

        public DoctorController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        [HttpGet("getbypaging")]
        public async Task<IActionResult> GetDoctorByPaging(int page = 1, int pageSize = 10, string sort = "Created_At",
                                                            string sortBy = "desc", string? search = null)
        {
            var result = await _doctorService.GetDoctorByPaging(page, pageSize, sort, sortBy, search);
            return Ok(result);
        }

        [HttpGet("getbyid")]
        public async Task<IActionResult> GetDoctorByID(Guid id)
        {
            var result = await _doctorService.GetDoctorById(id);
            return result is null ? NotFound() : Ok(result);
        }
        [HttpDelete("harddelete")]
        public async Task<IActionResult> HardDeleteDoctor(Guid id)
        {
            var result = await _doctorService.HardDeleteDoctor(id);
            return ToActionResult(result);
        }

        [HttpDelete("softdelete")]
        public async Task<IActionResult> SoftDeleteDoctor(Guid id)
        {
            var result = await _doctorService.SoftDeleteDoctor(id);
            return ToActionResult(result);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorRequestDto doctorDto)
        {
            var result = await _doctorService.InsertNewDoctor(doctorDto);
            return ToActionResult(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateDoctor([FromBody] UpdateDoctorRequestDto doctorDto)
        {
            var result = await _doctorService.UpdateDoctorData(doctorDto);
            return ToActionResult(result);
        }

        private IActionResult ToActionResult(string status) => status switch
        {
            ResponseStatus.Success => Ok(status),
            ResponseStatus.DoestNotExist => NotFound(status),
            ResponseStatus.Fail => BadRequest(status),
            _ => StatusCode(StatusCodes.Status500InternalServerError, status)
        };
    }
}
