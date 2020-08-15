using ExchangeRate.Data;
using ExchangeRate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ExchangeRate.Services.QuartzHostedService
{
    public class ExchangeRateJob : IJob
    {
        private readonly ILogger<ExchangeRateJob> _logger;

        private readonly ExchangeRateContext _exchangeRateContext;

        public ExchangeRateJob(ExchangeRateContext exchangeRateContext, ILogger<ExchangeRateJob> logger)
        {
            _logger = logger;
            _exchangeRateContext = exchangeRateContext;
        }

        public Task Execute(IJobExecutionContext context)
        {
            var httpClient = HttpClientFactory.Create();

            string url = "https://www.cnb.cz/" +
                "en/" +
                "financial_markets/" +
                "foreign_exchange_market/" +
                "exchange_rate_fixing/" +
                "year.txt?" +
                $"year={DateTime.Now.Year}";

            var requestResult = httpClient.GetStringAsync(url).Result;

            if (!string.IsNullOrEmpty(requestResult))
            {
                var result = new List<ExchangeRateFixing>();

                var names = requestResult
                    .Split("\n")[0].Split("|").Skip(1)
                    .Select((x, y) => new ExchangeRateFixing() 
                    {
                        Id = y,
                        Name = x.Split(" ")[1],
                        Nominal = int.Parse(x.Split(" ")[0])
                    });

                var rows = requestResult.Split("\n").Skip(1).ToArray();

                for (int i = 0; i < rows.Length; i++)
                {
                    var date = DateTime.Parse(rows[i].Split("|")[0]);

                    var data = rows[i].Split("|").Skip(1)
                        .Select((x, y) => new ExchangeRateFixing
                        {
                            Id = y,
                            Date = date,
                            Rate = GetDecimalValue(NumberStyles.AllowDecimalPoint, CultureInfo.CreateSpecificCulture("en-EN"), x)
                        });

                    result.AddRange(names.Join(data,
                        names => names.Id,
                        data => data.Id,
                        (names, data) => new ExchangeRateFixing()
                        {
                            Id = i * 100 + names.Id,
                            Name = names.Name,
                            Nominal = names.Nominal,
                            Date = data.Date,
                            Rate = data.Rate
                        }));
                }

                foreach (var item in result)
                {
                    if (_exchangeRateContext.ExchangeRates.Select(x => x.Id).Contains(item.Id))
                    {

                    }
                    else
                    {

                    }
                }
            }

            return Task.CompletedTask;
        }

        private decimal GetDecimalValue(NumberStyles style, CultureInfo culture, string x)
        {
            decimal.TryParse(x, style, culture, out decimal val);
            return val;
        }
    }
}
