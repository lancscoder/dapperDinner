using System.Data.Entity;

namespace DapperDinner.Models
{
    public class NerdDinners : DbContext
    {
        public NerdDinners()
        {
            Configuration.LazyLoadingEnabled = false;
        }

        public DbSet<Dinner> Dinners { get; set; }
        public DbSet<RSVP> RSVPs { get; set; }
    }
}