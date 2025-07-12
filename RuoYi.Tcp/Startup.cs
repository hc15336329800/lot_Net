using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RuoYi.Framework;
using RuoYi.Tcp.Services;

namespace RuoYi.Tcp
{


    // 注册服务到框架中
    [AppStartup(500)]
    public sealed class Startup : AppStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ModbusTcpService>();
            services.AddSingleton<ITcpService>(sp => sp.GetRequiredService<ModbusTcpService>());
            services.AddHostedService(sp => sp.GetRequiredService<ModbusTcpService>());
 
        }
    }
}
