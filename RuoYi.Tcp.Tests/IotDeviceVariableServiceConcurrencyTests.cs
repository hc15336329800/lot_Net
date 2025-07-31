using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RuoYi.Data.Entities.Iot;
using RuoYi.Iot.Repositories;
using RuoYi.Iot.Services;
using SqlSugar;
using Xunit;

public class IotDeviceVariableServiceConcurrencyTests
{
    private class DummyCache : RuoYi.Framework.Cache.ICache
    {
        private readonly Dictionary<string,object?> _store = new();
        public string? GetString(string key) => null;
        public string? GetStringAndRemove(string key) => null;
        public void SetString(string key,string value) { }
        public void SetString(string key,string value,long minutes) { }
        public void Remove(string key) { }
        public Task<string?> GetStringAsync(string key) => Task.FromResult<string?>(null);
        public Task SetStringAsync(string key,string value) => Task.CompletedTask;
        public Task SetStringAsync(string key,string value,long minutes) => Task.CompletedTask;
        public Task RemoveAsync(string key) => Task.CompletedTask;
        public IEnumerable<string> GetDbKeys(string pattern,int pageSize = 1000) => Array.Empty<string>();
        public Task<Dictionary<string,string>> GetDbInfoAsync(params object[] args) => Task.FromResult(new Dictionary<string,string>());
        public Task<long> GetDbSize( ) => Task.FromResult(0L);
        public void RemoveByPattern(string pattern) { }
        public T Get<T>(string key) => _store.TryGetValue(key,out var v) ? (T)v! : default!;
        public void Set<T>(string key,T value) => _store[key] = value;
        public void Set<T>(string key,T value,long minutes) => _store[key] = value;
        public void Remove<T>(string key) => _store.Remove(key);
        public Task<T> GetAsync<T>(string key) => Task.FromResult(_store.TryGetValue(key,out var v) ? (T)v! : default!);
        public Task SetAsync<T>(string key,T value) { _store[key] = value; return Task.CompletedTask; }
        public Task SetAsync<T>(string key,T value,long minutes) { _store[key] = value; return Task.CompletedTask; }
    }

    private class Env : Microsoft.AspNetCore.Hosting.IWebHostEnvironment, Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = System.IO.Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(System.IO.Directory.GetCurrentDirectory());
        public string WebRootPath { get; set; } = System.IO.Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(System.IO.Directory.GetCurrentDirectory());
    }

    private static SqlSugarClient CreateDb( )
    {
        var config = new ConnectionConfig
        {
            ConnectionString = $"DataSource={Guid.NewGuid()}.db",
            DbType = DbType.Sqlite,
            InitKeyType = InitKeyType.Attribute,
            IsAutoCloseConnection = true
        };
        var db = new SqlSugarClient(config);
        db.CodeFirst.InitTables<IotDeviceVariable,IotDeviceVariableHistory>();
        return db;
    }

    [Fact]
    public async Task ConcurrentSaveValueAsync_UsesSeparateConnections( )
    {
        var dbConfig = new ConnectionConfig
        {
            ConnectionString = $"DataSource={Guid.NewGuid()}.db",
            DbType = DbType.Sqlite,
            InitKeyType = InitKeyType.Attribute,
            IsAutoCloseConnection = true
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ISqlSugarClient>(_ => new SqlSugarClient(dbConfig));
        services.AddScoped(typeof(ISqlSugarRepository<>),typeof(SqlSugarRepository<>));
        services.AddScoped<IotDeviceRepository>();
        services.AddScoped<IotProductPointRepository>();
        services.AddScoped<IotDeviceService>();
        services.AddScoped<IotProductPointService>();
        services.AddScoped<IotDeviceVariableRepository>();
        services.AddScoped<IotDeviceVariableHistoryRepository>();
        services.AddSingleton<RuoYi.Framework.Cache.ICache,DummyCache>();
        services.AddScoped<IotDeviceVariableService>();

        var provider = services.BuildServiceProvider();
        var internalAppType = Type.GetType("RuoYi.Framework.InternalApp, RuoYi.Framework")!;
        internalAppType.GetField("RootServices",System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.SetValue(null,provider);
        internalAppType.GetField("InternalServices",System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.SetValue(null,services);
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string,string?>
            {
                {"AppSettings:SupportPackageNamePrefixs:0","RuoYi"}
            })
            .Build();
        internalAppType.GetField("Configuration",System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.SetValue(null,config);
        var envStub = new Env();
        internalAppType.GetField("HostEnvironment",System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.SetValue(null,envStub);
        internalAppType.GetField("WebHostEnvironment",System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.SetValue(null,envStub);
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(RuoYi.Framework.App).TypeHandle);

        // initialize table with one variable row
        using(var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            db.CodeFirst.InitTables<IotDeviceVariable,IotDeviceVariableHistory>();
            db.Insertable(new IotDeviceVariable
            {
                Id = 1,
                DeviceId = 1,
                VariableId = 1,
                Status = "0",
                DelFlag = "0",
                CurrentValue = "",
                LastUpdateTime = DateTime.Now,
                Remark = "",
                CreateBy = "",
                CreateTime = DateTime.Now,
                UpdateBy = "",
                UpdateTime = DateTime.Now
            }).ExecuteCommand();
        }

        var service = provider.GetRequiredService<IotDeviceVariableService>();

        var tasks = Enumerable.Range(0,5).Select(i => service.SaveValueAsync(1,1,"k",i.ToString())).ToArray();
        await Task.WhenAll(tasks);

        using(var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var count = db.Queryable<IotDeviceVariableHistory>().Count();
            Assert.Equal(5,count);
        }
    }
}