using Quartz;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using H.Pipes;
using H.Pipes.Args;
using MessageStructure;

namespace Jobs
{


    public class GameJob : AbstractJob
    {

        protected override async Task Run(IJobExecutionContext context)
        {
            var token = context.CancellationToken;
            JobKey jobKey = context.JobDetail.Key;
            JobDataMap data = context.MergedJobDataMap;


            var a = context.JobDetail.Durable;//即时运行,还是计划运行
            var s = context.JobDetail.RequestsRecovery; //是不是重试
            Console.WriteLine($"执行任务:{jobKey.Group}.{jobKey.Name}");
            var exePath = data.GetString("GameName");
            if (string.IsNullOrEmpty(exePath))
            {
                Console.WriteLine($"执行失败...");
                return;
            }
            //获取文件下发
            exePath = $"{Environment.CurrentDirectory}\\GameProgram\\{exePath}\\{exePath}.exe";
            var isd = Directory.Exists(exePath);

            var pipName = $"./{jobKey.Group}/{jobKey.Name}/{context.Trigger.Key.Group}/{context.Trigger.Key.Name}"; 
            Channel<JobInfo> aevent = Channel.CreateUnbounded<JobInfo>();




            await Collection(aevent, exePath, pipName, context);




            return;

        }


        private async Task Collection(Channel<JobInfo> endCh,string exePath,string pipName, IJobExecutionContext context)
        {
            JobDataMap data = context.MergedJobDataMap;
            var token = context.CancellationToken;
            StringBuilder outStr = new StringBuilder();

            //启动进程通讯,out
            await using var outServer = new PipeServer<PipeMessage>(pipName);
            outServer.Formatter = new H.Formatters.JsonFormatter();
            outServer.ClientConnected += async (o, args) =>
            {
                //连接成功 下发配置文件!
                await args.Connection.WriteAsync(new PipeMessage()
                {
                    Id = 0,
                    Configure = data.GetString("Configure"),
                });
                await args.Connection.WriteAsync(new PipeMessage()
                {
                    Id = -1,
                    Input = $"{context.Trigger.Key.Group}-{context.Trigger.Key.Name}",
                });
            };
            outServer.MessageReceived += async (sender, args) =>
            {
               await OnEventHandler(sender, args, context, outStr, endCh);
            } ;
            outServer.ExceptionOccurred += (o, args) => Console.WriteLine(args.Exception);
            _ = outServer.StartAsync(token);


            
            if (!StartProcess(exePath, pipName, out var process))
            {
                throw new Exception("启动进程出错");
            }
            token.Register(async () =>
            {
                //任务被取消
                await outServer.WriteAsync(new PipeMessage() { Id = 3 });
                await endCh.Writer.WriteAsync(new JobInfo() { warning = "任务被取消" });
                process?.Kill();
            });

            var source = CancellationTokenSource.CreateLinkedTokenSource(token);
            //等待进程退出,不正常退出都提示!
            _ = Task.Run(async () =>
            {
                await process.WaitForExitAsync(source.Token);
                await endCh.Writer.WriteAsync(new JobInfo() { error = "进程提前退出!" }, source.Token);
            }, source.Token);
            var info = await endCh.Reader.ReadAsync(token);//等待触发消息通知
            await outServer.StopAsync();//有消息了!任务不是出错就是完成了,直接关闭通讯
            source.Cancel(true);//取消进程等待
            //任务完成,查看状态!
            if (!string.IsNullOrEmpty(info.error))
            {
                //任务没有正常完成,抛出错误!
                throw new Exception(info.error);
            }
            else
            {
                //任务正常完成
            }
            //写入输出数据
            data["Output"] = outStr.ToString();
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

        private async Task OnEventHandler(object sender, 
            ConnectionMessageEventArgs<PipeMessage?> args, 
            IJobExecutionContext context, 
            StringBuilder outBuilder,
            Channel<JobInfo> endCh)
        {
            JobDataMap data = context.MergedJobDataMap;
            //获取到配置成功,发送输入数据
            if (args.Message != null)
            {
                switch (args.Message.Id)
                {
                    case 0:
                        //配置返回
                        if (args.Message.Output.Equals("ok"))
                        {
                            await args.Connection.WriteAsync(new PipeMessage() { Id = 1, Input = data.GetString("Input") });
                        }
                        break;
                    case 1:
                        //已经验证完数据
                        if (args.Message.Output.Equals("ok"))
                        {
                            await args.Connection.WriteAsync(new PipeMessage() { Id = 2, Input = "Start" });
                        }

                        break;
                    case 2:
                        //开始返回
                        if (args.Message.Output.Equals("ok"))
                        {
                            //任务开始成功
                        }
                        break;
                    case 3:
                        //任务取消返回!由客户端发起!取消了任务
                       await endCh.Writer.WriteAsync(new JobInfo(){error = "客户端取消任务"});
                        break;
                    case 4:
                        //记录日志返回
                        break;
                    case 5:
                        //返回数据单条输出
                        outBuilder.AppendLine(args.Message.Output);
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
