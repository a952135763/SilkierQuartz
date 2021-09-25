using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Nito.AsyncEx;
using SqlSugar;

namespace Quartz.Plugins.RecentHistory
{
    public class LastingExecutionHistoryStore : IExecutionHistoryStore
    {


        public string SchedulerName { get; set; }

        private SqlSugarScope sqlClient;

        private AsyncLock asyncLock = new AsyncLock();

        public async Task Init(string ConnectionType, string ConnectionString)
        {
            using var t = await asyncLock.LockAsync();
            if (sqlClient == null)
            {
                if (!Enum.TryParse(ConnectionType, true, out DbType dbType))
                {
                    throw new Exception("不支持这种类型");
                }
                ICacheService myCache = new SugarCache();
                sqlClient = new SqlSugarScope(new ConnectionConfig()
                {
                    ConnectionString = ConnectionString,
                    DbType = dbType,
                    IsAutoCloseConnection = true,
                    ConfigureExternalServices = new ConfigureExternalServices()
                    {
                        DataInfoCacheService = myCache
                    }
                }, (db) =>
                {
                    db.Aop.OnLogExecuting = (sql, pars) =>
                    {

                    };
                });
                sqlClient.CodeFirst.SetStringDefaultLength(250).InitTables<ExecutionHistoryEntry>();
                sqlClient.CodeFirst.SetStringDefaultLength(250).InitTables<HistoryCountEntry>();
                var h = await sqlClient.Queryable<HistoryCountEntry>()
                    .Where(p => p.SchedulerName == SchedulerName)
                    .FirstAsync();
                if (h is null)
                {
                    await sqlClient.Insertable(new HistoryCountEntry { SchedulerName = SchedulerName, TotalJobsExecuted = 0, TotalJobsFailed = 0 })
                        .ExecuteReturnIdentityAsync();

                }

            }



        }

        public Task<ExecutionHistoryEntry> Get(string fireInstanceId)
        {

            return sqlClient.Queryable<ExecutionHistoryEntry>().Where(p => p.FireInstanceId == fireInstanceId).FirstAsync();
        }

        public Task<ExecutionHistoryEntry> Get(int id)
        {

            return sqlClient.Queryable<ExecutionHistoryEntry>().Where(p => p.Id == id).FirstAsync();
        }

        public Task Save(ExecutionHistoryEntry entry)
        {
            var x = sqlClient.Storageable(entry).ToStorage();
            try
            {
                x.AsInsertable.ExecuteCommand(); //执行插入
                x.AsUpdateable.ExecuteCommand(); //执行更新　

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
          
            return Task.CompletedTask;
        }

        public Task Purge()
        {

            return Task.CompletedTask;
        }

        public async Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryJob(int limitPerJob)
        {
            return (await sqlClient.Queryable<ExecutionHistoryEntry>()
                    .Where(p => p.SchedulerName == SchedulerName)
                    .ToArrayAsync())
                 .GroupBy(x => x.Job)
                 .Select(x => x
                     .OrderByDescending(y => y.ActualFireTimeUtc)
                     .Take(limitPerJob)
                     .Reverse())
                 .SelectMany(x => x)
                 .ToArray();
        }

        public async Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryTrigger(int limitPerTrigger)
        {
            return (await sqlClient.Queryable<ExecutionHistoryEntry>()
                    .Where(p => p.SchedulerName == SchedulerName)
                    .ToArrayAsync())
                .GroupBy(x => x.Trigger)
                .Select(x => x.OrderByDescending(y => y.ActualFireTimeUtc).Take(limitPerTrigger).Reverse())
                .SelectMany(x => x).ToArray();
        }

        public async Task<IEnumerable<ExecutionHistoryEntry>> FilterLast(int limit)
        {
            return await sqlClient.Queryable<ExecutionHistoryEntry>()
                .Where(x => x.SchedulerName == SchedulerName)
                .OrderBy(y => y.ActualFireTimeUtc).
                Take(limit).
                ToArrayAsync();
        }

        public async Task<KeyValuePair<int, IEnumerable<ExecutionHistoryEntry>>> FilterAll(int pageIndex, int pageSize)
        {
            RefAsync<int> total = 0;
            var list = await sqlClient.Queryable<ExecutionHistoryEntry>()
                .Where(x => x.SchedulerName == SchedulerName)
                .OrderBy(y => y.ActualFireTimeUtc)
                .ToPageListAsync(pageIndex, pageSize, total);
           return new KeyValuePair<int, IEnumerable<ExecutionHistoryEntry>>(total.Value, list);
        }


        public async Task<int> GetTotalJobsExecuted()
        {

           return await sqlClient.Queryable<HistoryCountEntry>()
                .Where(p => p.SchedulerName == SchedulerName)
                .Select(p=>p.TotalJobsExecuted)
                .FirstAsync();
        }

        public async Task<int> GetTotalJobsFailed()
        {
            return await sqlClient.Queryable<HistoryCountEntry>()
                .Where(p => p.SchedulerName == SchedulerName)
                .Select(p => p.TotalJobsFailed)
                .FirstAsync();
        }

        public async Task IncrementTotalJobsExecuted()
        {
            await sqlClient.Updateable<HistoryCountEntry>()
                .SetColumns(p => p.TotalJobsExecuted == p.TotalJobsExecuted + 1)
                .Where(p => p.SchedulerName == SchedulerName)
                .ExecuteCommandAsync();
        }

        public async Task IncrementTotalJobsFailed()
        {
            await sqlClient.Updateable<HistoryCountEntry>()
                .SetColumns(p => p.TotalJobsFailed == p.TotalJobsFailed + 1)
                .Where(p => p.SchedulerName == SchedulerName)
                .ExecuteCommandAsync();
        }
    }







    public class SugarCache : ICacheService
    {
        MemoryCacheHelper cache = new MemoryCacheHelper();
        public void Add<V>(string key, V value)
        {
            cache.Set(key, value);
        }

        public void Add<V>(string key, V value, int cacheDurationInSeconds)
        {
            cache.Set(key, value, cacheDurationInSeconds);
        }

        public bool ContainsKey<V>(string key)
        {
            return cache.Exists(key);
        }

        public V Get<V>(string key)
        {
            return cache.Get<V>(key);
        }

        public IEnumerable<string> GetAllKey<V>()
        {
            return cache.GetCacheKeys();
        }

        public V GetOrCreate<V>(string cacheKey, Func<V> create, int cacheDurationInSeconds = int.MaxValue)
        {
            if (cache.Exists(cacheKey))
            {
                return cache.Get<V>(cacheKey);
            }
            else
            {
                var result = create();
                cache.Set(cacheKey, result, cacheDurationInSeconds);
                return result;
            }
        }

        public void Remove<V>(string key)
        {
            cache.Remove(key);
        }
    }
    public class MemoryCacheHelper
    {
        private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        /// <summary>
        /// 验证缓存项是否存在
        /// </summary>
        /// <param name="key">缓存Key</param>
        /// <returns></returns>
        public bool Exists(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return Cache.TryGetValue(key, out _);
        }

        /// <summary>
        /// 添加缓存
        /// </summary>
        /// <param name="key">缓存Key</param>
        /// <param name="value">缓存Value</param>
        /// <param name="expiresSliding">滑动过期时长（如果在过期时间内有操作，则以当前时间点延长过期时间）</param>
        /// <param name="expiressAbsoulte">绝对过期时长</param>
        /// <returns></returns>
        public bool Set(string key, object value, TimeSpan expiresSliding, TimeSpan expiressAbsoulte)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Cache.Set(key, value,
                new MemoryCacheEntryOptions().SetSlidingExpiration(expiresSliding)
                    .SetAbsoluteExpiration(expiressAbsoulte));
            return Exists(key);
        }

        /// <summary>
        /// 添加缓存
        /// </summary>
        /// <param name="key">缓存Key</param>
        /// <param name="value">缓存Value</param>
        /// <param name="expiresIn">缓存时长</param>
        /// <param name="isSliding">是否滑动过期（如果在过期时间内有操作，则以当前时间点延长过期时间）</param>
        /// <returns></returns>
        public bool Set(string key, object value, TimeSpan expiresIn, bool isSliding = false)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Cache.Set(key, value,
                isSliding
                    ? new MemoryCacheEntryOptions().SetSlidingExpiration(expiresIn)
                    : new MemoryCacheEntryOptions().SetAbsoluteExpiration(expiresIn));

            return Exists(key);
        }

        /// <summary>
        /// 添加缓存
        /// </summary>
        /// <param name="key">缓存Key</param>
        /// <param name="value">缓存Value</param>
        /// <returns></returns>
        public void Set(string key, object value)
        {
            Set(key, value, TimeSpan.FromDays(1));
        }

        /// <summary>
        /// 添加缓存
        /// </summary>
        /// <param name="key">缓存Key</param>
        /// <param name="value">缓存Value</param>
        /// <param name="ts"></param>
        /// <returns></returns>
        public void Set(string key, object value, TimeSpan ts)
        {
            Set(key, value, ts, false);
        }

        /// <summary>
        /// 添加缓存
        /// </summary>
        /// <param name="key">缓存Key</param>
        /// <param name="value">缓存Value</param>
        /// <param name="ts"></param>
        /// <returns></returns>
        public void Set(string key, object value, int seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            Set(key, value, ts, false);
        }
        #region 删除缓存

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="key">缓存Key</param>
        /// <returns></returns>
        public void Remove(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            Cache.Remove(key);
        }

        /// <summary>
        /// 批量删除缓存
        /// </summary>
        /// <returns></returns>
        public void RemoveAll(IEnumerable<string> keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            keys.ToList().ForEach(item => Cache.Remove(item));
        }
        #endregion

        #region 获取缓存

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key">缓存Key</param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return Cache.Get<T>(key);
        }

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key">缓存Key</param>
        /// <returns></returns>
        public object Get(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return Cache.Get(key);
        }

        /// <summary>
        /// 获取缓存集合
        /// </summary>
        /// <param name="keys">缓存Key集合</param>
        /// <returns></returns>
        public IDictionary<string, object> GetAll(IEnumerable<string> keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            var dict = new Dictionary<string, object>();
            keys.ToList().ForEach(item => dict.Add(item, Cache.Get(item)));
            return dict;
        }
        #endregion

        /// <summary>
        /// 删除所有缓存
        /// </summary>
        public void RemoveCacheAll()
        {
            var l = GetCacheKeys();
            foreach (var s in l)
            {
                Remove(s);
            }
        }

        /// <summary>
        /// 删除匹配到的缓存
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public void RemoveCacheRegex(string pattern)
        {
            IList<string> l = SearchCacheRegex(pattern);
            foreach (var s in l)
            {
                Remove(s);
            }
        }

        /// <summary>
        /// 搜索 匹配到的缓存
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public IList<string> SearchCacheRegex(string pattern)
        {
            var cacheKeys = GetCacheKeys();
            var l = cacheKeys.Where(k => Regex.IsMatch(k, pattern)).ToList();
            return l.AsReadOnly();
        }

        /// <summary>
        /// 获取所有缓存键
        /// </summary>
        /// <returns></returns>
        public List<string> GetCacheKeys()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var entries = Cache.GetType().GetField("_entries", flags).GetValue(Cache);
            var cacheItems = entries as IDictionary;
            var keys = new List<string>();
            if (cacheItems == null) return keys;
            foreach (DictionaryEntry cacheItem in cacheItems)
            {
                keys.Add(cacheItem.Key.ToString());
            }
            return keys;
        }
    }
}
