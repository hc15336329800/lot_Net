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
using RuoYi.Framework.DataEncryption.Extensions;

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
    public static string BuildAutoRegPacket(IotDeviceDto device,bool encrypt = false)
    {
        ArgumentNullException.ThrowIfNull(device);

        const string header = "##"; // two byte packet header
        var dn = (device.DeviceDn ?? string.Empty).PadRight(20).Substring(0,20);
        var key = (device.CommKey ?? string.Empty).PadRight(8).Substring(0,8);
        var card = (device.IotCardNo ?? string.Empty).PadRight(20).Substring(0,20);

        var plain = string.Concat(header,dn,key,card);

        if(encrypt && !string.IsNullOrEmpty(device.CommKey))
        {
            var bytes = Encoding.UTF8.GetBytes(plain);
            var enc = bytes.ToAESEncrypt(device.CommKey);
            return Convert.ToBase64String(enc);
        }

        return plain;
    }
}