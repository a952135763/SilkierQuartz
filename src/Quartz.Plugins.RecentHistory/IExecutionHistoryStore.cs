using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqlSugar;

namespace Quartz.Plugins.RecentHistory
{
    [Serializable]
    [SugarTable("Logs_JobsHistory")]
    public class ExecutionHistoryEntry
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        [SugarColumn(Length = 250, IndexGroupNameList = new []{ "_FireInstanceId" } )]
        public string FireInstanceId { get; set; }

        [SugarColumn(Length = 250, IndexGroupNameList = new[] { "_SchedulerInstanceId" })]
        public string SchedulerInstanceId { get; set; }

        [SugarColumn(Length = 250, IndexGroupNameList = new[] { "_SchedulerName" })]
        public string SchedulerName { get; set; }

        public string Job { get; set; }
        public string Trigger { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? ScheduledFireTimeUtc { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? ActualFireTimeUtc { get; set; }

        public bool Recovering { get; set; }
        public bool Vetoed { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? FinishedTimeUtc { get; set; }

        [SugarColumn(IsNullable = true, ColumnDataType = "text")]
        public string ExceptionMessage { get; set; }
        public bool Cancelled { get; set; }

        [SugarColumn(ColumnDataType = "text", IsNullable = true)]
        public string DetailedLogs { get; set; }

        [SugarColumn(ColumnDataType = "text", IsNullable = true)]
        public string OutputInfo { get; set; }

        [SugarColumn(ColumnDataType = "text", IsJson = true,IsNullable = true)]
        public JobDataMap JobStartData { get; set; }

        [SugarColumn(ColumnDataType = "text", IsJson = true,IsNullable = true)]
        public JobDataMap JobEndData { get; set; }

    }

    [SugarTable("Logs_JobsCount")]
    public class HistoryCountEntry
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int? Id { get; set; }

        [SugarColumn(Length = 250, IndexGroupNameList = new[] { "_SchedulerName" })]
        public string SchedulerName { get; set; }

        public int TotalJobsExecuted { get; set; }

        public int TotalJobsFailed { get; set; }
    }

    public interface IExecutionHistoryStore
    {
        string SchedulerName { get; set; }
        ISugarQueryable<ExecutionHistoryEntry> Queryable { get; }
        Task Init(string ConnectionType,string ConnectionString);
        Task<ExecutionHistoryEntry> Get(int id);
        Task<ExecutionHistoryEntry> Get(string fireInstanceId);
        Task Save(ExecutionHistoryEntry entry);
        Task Purge();

        Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryJob(int limitPerJob);
        Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryTrigger(int limitPerTrigger);
        Task<IEnumerable<ExecutionHistoryEntry>> FilterLast(int limit);
        Task<KeyValuePair<int, IEnumerable<ExecutionHistoryEntry>>> FilterAll(int pageIndex, int pageSize);

        Task<int> GetTotalJobsExecuted();
        Task<int> GetTotalJobsFailed();

        Task IncrementTotalJobsExecuted();
        Task IncrementTotalJobsFailed();

    }


    public interface IExecutionHistoryResult
    {
        //运行详细日志
        string OutLog { get; set; }

        //运行详细结果
        string Output { get; set; }
    }

    public class ExecutionHistoryResult: IExecutionHistoryResult
    {
        //运行详细日志
       public string OutLog { get; set; }

        //运行详细结果
        public string Output { get; set; }

        public ExecutionHistoryResult(){}
        public ExecutionHistoryResult(string log, string output)
        {
            OutLog = log;
            Output = output;
        }
    }
}
