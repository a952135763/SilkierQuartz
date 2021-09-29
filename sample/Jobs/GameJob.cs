using Quartz;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using H.Pipes;
using H.Pipes.Args;
using MessageStructure;
using Quartz.Plugins.RecentHistory;
using SilkierQuartz.Example.GameProgram;

namespace Jobs
{

    /// <summary>
    /// IJobExecutionContext.Result  禁止使用!
    /// </summary>
    public class GameJob : AbstractJob
    {

        protected override async Task<IExecutionHistoryResult> Run(IJobExecutionContext context)
        {
            JobKey jobKey = context.JobDetail.Key;
            JobDataMap data = context.MergedJobDataMap;

            
            var a = context.JobDetail.Durable;//即时运行,还是计划运行
            var s = context.JobDetail.RequestsRecovery; //是不是重试
            Console.WriteLine($"执行任务:{jobKey.Group}.{jobKey.Name}");
            var gameUrl = data.GetString("_GameUrl");
            var pipName = $"./{context.FireInstanceId}/{jobKey}";

            if (string.IsNullOrEmpty(gameUrl))
            {
                Console.WriteLine($"执行失败...必须需要参数_GameName");
                throw new JobExecutionException($"必须需要参数_GameName");
            }
            //自动todo::获取文件下发
            var get = new AutoGet($"{Environment.CurrentDirectory}/GameTmp");
            get.RunUrl(gameUrl,new string[]{ pipName },out var process);
            if (process == null)
            {
                throw new JobExecutionException($"执行失败...对应脚本拉取失败");
            }
            Channel<JobInfo> aevent = Channel.CreateUnbounded<JobInfo>();


            return await Collection(aevent, process, pipName, context);

        }


        private async Task<IExecutionHistoryResult> Collection(Channel<JobInfo> endCh,Process process, string pipName, IJobExecutionContext context)
        {
            JobDataMap data = context.MergedJobDataMap;
            var token = (CancellationToken)context.Get("token");

            StringBuilder outStr = new StringBuilder();
            StringBuilder outLogStr = new StringBuilder();

            var targetCount = data.GetIntValue("_OutputCount");

            //启动进程通讯,out
            await using var outServer = new PipeServer<PipeMessage>(pipName);
            outServer.Formatter = new H.Formatters.JsonFormatter();
            outServer.ClientConnected += async (o, args) =>
            {
                //连接成功 下发配置文件!
                await args.Connection.WriteAsync(new PipeMessage()
                {
                    Id = 0,
                    Configure = data.GetString("_Configure"),
                });
                await args.Connection.WriteAsync(new PipeMessage()
                {
                    Id = -1,
                    Input = $"{context.JobDetail.Key}-{context.FireTimeUtc.ToLocalTime()}",
                });
            };
            outServer.MessageReceived += async (sender, args) =>
            {
               await OnEventHandler(sender, args, context, outStr, targetCount, outLogStr, endCh);
            } ;
            outServer.ExceptionOccurred += (o, args) => Console.WriteLine(args.Exception);
            _ = outServer.StartAsync(token);



            process.Start();

            token.Register(async () =>
            {
                //任务被取消
                await outServer.WriteAsync(new PipeMessage() { Id = 3 });
                await endCh.Writer.WriteAsync(new JobInfo() { warning = "任务被取消" });
                TryStopProcess(process);
                process = null;
                Console.WriteLine("任务被远程取消");
            });

            var source = CancellationTokenSource.CreateLinkedTokenSource(token);
            //等待进程退出,不正常退出都提示!
            _ = Task.Run(async () =>
            {
                await process?.WaitForExitAsync(source.Token);
                await endCh.Writer.WriteAsync(new JobInfo() { warning = "进程被意外关闭!可能数据丢失" }, source.Token);
            }, source.Token);



            var info = await endCh.Reader.ReadAsync(token);//等待触发消息通知
            source.Cancel(true);//取消进程等待
            await outServer.StopAsync();//有消息了!任务不是出错就是完成了,直接关闭通讯
            TryStopProcess(process);//尝试关闭进程
            process = null;

            //任务完成,查看状态!
            if (!string.IsNullOrEmpty(info.error))
            {
                //任务没有正常完成,抛出错误!是没法记录日志和输出数据的 todo::运行中输出数据和日志实时保存
                throw new JobExecutionException(info.error);
            }
            //任务正常完成
            if (!string.IsNullOrEmpty(info.warning))
            {
                outLogStr.AppendLine($"警告信息:{info.warning}");
            }
            return new ExecutionHistoryResult(){ OutLog=outLogStr.ToString(), Output= outStr.ToString() };
        }

        private bool StartProcess(string exePath, string arg, out Process p)
        {
            var difr = Path.GetDirectoryName(exePath);
            p = new Process();
            p.StartInfo.WorkingDirectory = difr;
            p.StartInfo.FileName = exePath;
            p.StartInfo.Arguments = arg;
            return p.Start();
        }

        private void TryStopProcess(Process p)
        {
            if (p is { })
            {
                try
                {
                    p.Kill();
                }
                catch (Exception)
                {
                }

            }
        }

        private async Task OnEventHandler(object sender, 
            ConnectionMessageEventArgs<PipeMessage?> args, 
            IJobExecutionContext context, 
            StringBuilder outBuilder,
            int targetCount,
            StringBuilder outLogBuilder,
            Channel<JobInfo> endCh)
        {
            JobDataMap data = context.MergedJobDataMap;
            //获取到配置成功,发送输入数据
            if (args.Message != null)
            {
                switch (args.Message.Id)
                {
                    case 0:
                        //返回配置初步验证情况,ok继续下发输入信息
                        if (args.Message.Output.Equals("ok"))
                        {
                            await args.Connection.WriteAsync(new PipeMessage() { Id = 1, Input = data.GetString("_Input") });
                            return;
                        }
                        await endCh.Writer.WriteAsync(new JobInfo() { error = $"脚本配置失败-{args.Message.Output}" });
                        break;
                    case 1:
                        //输入信息已经验证完,发送开始运行
                        if (args.Message.Output.Equals("ok"))
                        {
                            await args.Connection.WriteAsync(new PipeMessage() { Id = 2, Input = "Start" });
                            return;
                        }
                        await endCh.Writer.WriteAsync(new JobInfo() { error = $"脚本输入信息失败-{args.Message.Output}" });

                        break;
                    case 2:
                        //开始运行返回
                        if (args.Message.Output.Equals("ok"))
                        {
                            //任务开始成功
                            return;
                        }
                        await endCh.Writer.WriteAsync(new JobInfo() { error = $"脚本开始失败-{args.Message.Output}" });
                        break;
                    case 3:
                        //任务取消返回!由客户端发起!取消了任务
                       await endCh.Writer.WriteAsync(new JobInfo(){error = $"脚本取消任务-{args.Message.Output}" });
                        break;
                    case 4:
                        //记录日志返回
                        outLogBuilder.AppendLine(args.Message.Output);
                        break;
                    case 5:
                        //返回数据单条输出
                        outBuilder.AppendLine(args.Message.Output);
                        if (Regex.Matches(outBuilder.ToString(), Environment.NewLine).Count >= targetCount)
                        {
                            await endCh.Writer.WriteAsync(new JobInfo());
                            return;
                        }
                        break;
                    case 6:
                        //任务正常返回
                        await endCh.Writer.WriteAsync(new JobInfo());
                        break;
                }
            }
        }
    }


    internal class JobInfo
    {
        public string error;
        public string warning;

    }
}
