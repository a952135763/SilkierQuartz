using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobs;
using Newtonsoft.Json;
using Quartz;
using Quartz.Plugins.RecentHistory;

namespace Jobs
{

    public class TestJob: AbstractJob
    {
        protected override async Task<IExecutionHistoryResult> Run(IJobExecutionContext context)
        {
            JobDataMap data = context.MergedJobDataMap;


            var i = data.GetString("_Input");

            if (int.TryParse(i, out var j))
            {
                Console.WriteLine(JsonConvert.SerializeObject(data));
                Thread.Sleep(j * 1000);
                return new ExecutionHistoryResult("", $"{++j}");
            }
            else
            {

                return new ExecutionHistoryResult("", $"输入:{i}");
            }

          
        }
    }
}
