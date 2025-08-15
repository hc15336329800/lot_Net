using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RuoYi.Quartz.Services;

namespace RuoYi.Quartz;

[AppStartup(100)]
public sealed class Startup : AppStartup
{


    //用 ScheduleHostedService（托管服务）替换了基于线程的启动流程，在服务配置时通过依赖注入注册调度器。
    public void ConfigureServices(IServiceCollection services)
    {
        //_scheduleThread.Start(); //原始的启动方式
        services.AddHostedService<ScheduleHostedService>();
    }
}


//实现了一个 BackgroundService，在启动时立即调用 InitSchedule( )，并带有错误日志和自动重试机制，以在初始化失败时恢复。
internal sealed class ScheduleHostedService : BackgroundService
{
    private readonly SysJobService _jobService;
    private readonly ILogger<ScheduleHostedService> _logger;

    public ScheduleHostedService(SysJobService jobService,ILogger<ScheduleHostedService> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _jobService.InitSchedule();
                break;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,"初始化任务调度失败，5秒后重试");
                await Task.Delay(TimeSpan.FromSeconds(5),stoppingToken);
            }
        }
    }
}