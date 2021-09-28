using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Quartz.Impl.Matchers;
using Quartz.Spi;

namespace Quartz.Impl.RedisJobStore
{
    public class RedisJobStore: IJobStore
    {
        public Task Initialize(ITypeLoadHelper loadHelper, ISchedulerSignaler signaler,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task SchedulerStarted(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task SchedulerPaused(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task SchedulerResumed(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task Shutdown(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task StoreJobAndTrigger(IJobDetail newJob, IOperableTrigger newTrigger,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsJobGroupPaused(string groupName, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsTriggerGroupPaused(string groupName, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task StoreJob(IJobDetail newJob, bool replaceExisting, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task StoreJobsAndTriggers(IReadOnlyDictionary<IJobDetail, IReadOnlyCollection<ITrigger>> triggersAndJobs, bool replace,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveJob(JobKey jobKey, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveJobs(IReadOnlyCollection<JobKey> jobKeys, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IJobDetail> RetrieveJob(JobKey jobKey, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task StoreTrigger(IOperableTrigger newTrigger, bool replaceExisting,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveTrigger(TriggerKey triggerKey, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveTriggers(IReadOnlyCollection<TriggerKey> triggerKeys, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<bool> ReplaceTrigger(TriggerKey triggerKey, IOperableTrigger newTrigger,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IOperableTrigger> RetrieveTrigger(TriggerKey triggerKey, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<bool> CalendarExists(string calName, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<bool> CheckExists(JobKey jobKey, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<bool> CheckExists(TriggerKey triggerKey, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task ClearAllSchedulingData(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task StoreCalendar(string name, ICalendar calendar, bool replaceExisting, bool updateTriggers,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveCalendar(string calName, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<ICalendar> RetrieveCalendar(string calName, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<int> GetNumberOfJobs(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<int> GetNumberOfTriggers(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<int> GetNumberOfCalendars(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<JobKey>> GetJobKeys(GroupMatcher<JobKey> matcher, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<TriggerKey>> GetTriggerKeys(GroupMatcher<TriggerKey> matcher, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<string>> GetJobGroupNames(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<string>> GetTriggerGroupNames(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<string>> GetCalendarNames(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IOperableTrigger>> GetTriggersForJob(JobKey jobKey, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<TriggerState> GetTriggerState(TriggerKey triggerKey, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task PauseTrigger(TriggerKey triggerKey, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<string>> PauseTriggers(GroupMatcher<TriggerKey> matcher, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task PauseJob(JobKey jobKey, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<string>> PauseJobs(GroupMatcher<JobKey> matcher, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task ResumeTrigger(TriggerKey triggerKey, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<string>> ResumeTriggers(GroupMatcher<TriggerKey> matcher, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<string>> GetPausedTriggerGroups(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task ResumeJob(JobKey jobKey, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<string>> ResumeJobs(GroupMatcher<JobKey> matcher, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task PauseAll(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task ResumeAll(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IOperableTrigger>> AcquireNextTriggers(DateTimeOffset noLaterThan, int maxCount, TimeSpan timeWindow,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task ReleaseAcquiredTrigger(IOperableTrigger trigger, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<TriggerFiredResult>> TriggersFired(IReadOnlyCollection<IOperableTrigger> triggers, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task TriggeredJobComplete(IOperableTrigger trigger, IJobDetail jobDetail, SchedulerInstruction triggerInstCode,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public bool SupportsPersistence { get; }
        public long EstimatedTimeToReleaseAndAcquireTrigger { get; }
        public bool Clustered { get; }
        public string InstanceId { get; set; }
        public string InstanceName { get; set; }
        public int ThreadPoolSize { get; set; }
    }
}
