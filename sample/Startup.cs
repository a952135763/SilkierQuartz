using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using System.Collections.Generic;
using System.Net;

namespace SilkierQuartz.Example
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddSilkierQuartz(options =>
            {
                options.VirtualPathRoot = "/quartz";
                options.UseLocalTime = true;
                options.DefaultDateFormat = "yyyy-MM-dd";
                options.DefaultTimeFormat = "HH:mm:ss";
                options.CronExpressionOptions = new CronExpressionDescriptor.Options()
                {
                    DayOfWeekStartIndexZero = false,//Quartz uses 1-7 as the range
                    Use24HourTimeFormat = true
                };
               
            }
#if ENABLE_AUTH
            ,
            authenticationOptions =>
            {
                authenticationOptions.AuthScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authenticationOptions.SilkierQuartzClaim = "Silkier";
                authenticationOptions.SilkierQuartzClaimValue = "Quartz";
                authenticationOptions.UserName = "admin";
                authenticationOptions.UserPassword = "password";
                authenticationOptions.AccessRequirement = SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyUsersWithClaim;
            }
#else 
    ,
            authenticationOptions =>
            {
                authenticationOptions.AccessRequirement = SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous;
            }
#endif
            ,
            stdSchedulerFactoryOptions: properties =>
            {

                //数据库连接信息
                properties["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX,Quartz";
                properties["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.MySQLDelegate,Quartz";
                properties["quartz.jobStore.dataSource"] = "myDS";

                //表前缀
                properties["quartz.jobStore.tablePrefix"] = "QRTZ_";
                //连接字符串
                properties["quartz.dataSource.myDS.connectionString"] = "Server=localhost;Database=qze;Uid=root;Pwd=H9MvYSqY3JmAC4aj;SslMode=None";
                properties["quartz.dataSource.myDS.provider"] = "MySql";

                //json序列化
                properties["quartz.serializer.type"] = "json";
                properties["quartz.jobStore.useProperties"] = "true";

                //群集信息
                properties["quartz.scheduler.instanceName"] = "GameGo";
                properties["quartz.scheduler.instanceId"] = "AUTO";
                properties["quartz.jobStore.clustered"] = "true";
                properties["quartz.jobStore.clusterCheckinInterval"] = "1000";


                //最大线程数量
                properties["quartz.threadPool.maxConcurrency"] = "1";

                //任务监控插件
                properties["quartz.plugin.recentHistory.type"] = "Quartz.Plugins.RecentHistory.ExecutionHistoryPlugin,Quartz.Plugins.RecentHistory";
                properties["quartz.plugin.recentHistory.storeType"] = "Quartz.Plugins.RecentHistory.Impl.InProcExecutionHistoryStore, Quartz.Plugins.RecentHistory";
            } 
            );
            services.AddOptions();
            services.Configure<AppSettings>(Configuration);
            services.Configure<InjectProperty>(options => { options.WriteText = "This is inject string"; });
            services.AddQuartzJob<Jobs.GameJob>();
            services.AddQuartzJob<Jobs.TestJob>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSilkierQuartz();
        }
    }
}
