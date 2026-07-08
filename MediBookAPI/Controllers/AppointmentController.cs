using Data.Dtos;
using Data.Enums;
using MediBookAPI.Services.AppointmentServices;
using Microsoft.AspNetCore.Mvc;

namespace MediBookAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AppointmentController : Controller
{
    private readonly IAppointmentService _appointmentService;
    
    public AppointmentController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }
    
    [HttpPost]
    public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentRequestDto dto)
    {
        var (status, appointmentId) = await _appointmentService.BookAppointmentAsync(dto);
        return status switch
        {
            ResponseStatus.Success => CreatedAtAction(nameof(GetAppointmentById), new { id = appointmentId }, new { appointmentId, status }),
            ResponseStatus.DoestNotExist => NotFound(status),
            ResponseStatus.Conflict => Conflict(status),
            ResponseStatus.Fail => BadRequest(status),
            _ => StatusCode(StatusCodes.Status500InternalServerError, status)
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAppointmentById(Guid id)
    {
        var result = await _appointmentService.GetAppointmentByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}/confirm")]
    public async Task<IActionResult> ConfirmAppointment(Guid id, [FromQuery] Guid? changedByUserId)
    {
        var status = await _appointmentService.ConfirmAppointmentAsync(id, changedByUserId);
        return ToActionResult(status);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> CancelAppointment(Guid id, [FromQuery] Guid? changedByUserId)
    {
        var status = await _appointmentService.CancelAppointmentAsync(id, changedByUserId);
        return ToActionResult(status);
    }

    private IActionResult ToActionResult(string status) => status switch
    {
        ResponseStatus.Success => NoContent(),
        ResponseStatus.DoestNotExist => NotFound(status),
        ResponseStatus.Conflict => Conflict(status),
        ResponseStatus.Fail => BadRequest(status),
        _ => StatusCode(StatusCodes.Status500InternalServerError, status)
    };
}