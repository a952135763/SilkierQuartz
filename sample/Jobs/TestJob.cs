using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jobs;
using Newtonsoft.Json;
using Quartz;

namespace Jobs
{

    public class TestJob: AbstractJob
    {
        protected override async Task Run(IJobExecutionContext context)
        {
            JobDataMap data = context.MergedJobDataMap;



            var i = data.GetIntValue("i");
            Console.WriteLine(JsonConvert.SerializeObject(data));

            data["Output"] = ++i;
            return ;
        }
    }
}
