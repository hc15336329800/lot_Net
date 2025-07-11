//using AspectCore.Extensions.DependencyInjection;
//using RuoYi.Framework.Logging;

//using RuoYi.Zk.AC.Services;               // ← 你的后台服务
//using Serilog;                            // ← Serilog 核心
//using Serilog.Events;                     // ← 日志级别枚举
//using Serilog.AspNetCore;                 // ← Serilog.AspNetCore 提供 UseSerilog() 扩展


//internal class Program
//{
//    private static void Main(string[] args)
//    {
//        //var builder = WebApplication.CreateBuilder(args).Inject(); 原始



//        //---------------------------------设置日志导出路径---------------------------------

//        // 【修改①】通过 WebApplicationOptions 指定 ContentRootPath
//        var webAppOptions = new WebApplicationOptions
//        {
//            Args = args,
//            ContentRootPath = AppContext.BaseDirectory
//        };
//        var builder = WebApplication.CreateBuilder(webAppOptions)
//            .Inject();

//        // 【修改②】读取配置并初始化 Serilog
//        var section = builder.Configuration.GetSection("LoggingFile");
//        var logDir = Path.Combine(AppContext.BaseDirectory,section["Path"] ?? "Logs");
//        Directory.CreateDirectory(logDir);

//        // 注意：这里用的是 Serilog.Log ，不会跟 RuoYi.Framework.Logging.Log 冲突
//        Serilog.Log.Logger = new LoggerConfiguration()
//            .MinimumLevel.Information()
//            .Enrich.FromLogContext()
//            .WriteTo.Console()  // ← 新增：控制台输出
//            .WriteTo.File(
//                Path.Combine(logDir,section["FileName"] ?? "ruoyi-.log"),
//                rollingInterval: Enum.Parse<RollingInterval>(section["RollingInterval"] ?? "Day")
//            )
//            .CreateLogger();

//        // 【修改③】启用 Serilog 扩展
//        builder.Host.UseSerilog().UseConsoleLifetime();  // ← 新增：挂载控制台生命周期;



//        //-------------------------------------面是你原有的启动流程------------------------------------------------

//        builder.WebHost.ConfigureKestrel(serverOptions =>
//        {
//            // Set properties and call methods on options
//            // serverOptions.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
//            serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(3);
//            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
//        });

//        // 用AspectCore替换默认的IOC容器, 用于AOP拦截, 如 事务拦截器: TransactionalAttribute 
//        builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());


//        // ← 在这里注册后台服务   新增的tcp业务
//        builder.Services.AddHostedService<SensorDataListenerService>();


//        builder.Build().Run();
//    }
//}




// 旧的额
using AspectCore.Extensions.DependencyInjection;
using RuoYi.Zk.AC.Services;           // ← 新增


internal class Program
{
    private static void Main(string[] args)// 应用程序的入口点，定义在 Main 方法中
    {
        // 创建 Web 应用程序构建器并调用 Inject 方法进行依赖注入初始化
        // Inject() 方法用于将 AspectCore 注入到默认的 IoC 容器中，使得 AOP 功能可以在整个应用中使用
        var builder = WebApplication.CreateBuilder(args).Inject();

        // 配置 Kestrel 服务器的一些限制和选项，Kestrel 是 ASP.NET Core 的内置轻量级 Web 服务器
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            // 设置请求体的最大大小 (单位为字节)，这里被注释掉了，表示如果取消注释，可以限制客户端上传文件或数据的大小
            // serverOptions.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 限制为 50 MB

            // 设置服务器连接的保持活动时间，即在空闲连接时保持连接不关闭的时间，这里设置为 3 分钟
            serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(3);

            // 设置请求头的超时时间，如果服务器没有在指定时间内收到完整的请求头，则终止请求。这里设置为 1 分钟
            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
        });

        // 用 AspectCore 替换默认的 IoC 容器，以支持 AOP 特性
        // AOP 可以用于拦截方法调用并在其前后执行自定义逻辑，比如事务管理、日志记录等
        // TransactionalAttribute 是常见的事务拦截器示例，用于确保方法在事务中执行，发生异常时会回滚
        builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());


        // ← 在这里注册后台服务   新增的tcp业务
        builder.Services.AddHostedService<SensorDataListenerService>();


        // 构建应用程序，准备启动
        builder.Build().Run();// 构建并启动 Web 应用程序，监听和处理 HTTP 请求
    }
}