using AspectCore.Extensions.DependencyInjection;
using RuoYi.Framework.Logging;

using RuoYi.Zk.AC.Services;               // ← 你的后台服务
using Serilog;                            // ← Serilog 核心
using Serilog.Events;                     // ← 日志级别枚举
using Serilog.AspNetCore;                 // ← Serilog.AspNetCore 提供 UseSerilog() 扩展


internal class Program
{
    private static void Main(string[] args)
    {
        //var builder = WebApplication.CreateBuilder(args).Inject(); 原始



        //---------------------------------设置日志导出路径---------------------------------

        // 【修改①】通过 WebApplicationOptions 指定 ContentRootPath
        var webAppOptions = new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        };
        var builder = WebApplication.CreateBuilder(webAppOptions)
            .Inject();

        // 【修改②】读取配置并初始化 Serilog
        var section = builder.Configuration.GetSection("LoggingFile");
        var logDir = Path.Combine(AppContext.BaseDirectory,section["Path"] ?? "Logs");
        Directory.CreateDirectory(logDir);

        // 注意：这里用的是 Serilog.Log ，不会跟 RuoYi.Framework.Logging.Log 冲突
        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()  // ← 新增：控制台输出
            .WriteTo.File(
                Path.Combine(logDir,section["FileName"] ?? "ruoyi-.log"),
                rollingInterval: Enum.Parse<RollingInterval>(section["RollingInterval"] ?? "Day")
            )
            .CreateLogger();

        // 【修改③】启用 Serilog 扩展
        builder.Host.UseSerilog().UseConsoleLifetime();  // ← 新增：挂载控制台生命周期;



        //-------------------------------------面是你原有的启动流程------------------------------------------------

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            // Set properties and call methods on options
            // serverOptions.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
            serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(3);
            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
        });

        // 用AspectCore替换默认的IOC容器, 用于AOP拦截, 如 事务拦截器: TransactionalAttribute 
        builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());


        // ← 在这里注册后台服务   新增的tcp业务
        builder.Services.AddHostedService<SensorDataListenerService>();


        builder.Build().Run();
    }
}