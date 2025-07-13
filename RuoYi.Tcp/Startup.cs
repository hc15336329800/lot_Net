using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RuoYi.Framework;
using RuoYi.Tcp.Services;
using Microsoft.Extensions.Options;
using RuoYi.Tcp.Configs;
using RuoYi.Iot.Services;



namespace RuoYi.Tcp
{


    // 注册服务到框架中
    [AppStartup(500)]
    public sealed class Startup : AppStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // 绑定应用配置中的 TcpServer 配置节到 TcpServerOptions 对象
            // 这样可以通过配置文件来初始化 TcpServerOptions 的属性
            services.AddOptions<TcpServerOptions>().BindConfiguration("TcpServer");



            // 注册 ModbusRtuService 为单例服务
            // 该服务将会在整个应用生命周期内始终保持同一个实例
            services.AddSingleton<ModbusRtuService>();

            // 注册 ITcpService 接口的实现，使用 ModbusRtuService 作为实现
            // 通过依赖注入提供 ModbusRtuService 实例，方便其他服务或控制器使用
            services.AddSingleton<ITcpService>(sp => sp.GetRequiredService<ModbusRtuService>());

            // ModbusRtuService 仅在设备主动连接时由 TcpService 调用
            // 不再作为后台任务主动轮询设备
 

            // 注册 TcpService 为单例服务
            services.AddSingleton<TcpService>();
            services.AddSingleton<ITcpSender>(sp => sp.GetRequiredService<TcpService>());


            // 将 TcpService 注册为后台服务
            // 这会启动一个长期运行的后台任务
            services.AddHostedService(sp => sp.GetRequiredService<TcpService>());

        }
    }
}
