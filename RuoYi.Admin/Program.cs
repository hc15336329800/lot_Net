//using AspectCore.Extensions.DependencyInjection;
//using RuoYi.Framework.Logging;

//using RuoYi.Zk.AC.Services;               // �� ��ĺ�̨����
//using Serilog;                            // �� Serilog ����
//using Serilog.Events;                     // �� ��־����ö��
//using Serilog.AspNetCore;                 // �� Serilog.AspNetCore �ṩ UseSerilog() ��չ


//internal class Program
//{
//    private static void Main(string[] args)
//    {
//        //var builder = WebApplication.CreateBuilder(args).Inject(); ԭʼ



//        //---------------------------------������־����·��---------------------------------

//        // ���޸Ģ١�ͨ�� WebApplicationOptions ָ�� ContentRootPath
//        var webAppOptions = new WebApplicationOptions
//        {
//            Args = args,
//            ContentRootPath = AppContext.BaseDirectory
//        };
//        var builder = WebApplication.CreateBuilder(webAppOptions)
//            .Inject();

//        // ���޸Ģڡ���ȡ���ò���ʼ�� Serilog
//        var section = builder.Configuration.GetSection("LoggingFile");
//        var logDir = Path.Combine(AppContext.BaseDirectory,section["Path"] ?? "Logs");
//        Directory.CreateDirectory(logDir);

//        // ע�⣺�����õ��� Serilog.Log ������� RuoYi.Framework.Logging.Log ��ͻ
//        Serilog.Log.Logger = new LoggerConfiguration()
//            .MinimumLevel.Information()
//            .Enrich.FromLogContext()
//            .WriteTo.Console()  // �� ����������̨���
//            .WriteTo.File(
//                Path.Combine(logDir,section["FileName"] ?? "ruoyi-.log"),
//                rollingInterval: Enum.Parse<RollingInterval>(section["RollingInterval"] ?? "Day")
//            )
//            .CreateLogger();

//        // ���޸Ģۡ����� Serilog ��չ
//        builder.Host.UseSerilog().UseConsoleLifetime();  // �� ���������ؿ���̨��������;



//        //-------------------------------------������ԭ�е���������------------------------------------------------

//        builder.WebHost.ConfigureKestrel(serverOptions =>
//        {
//            // Set properties and call methods on options
//            // serverOptions.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
//            serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(3);
//            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
//        });

//        // ��AspectCore�滻Ĭ�ϵ�IOC����, ����AOP����, �� ����������: TransactionalAttribute 
//        builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());


//        // �� ������ע���̨����   ������tcpҵ��
//        builder.Services.AddHostedService<SensorDataListenerService>();


//        builder.Build().Run();
//    }
//}




// �ɵĶ�
using AspectCore.Extensions.DependencyInjection;
using RuoYi.Zk.AC.Services;           // �� ����


internal class Program
{
    private static void Main(string[] args)// Ӧ�ó������ڵ㣬������ Main ������
    {
        // ���� Web Ӧ�ó��򹹽��������� Inject ������������ע���ʼ��
        // Inject() �������ڽ� AspectCore ע�뵽Ĭ�ϵ� IoC �����У�ʹ�� AOP ���ܿ���������Ӧ����ʹ��
        var builder = WebApplication.CreateBuilder(args).Inject();

        // ���� Kestrel ��������һЩ���ƺ�ѡ�Kestrel �� ASP.NET Core ������������ Web ������
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            // ���������������С (��λΪ�ֽ�)�����ﱻע�͵��ˣ���ʾ���ȡ��ע�ͣ��������ƿͻ����ϴ��ļ������ݵĴ�С
            // serverOptions.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // ����Ϊ 50 MB

            // ���÷��������ӵı��ֻʱ�䣬���ڿ�������ʱ�������Ӳ��رյ�ʱ�䣬��������Ϊ 3 ����
            serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(3);

            // ��������ͷ�ĳ�ʱʱ�䣬���������û����ָ��ʱ�����յ�����������ͷ������ֹ������������Ϊ 1 ����
            serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
        });

        // �� AspectCore �滻Ĭ�ϵ� IoC ��������֧�� AOP ����
        // AOP �����������ط������ò�����ǰ��ִ���Զ����߼����������������־��¼��
        // TransactionalAttribute �ǳ���������������ʾ��������ȷ��������������ִ�У������쳣ʱ��ع�
        builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());


        // �� ������ע���̨����   ������tcpҵ��
        builder.Services.AddHostedService<SensorDataListenerService>();


        // ����Ӧ�ó���׼������
        builder.Build().Run();// ���������� Web Ӧ�ó��򣬼����ʹ��� HTTP ����
    }
}