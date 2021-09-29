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
                authenticationOptions.SilkierQuartzClaim = Dns.GetHostName();
                authenticationOptions.SilkierQuartzClaimValue = "12222";
                authenticationOptions.UserName = "admin";
                authenticationOptions.UserPassword = "123456";
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

                //���ݿ�������Ϣ
                properties["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX,Quartz";
                properties["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.MySQLDelegate,Quartz";
                properties["quartz.jobStore.dataSource"] = "myDS";

                //��ǰ׺
                properties["quartz.jobStore.tablePrefix"] = "QRTZ_";
                //�����ַ���
                properties["quartz.dataSource.myDS.connectionString"] = "Server=47.107.180.26;Port=3333;Database=jobstest;Uid=jobstest;Pwd=pNm567m2Td7pC3ZA;SslMode=None";
                properties["quartz.dataSource.myDS.provider"] = "MySql";

                //json���л�
                properties["quartz.serializer.type"] = "json";
                properties["quartz.jobStore.useProperties"] = "true";

                //Ⱥ����Ϣ
                properties["quartz.scheduler.instanceName"] = "GameJobs";
                //�ڵ�����,������Ⱥ����Ψһ����ֹͬһ̨���Կ�����������
                properties["quartz.scheduler.instanceId"] = $"{Dns.GetHostName()}";
                properties["quartz.jobStore.clustered"] = "true";
                properties["quartz.jobStore.clusterCheckinInterval"] = "2000";


                //��������߳�����
                properties["quartz.threadPool.maxConcurrency"] = "1";



                //��ҵ�����־���,�������,Ⱥ�������ϱ�
                properties["quartz.plugin.recentHistory.type"] = "Quartz.Plugins.RecentHistory.ExecutionHistoryPlugin,Quartz.Plugins.RecentHistory";
                properties["quartz.plugin.recentHistory.storeType"] = "Quartz.Plugins.RecentHistory.LastingExecutionHistoryStore,Quartz.Plugins.RecentHistory";
                properties["quartz.plugin.recentHistory.connectionType"] = "Mysql";
                properties["quartz.plugin.recentHistory.connectionString"] = "Server=47.107.180.26;Port=3333;Database=jobstest;Uid=jobstest;Pwd=pNm567m2Td7pC3ZA;SslMode=None";
                properties["quartz.plugin.recentHistory.theServer"] = "";
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
