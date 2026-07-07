using Data.Dtos;
using Data.Enums;
using Data.Models;
using Infra.Services;
using Infra.UnitOfWork;
using Microsoft.Extensions.Configuration;

namespace Infra.Utility;

public class SlotAvailabilityHelper
{
    private static readonly TimeSpan CacheExpireTime = TimeSpan.FromMinutes(5);
    
    private readonly ICacheService _cacheService;
    private readonly IBookingUnitOfWork _uow;

    public SlotAvailabilityHelper(IBookingUnitOfWork uow, ICacheService cacheService)
    {
        _cacheService = cacheService;
        _uow = uow;
    }
    
    private static string CacheKey(Guid doctorId) => $"slots:availability:{doctorId}";

    public async Task<List<AvailableSlotDto>> GetAvailableSlotAsync(Guid doctorId)
    {
        var cached = await _cacheService.GetAsync<List<AvailableSlotDto>>(CacheKey(doctorId));
        
        if(cached is not null)
            return cached;
        
        var slots = await _uow.Slots.FindAsync(s => s.Doctor_Id == doctorId 
                                                    && s.Status == SlotStatus.Available);

        var result = slots.OrderBy(s => s.Start_Time)
            .Select(s => new AvailableSlotDto(s.Id, s.Start_Time, s.End_Time)).ToList();
        
        await _cacheService.SetAsync(CacheKey(doctorId), result, CacheExpireTime);
        
        return result;
    }
}