using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace UnitTesting
{
    public class MigrationDriftTests
    {
        // Fails the build the moment an entity/enum/index change isn't captured by a
        // `dotnet ef migrations add` — catches schema drift before it reaches a real database.
        [Fact]
        public void Model_Has_No_Changes_Missing_From_A_Migration()
        {
            var options = new DbContextOptionsBuilder<BookingDBContext>()
                .UseNpgsql("Host=localhost;Database=migration-check")
                .Options;

            using var context = new BookingDBContext(options);

            Assert.False(context.Database.HasPendingModelChanges());
        }
    }
}
