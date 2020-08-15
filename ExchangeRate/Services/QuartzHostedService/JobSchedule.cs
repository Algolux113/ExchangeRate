using System;

namespace ExchangeRate.Services.QuartzHostedService
{
    public class JobSchedule
    {
        public Type JobType { get; }

        public string CronExpression { get; }

        public JobSchedule(Type jobType, string cronExpression)
        {
            JobType = jobType;
            CronExpression = cronExpression;
        }
    }
}
