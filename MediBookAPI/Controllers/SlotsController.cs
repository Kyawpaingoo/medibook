using Data.Dtos;
using Data.Enums;
using MediBookAPI.Services.SlotServices;
using Microsoft.AspNetCore.Mvc;

namespace MediBookAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SlotsController : ControllerBase
{
    private readonly ISlotService _slotService;
    
    public SlotsController(ISlotService slotService)
    {
        _slotService = slotService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateSlot([FromBody] CreateSlotRequestDto dto)
    {
        var (status, slotId) = await _slotService.CreateSlotAsync(dto);

        return status switch
        {
            ResponseStatus.Success => CreatedAtAction(nameof(GetSlotById), new { id = slotId }, new { slotId, status }),
            ResponseStatus.DoestNotExist => NotFound(status),
            ResponseStatus.Conflict => Conflict(status),
            ResponseStatus.Fail => BadRequest(status),
            _ => StatusCode(StatusCodes.Status500InternalServerError, status)
        };
    }
    
    [HttpGet("getbyid")]
    public async Task<IActionResult> GetSlotById(Guid id)
    {
        var result = await _slotService.GetSlotAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableSlots(Guid doctorId)
    {
        var result = await _slotService.GetAvailableSlotsAsync(doctorId);
        return Ok(result);
    }

    [HttpDelete("cancel")]
    public async Task<IActionResult> CancelSlot(Guid id)
    {
        var status = await _slotService.CancelSlotAsync(id);
        return status switch
        {
            ResponseStatus.Success => NoContent(),
            ResponseStatus.DoestNotExist => NotFound(status),
            ResponseStatus.Conflict => Conflict(status),
            ResponseStatus.Fail => BadRequest(status),
            _ => StatusCode(StatusCodes.Status500InternalServerError, status)
        };
    }
}