namespace ExchangeRate.Data
{
    using ExchangeRate.Models;
    using Microsoft.EntityFrameworkCore;

    public class ExchangeRateContext : DbContext
    {
        public ExchangeRateContext(DbContextOptions<ExchangeRateContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<ExchangeRateFixing> ExchangeRates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExchangeRateFixing>().ToTable("ExchangeRate");
        }
    }
}
