using System;

namespace ExchangeRate.Models
{
    public class ExchangeRateFixing
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Nominal { get; set; }

        public DateTime Date { get; set; }

        public decimal Rate { get; set; }
    }
}
