using AspectCore.Extensions.DependencyInjection;
using Microsoft.AspNetCore.HttpOverrides;
using Newtonsoft.Json;
using RuoYi.Admin.Authorization;
using RuoYi.Common.Files;
using RuoYi.Common.Utils;
using RuoYi.Framework.Cache;
using RuoYi.Framework.Filters;
using RuoYi.Framework.RateLimit;


namespace RuoYi.Admin
{


    /// <summary>
    /// Startup.cs：负责应用层的配置，主要包含 ConfigureServices（在这里向依赖注入容器注册服务）以及 Configure（设置 HTTP 请求管道，包括各种中间件的顺序）
    /// </summary>
    [AppStartup(10000)]
    public class Startup: AppStartup
    {
        public void ConfigureServices(IServiceCollection services)
         {

            // 自定义 HttpClient : 用于调用外部url接口 实现模块间通讯
            services.AddHttpClient("GeneralClient",client =>
            {
                client.BaseAddress = new Uri("http://localhost:5000/"); // 替换为实际服务地址
                client.Timeout = TimeSpan.FromSeconds(30); // 设置请求超时
            });

            // 自定义 HttpRequestService
            services.AddScoped<HttpRequestService>();


            //Console 日志格式化：
            services.AddConsoleFormatter();
            //允许应用程序处理跨域请求，启用了 CORS 功能。
            services.AddCorsAccessor();

            // 注册JWT（JSON Web Token）鉴权功能，用于保护 API 或页面。
            services.AddRyJwt();
            // 捕获全局异常
            services.AddMvc(opt =>
            {
                opt.Filters.Add(typeof(GlobalExceptionFilter));//全局异常过滤器：
            });

            services.AddControllersWithViews()
                // 使用 Newtonsoft.Json 作为 JSON 序列化器 
                .AddNewtonsoftJson(options =>
                {
                    // 忽略循环引用
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;// 忽略循环引用
                    options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";//设置了日期格式

                    // 忽略所有 null 属性
                    //options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    // long 类型序列化时转 string, 防止 JavaScript 出现精度溢出问题
                    //options.SerializerSettings.Converters.AddLongTypeConverters();
                })
                .AddInject(options =>
                {
                    // 不启用全局验证: GlobalEnabled = false
                    options.ConfigureDataValidation(options => { options.GlobalEnabled = false; });
                })
                ;

            // 参数验证返回值处理
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = (context) =>
                {
                    var msg = string.Join(";", context.ModelState.Select(x => x.Value.Errors?.FirstOrDefault().ErrorMessage).ToList());
                    return new JsonResult(AjaxResult.Error(msg));
                };
            });

            // 如果服务器端使用了 nginx/iis 等反向代理工具，可添加以下代码配置：
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.All;

                // 若上面配置无效可尝试下列代码，比如在 IIS 中
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            #region 日志
            // 全局启用 LoggingMonitor 功能
            services.AddMonitorLogging();

            // 日志
            Array.ForEach(new[] { LogLevel.Information, LogLevel.Warning, LogLevel.Error }, logLevel =>
            {
                services.AddFileLogging("logs/application-{1}-{0:yyyy}-{0:MM}-{0:dd}.log", options =>
                {
                    options.FileNameRule = fileName => string.Format(fileName, DateTime.UtcNow, logLevel.ToString());
                    options.WriteFilter = logMsg => logMsg.LogLevel == logLevel;
                });
            });
            #endregion

            // 远程请求 用于处理远程 HTTP 请求。
            services.AddRemoteRequest();

            // SqlSugar 注册 SqlSugar ORM 框架。
            services.AddSqlSugarScope();

            //  Cache 注册缓存服务。
            services.AddCache();

            // SignalR 用于实时通信。
            services.AddSignalR();

            // captcha 注册验证码功能。
            services.AddLazyCaptcha();

            // 自定义拦截器 (AspectCore)
            services.ConfigureDynamicProxy();

            // 限流 https://github.com/cristipufu/aspnetcore-redis-rate-limiting/tree/master
            services.AddConcurrencyLimiter();
        }




        /// <summary>
        /// 如何处理每个传入的 HTTP 请求。
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // 如果服务器端使用了 nginx/iis 等反向代理工具: 注意在 Configure 最前面配置
            app.UseForwardedHeaders();

            if (env.IsDevelopment())//开发环境处理：在开发环境中显示详细的异常信息，而在生产环境中使用全局错误处理和 HSTS
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            //app.UseHttpsRedirection();

            // 配置应用的静态文件支持，使用自定义的 RyStaticFiles 来处理特殊的静态文件需求。并且启用路由。：
            app.UseStaticFiles();
            app.UseRyStaticFiles(env); 
            app.UseRouting();

            // 跨域和认证
            app.UseCorsAccessor();
            app.UseAuthentication();
            app.UseAuthorization();

            // 注入基础中间件
            app.UseInject();

            // 限流
            app.UseRateLimiter();

            // 配置路由映射，将请求路由到控制器的相应方法。
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}