using AspectCore.Extensions.DependencyInjection;
using RuoYi.Zk.AC.Services;           // �� ����


internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args).Inject();
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