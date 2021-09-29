using Quartz;
using Quartz.Impl.Matchers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SilkierQuartz.Configuration;

namespace SilkierQuartz
{
    internal class Cache
    {
        private readonly Services _services;
        public Cache(Services services)
        {
            _services = services;
        }

        private KeyValuePair<string, string>[] _jobTypes;
        public KeyValuePair<string,string>[] JobTypes
        {
            get
            {
                if (_jobTypes == null)
                {
                    lock (this)
                    {
                        if (_jobTypes == null)
                        {
                            var types = JobsListHelper.GetSilkierQuartzJobs();



                            var keys = _services.Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()).GetAwaiter().GetResult();
                            var knownTypes = new List<KeyValuePair<string, string>>();


                            foreach (var t in types)
                            {
                                var so = t.GetCustomAttribute<SilkierQuartzAttribute>();
                                knownTypes.Add(new KeyValuePair<string, string>( t.RemoveAssemblyDetails(), so.Identity));
                            }



                            UpdateJobTypes(knownTypes);
                        }
                    }
                }
                return _jobTypes;
            }
        }

        public void UpdateJobTypes(List<KeyValuePair<string, string>> list)
        {
            if (_jobTypes != null)
                list = (List<KeyValuePair<string, string>>)list.Concat(_jobTypes); // append existing types
            _jobTypes = list.Distinct().OrderBy(x => x.Key).ToArray();
        }

    }
}
