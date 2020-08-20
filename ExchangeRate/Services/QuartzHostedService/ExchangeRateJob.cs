namespace ExchangeRate.Services.QuartzHostedService
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using ExchangeRate.Data;
    using ExchangeRate.Models;
    using Microsoft.EntityFrameworkCore.Internal;
    using Microsoft.Extensions.Logging;
    using Quartz;

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
                var exchangeRates = GetExchangeRates(requestResult);

                SaveExchangeRatesToDB(exchangeRates);
            }

            return Task.CompletedTask;
        }

        private List<ExchangeRateFixing> GetExchangeRates(string requestResult)
        {
            var result = new List<ExchangeRateFixing>();

            try
            {
                var names = requestResult
                    .Split("\n")[0].Split("|").Skip(1)
                    .Select((x, y) => new ExchangeRateFixing()
                    {
                        Id = y,
                        Name = x.Split(" ")[1],
                        Nominal = int.Parse(x.Split(" ")[0]),
                    });

                var rows = requestResult.Split("\n").Skip(1).TakeWhile(x => x != "").ToArray();

                for (int i = 0; i < rows.Length; i++)
                {
                    var date = DateTime.Parse(rows[i].Split("|")[0]).Date;

                    var data = rows[i].Split("|").Skip(1)
                        .Select((x, y) => new ExchangeRateFixing
                        {
                            Id = y,
                            Date = date,
                            Rate = GetDecimalValue(NumberStyles.AllowDecimalPoint, CultureInfo.CreateSpecificCulture("en-EN"), x),
                        });

                    result.AddRange(names.Join(
                        data,
                        names => names.Id,
                        data => data.Id,
                        (names, data) => new ExchangeRateFixing()
                        {
                            Name = names.Name,
                            Nominal = names.Nominal,
                            Date = data.Date,
                            Rate = data.Rate,
                        }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Произошла ошибка при получении данных сервиса банка.", ex.Message);
            }

            return result;
        }

        private decimal GetDecimalValue(NumberStyles style, CultureInfo culture, string x)
        {
            decimal.TryParse(x, style, culture, out decimal val);
            return val;
        }

        private void SaveExchangeRatesToDB(List<ExchangeRateFixing> exchangeRates)
        {
            foreach (var item in exchangeRates)
            {
                try
                {
                    var currentRate = _exchangeRateContext.ExchangeRates
                        .Where(x => x.Name == item.Name &&
                            x.Date.Date == item.Date.Date)
                        .FirstOrDefault();

                    if (currentRate == null)
                    {
                        _exchangeRateContext.Add(item);
                    }
                    else
                    {
                        var rate = Math.Round(item.Rate, 2, MidpointRounding.AwayFromZero);

                        if (currentRate.Rate != rate)
                        {
                            currentRate.Rate = rate;
                            _exchangeRateContext.Update(currentRate);
                        }
                    }

                    _exchangeRateContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Произошла ошибка при сохранении данных в БД.", ex.Message);
                }
            }
        }
    }
}
