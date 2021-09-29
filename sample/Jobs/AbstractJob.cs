using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Quartz.Plugins.RecentHistory;

namespace Jobs
{
    public abstract class AbstractJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                JobDataMap data = context.MergedJobDataMap;
                var runningTime = data.GetIntValue("_最大运行时间");
                if (runningTime > 0)
                {
                    var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
                    context.Put("token", tokenSource.Token);
                    tokenSource.CancelAfter(TimeSpan.FromSeconds(runningTime));
                }
                else
                {
                    context.Put("token", context.CancellationToken);
                }
                var res = await Run(context);
                context.Result = res;
                Console.WriteLine("任务结束");
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

        protected abstract Task<IExecutionHistoryResult> Run(IJobExecutionContext context);
    }





}
