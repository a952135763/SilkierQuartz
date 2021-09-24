using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;

namespace Jobs
{
    public abstract class AbstractJob : IJob
    {


        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await Run(context);
            }
            catch (OperationCanceledException)
            {
                //此错误直接取消
            }
            catch (JobExecutionException)
            {
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new JobExecutionException(e, true);
            }
        }

        protected abstract Task Run(IJobExecutionContext context);
    }



}
