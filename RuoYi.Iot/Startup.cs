using Microsoft.Extensions.DependencyInjection;
using RuoYi.Framework;
using RuoYi.Iot.Controllers;

namespace RuoYi.Iot;

[AppStartup(501)]
public sealed class Startup : AppStartup
{
    /// <summary>
    /// 依赖注入服务注册入口（RuoYi.NET框架模块化方式）。
    /// 在此方法中注册当前模块的相关服务。
    /// </summary>
    /// <param name="services">IoC容器服务集合</param>
    public void ConfigureServices(IServiceCollection services)
    {
        // 将IotDeviceController注册为瞬时对象（每次请求都创建新实例）
        services.AddTransient<IotDeviceController>();
    }
}
