using AspectCore.Extensions.DependencyInjection;
using RuoYi.Zk.AC.Services;           // ← 新增


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

        // 用AspectCore替换默认的IOC容器, 用于AOP拦截, 如 事务拦截器: TransactionalAttribute 
        builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());


        // ← 在这里注册后台服务   新增的tcp业务
        builder.Services.AddHostedService<SensorDataListenerService>();

        builder.Build().Run();
    }
}