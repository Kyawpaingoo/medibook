using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace UnitTesting;

public class ConcurrencyConfigurationTest
{
    /// <summary>
    /// Entity_Uses_Xmin_As_Concurrency_Token
    /// Guards the concurrency-control migration itself: if the "xmin" shadow property
    /// is ever removed or misconfigured on tbSlots/tbAppointments, double-booking
    /// protection silently degrades to "last write wins" — this fails loudly instead.
    /// </summary>
    [Theory]
    [InlineData(typeof(tbSlots))]
    [InlineData(typeof(tbAppointments))]
    public void Entity_Uses_Xmin_As_Concurrency_Token(Type entityType)
    {
        var options = new DbContextOptionsBuilder<BookingDBContext>()
            .UseNpgsql("Host=localhost;Database=concurrency-token-check")
            .Options;

        using var context = new BookingDBContext(options);

        var xminProperty = context.Model.FindEntityType(entityType)!.FindProperty("xmin");

        Assert.NotNull(xminProperty);
        Assert.True(xminProperty!.IsConcurrencyToken);
    }
}