using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RuoYi.Common.Data;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Data.Entities.Iot;
using RuoYi.Framework.DependencyInjection;
using RuoYi.Iot.Repositories;

namespace RuoYi.Iot.Services;

public class IotDeviceService : BaseService<IotDevice,IotDeviceDto>, ITransient
{
    private readonly IotDeviceRepository _repo;
    private readonly ILogger<IotDeviceService> _logger;

    public IotDeviceService(ILogger<IotDeviceService> logger,IotDeviceRepository repo)
    {
        _logger = logger;
        _repo = repo;
        BaseRepo = repo;
    }


    public async Task<IotDevice?> GetByPacketAsync(string packet)
    {
        return await _repo.GetByPacketAsync(packet);
    }

    public async Task<IotDevice> GetAsync(long id)
    {
        return await base.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IotDeviceDto> GetDtoAsync(long id)
    {
        var dto = new IotDeviceDto { Id = id };
        return await _repo.GetDtoFirstAsync(dto);
    }

    public async Task<int> UpdateStatusAsync(long id,string status)
    {
        return await _repo.UpdateStatusAsync(id,status);
    }

    /// <summary>
    /// 根据设备信息生成自动注册包字符串
    /// </summary>
    public string BuildAutoRegPacket(IotDeviceDto device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var host = device.TcpHost ?? string.Empty;
        var port = device.TcpPort?.ToString() ?? string.Empty;
        var pid = device.ProductId?.ToString() ?? string.Empty;
        var dn = device.DeviceDn ?? string.Empty;
        var key = device.CommKey ?? string.Empty;

        return $"{host}:{port}?pid={pid}&dn={dn}&key={key}";
    }
}