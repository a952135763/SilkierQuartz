using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz.Plugins.RecentHistory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Quartz;
using SilkierQuartz.Models;

namespace SilkierQuartz.Controllers
{
    [Authorize(Policy = SilkierQuartzAuthenticationOptions.AuthorizationPolicyName)]
    public class HistoryController : PageControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var store = Scheduler.Context.GetExecutionHistoryStore();

            ViewBag.HistoryEnabled = store != null;

            if (store == null)
                return View(null);

            IEnumerable<ExecutionHistoryEntry> history = await store.FilterLast(100);

            var list = new List<object>();

            foreach (var h in history.OrderByDescending(x => x.ActualFireTimeUtc))
            {
                string state = "完成", icon = "check";
                var endTime = h.FinishedTimeUtc;

                if (h.Vetoed)
                {
                    state = "否决";
                    icon = "ban";
                }
                else if (!string.IsNullOrEmpty(h.ExceptionMessage))
                {
                    state = "错误";
                    icon = "close";
                }
                else if (h.FinishedTimeUtc == null)
                {
                    state = "运行中";
                    icon = "play";
                    endTime = DateTime.UtcNow;
                }
                else if (h.Cancelled)
                {
                    state = "取消";
                    icon = "exchange";
                }

                var jobKey = h.Job.Split('.');
                var triggerKey = h.Trigger.Split('.');

                list.Add(new
                {
                    Entity = h,
                    JobGroup = jobKey[0],
                    JobName = h.Job.Substring(jobKey[0].Length + 1),
                    TriggerGroup = triggerKey[0],
                    TriggerName = h.Trigger.Substring(triggerKey[0].Length + 1),
                    FireInstanceId = h.FireInstanceId,
                    ScheduledFireTimeUtc = h.ScheduledFireTimeUtc?.ToDefaultFormat(),
                    ActualFireTimeUtc = h.ActualFireTimeUtc?.ToDefaultFormat(),
                    FinishedTimeUtc = h.FinishedTimeUtc?.ToDefaultFormat(),
                    Duration = (endTime - h.ActualFireTimeUtc)?.ToString("hh\\:mm\\:ss"),
                    State = state,
                    StateIcon = icon,
                });
            }

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Show(int Id)
        {
            var store = Scheduler.Context.GetExecutionHistoryStore();
            ViewBag.Activation = false;

            if (Id > 0)
            {
                var data = await store.Get(Id);
                if (data != null)
                {
                    ViewBag.Activation = true;
                    var jobDataStartMap = new JobDataMapModel() { Template = JobDataMapItemTemplate };
                    var jobDataEndMap = new JobDataMapModel() { Template = JobDataMapItemTemplate };

                    jobDataStartMap.Items.AddRange(data.JobStartData.ToJobDataMapItems(Services));

                    jobDataEndMap.Items.AddRange(data.JobEndData.ToJobDataMapItems(Services));
                    data.ActualFireTimeUtc = data.ActualFireTimeUtc?.ToLocalTime();
                    data.FinishedTimeUtc = data.FinishedTimeUtc?.ToLocalTime();
                    data.ScheduledFireTimeUtc = data.ScheduledFireTimeUtc?.ToLocalTime();

                    return View(new { Entry = data, JobDataStartMap = jobDataStartMap, JobDataEndMap = jobDataEndMap });
                }

            }
            return View(null);
        }
    }
}
