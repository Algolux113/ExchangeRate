using ExchangeRate.Models;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRate.Data
{
    public class ExchangeRateContext : DbContext
    {
        public ExchangeRateContext(DbContextOptions<ExchangeRateContext> options) : base(options)
        {
        }

        public DbSet<ExchangeRateFixing> ExchangeRates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExchangeRateFixing>().ToTable("ExchangeRate");
        }
    }
}
