using AspectCore.Extensions.DependencyInjection;
using RuoYi.Framework.Logging;

using RuoYi.Zk.AC.Services;               // �� ��ĺ�̨����
using Serilog;                            // �� Serilog ����
using Serilog.Events;                     // �� ��־����ö��
using Serilog.AspNetCore;                 // �� Serilog.AspNetCore �ṩ UseSerilog() ��չ


internal class Program
{
    private static void Main(string[] args)
    {
        //var builder = WebApplication.CreateBuilder(args).Inject(); ԭʼ



        //---------------------------------������־����·��---------------------------------

        // ���޸Ģ١�ͨ�� WebApplicationOptions ָ�� ContentRootPath
        var webAppOptions = new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        };
        var builder = WebApplication.CreateBuilder(webAppOptions)
            .Inject();

        // ���޸Ģڡ���ȡ���ò���ʼ�� Serilog
        var section = builder.Configuration.GetSection("LoggingFile");
        var logDir = Path.Combine(AppContext.BaseDirectory,section["Path"] ?? "Logs");
        Directory.CreateDirectory(logDir);

        // ע�⣺�����õ��� Serilog.Log ������� RuoYi.Framework.Logging.Log ��ͻ
        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()  // �� ����������̨���
            .WriteTo.File(
                Path.Combine(logDir,section["FileName"] ?? "ruoyi-.log"),
                rollingInterval: Enum.Parse<RollingInterval>(section["RollingInterval"] ?? "Day")
            )
            .CreateLogger();

        // ���޸Ģۡ����� Serilog ��չ
        builder.Host.UseSerilog().UseConsoleLifetime();  // �� ���������ؿ���̨��������;



        //-------------------------------------������ԭ�е���������------------------------------------------------

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            // Set properties and call methods on options
            // serverOptions.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
            serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(3);
            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
        });

        // ��AspectCore�滻Ĭ�ϵ�IOC����, ����AOP����, �� ����������: TransactionalAttribute 
        builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());


        // �� ������ע���̨����   ������tcpҵ��
        builder.Services.AddHostedService<SensorDataListenerService>();


        builder.Build().Run();
    }
}