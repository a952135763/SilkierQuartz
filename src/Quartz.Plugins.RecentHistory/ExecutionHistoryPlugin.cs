using Quartz.Impl.Matchers;
using Quartz.Spi;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SqlSugar;

namespace Quartz.Plugins.RecentHistory
{
    public class ExecutionHistoryPlugin : ISchedulerPlugin, IJobListener
    {
        IScheduler _scheduler;
        IExecutionHistoryStore _store;

        public string Name { get; set; }
        public Type StoreType { get; set; }
        public string ConnectionString { get; set; }
        public string ConnectionType { get; set; }
        public string TheServer { get; set; }

        public Task Initialize(string pluginName, IScheduler scheduler, CancellationToken cancellationToken = default(CancellationToken))
        {
            Name = pluginName;
            _scheduler = scheduler;
            _scheduler.ListenerManager.AddJobListener(this, GroupMatcher<JobKey>.AnyGroup());
            //

            return Task.FromResult(0);
        }

        public Task Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            _store = _scheduler.Context.GetExecutionHistoryStore();

            if (_store == null)
            {
                if (StoreType != null)
                    _store = (IExecutionHistoryStore)Activator.CreateInstance(StoreType);

                if (_store == null)
                    throw new Exception(nameof(StoreType) + " is not set.");
                _scheduler.Context.SetExecutionHistoryStore(_store);
            }

            _store.SchedulerName = _scheduler.SchedulerName;
            _store.Init(ConnectionType, ConnectionString);

            return _store.Purge();
        }

        public Task Shutdown(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            //任务执行前
            context.Put("outStr", new StringBuilder());
            context.Put("outLogStr", new StringBuilder());
            var entry = new ExecutionHistoryEntry()
            {
                FireInstanceId = context.FireInstanceId,
                SchedulerInstanceId = context.Scheduler.SchedulerInstanceId,
                SchedulerName = context.Scheduler.SchedulerName,
                ActualFireTimeUtc = context.FireTimeUtc.UtcDateTime,
                ScheduledFireTimeUtc = context.ScheduledFireTimeUtc?.UtcDateTime,
                Recovering = context.Recovering,
                Job = context.JobDetail.Key.ToString(),
                Trigger = context.Trigger.Key.ToString(),
                JobStartData = context.MergedJobDataMap

            };
            if (context.Recovering)
            {
                var lastEntry = _store.Queryable
                     .Where(p => p.Trigger == context.RecoveringTriggerKey.ToString())
                     .OrderBy(p=>p.ScheduledFireTimeUtc,OrderByType.Desc)
                     .First();
                if (lastEntry != null)
                {
                    lastEntry.FinishedTimeUtc= DateTime.UtcNow;
                    lastEntry.ExceptionMessage = "任务执行时调度实例被非法关闭";
                    _store.Save(lastEntry);
                }
            }

            return _store.Save(entry);
        }

        /// <summary>
        /// 执行等待当前的任务!
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task NextJob(IJobExecutionContext context)
        {
            if (context.Result is IExecutionHistoryResult rest)
            {
                //执行完成前!
                //获取当前组任务key
                var keys = await context.Scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(context.JobDetail.Key.Group));
                //获取任务详情
                var nextJon = keys.Where(p => !Equals(p, context.JobDetail.Key))
                    .Select(p => context.Scheduler.GetJobDetail(p).Result)
                    .ToArray();
                foreach (IJobDetail detail in nextJon)
                {
                    var data = detail.JobDataMap;
                    string targetJobs = data.GetString("_监听启动");
                    if (!string.IsNullOrEmpty(targetJobs))
                    {
                        string[] targetSplit = targetJobs.Split("\r\n");

                        string targetDen = targetSplit.FirstOrDefault(p => p.Equals(context.JobDetail.Key.Name));
                        if (!string.IsNullOrEmpty(targetDen))
                        {
                            data["_输入信息"] = rest.Output;
                            data.Remove("_监听启动");
                            try
                            {
                                await context.Scheduler.TriggerJob(detail.Key, data);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }

                        }

                    }

                }

            }

        }

        public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default(CancellationToken))
        {

            //我无法取消此任务,从管理端取消的任务不触发监听
            //发生错误的任务,不触发监听
            if (!context.CancellationToken.IsCancellationRequested && jobException == null)
            {
                try
                {

                    await NextJob(context);

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            var entry = await _store.Get(context.FireInstanceId);
            
            if (entry != null)
            {
                entry.Cancelled = context.CancellationToken.IsCancellationRequested;
                entry.FinishedTimeUtc = DateTime.UtcNow;
                entry.ExceptionMessage = jobException?.GetBaseException()?.Message;

                StringBuilder outStr = (StringBuilder)context.Get("outStr");
                StringBuilder outLogStr = (StringBuilder)context.Get("outLogStr");

                entry.DetailedLogs = outStr?.ToString();
                entry.OutputInfo = outLogStr?.ToString();
                entry.JobEndData = context.MergedJobDataMap;
                await _store.Save(entry);
            }
            if (jobException == null)
                await _store.IncrementTotalJobsExecuted();
            else
                await _store.IncrementTotalJobsFailed();

        }

        public async Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var entry = await _store.Get(context.FireInstanceId);
            if (entry != null)
            {
                entry.Vetoed = true;
                await _store.Save(entry);
            }
        }
    }
}
