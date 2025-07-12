using Microsoft.Extensions.DependencyInjection;
using RuoYi.Framework;
using RuoYi.Mqtt.Services;

namespace RuoYi.Mqtt
{
    // MQTT 服务虽然没有在 Program.cs 中显式注册，但它已经通过框架的 AppStartup 机制自动集成到应用启动流程。
    // 框架在 AddApp() 中执行 AddStartups() 扫描,AddStartups() 会遍历所有继承 AppStartup 的类并执行其 ConfigureServices 方法



    [AppStartup(500)] // 设置模块启动顺序，确保在 Admin 启动后加载
    public sealed class Startup : AppStartup
    {
        //依赖注入(应用程序的启动过程中用于注册应用所需的服务和中间件)
        public void ConfigureServices(IServiceCollection services)
        {
            // 将 MQTT 服务注册为单例模式
            services.AddSingleton<IMqttService,MqttService>();

 

            // 如果有其他服务需要在这个模块中初始化，可以在这里进行
        }

        // 此处无需再定义 Configure 方法，因为在全局的 Startup 中已经处理了 HTTP 管道配置。
    }
}
