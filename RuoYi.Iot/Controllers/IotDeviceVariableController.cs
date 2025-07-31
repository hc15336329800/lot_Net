using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RuoYi.Common.Enums;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Framework;
using RuoYi.Iot.Services;
using RuoYi.System;
using SqlSugar;
using RuoYi.Common.Utils;


namespace RuoYi.Iot.Controllers
{
    [ApiDescriptionSettings("Iot")]
    [Route("iot/deviceVariable")]
    [AllowAnonymous] //匿名
    public class IotDeviceVariableController : ControllerBase
    {
        private readonly ILogger<IotDeviceVariableController> _logger;
        private readonly IotDeviceVariableService _service;

        public IotDeviceVariableController(ILogger<IotDeviceVariableController> logger,IotDeviceVariableService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet("list")]
        public async Task<SqlSugarPagedList<IotDeviceVariableDto>> List([FromQuery] IotDeviceVariableDto dto)
        {
            return await _service.GetDtoPagedListAsync(dto);
        }


        //查询详情，根据device_id
        [HttpGet("infobyid/{device_id}")]
        public async Task<AjaxResult> Get([FromRoute(Name = "device_id")] long deviceId)
        {
            await _service.SyncDeviceVariablesAsync(deviceId);
            var list = await _service.GetDtoListAsync(new IotDeviceVariableDto { DeviceId = deviceId });
            return AjaxResult.Success(list);
        }

        [HttpPost]
        [Log(Title = "设备变量",BusinessType = BusinessType.INSERT)]
        public async Task<AjaxResult> Add([FromBody] IotDeviceVariableDto dto)
        {
            var ok = await _service.InsertAsync(dto);

             if(ok)
            {
                _service.RemoveCache(dto.DeviceId ?? 0);
            }

            if(ok && dto.DeviceId.HasValue && dto.VariableId.HasValue && !string.IsNullOrEmpty(dto.VariableKey) && !string.IsNullOrEmpty(dto.CurrentValue))
            {
                await _service.SaveValueAsync(dto.DeviceId.Value,dto.VariableId.Value,dto.VariableKey!,dto.CurrentValue!);
            }

            return AjaxResult.Success(ok);
        }

        [HttpPut]
        [Log(Title = "设备变量",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> Edit([FromBody] IotDeviceVariableDto dto)
        {
            var data = await _service.UpdateAsync(dto);

            if(data > 0)
            {
                _service.RemoveCache(dto.DeviceId ?? 0);
            }


            if(dto.DeviceId.HasValue && dto.VariableId.HasValue && !string.IsNullOrEmpty(dto.VariableKey) && !string.IsNullOrEmpty(dto.CurrentValue))
            {
                await _service.SaveValueAsync(dto.DeviceId.Value,dto.VariableId.Value,dto.VariableKey!,dto.CurrentValue!);
            }

            return AjaxResult.Success(data);
        }
 

        [HttpPost("delete")]
        [Log(Title = "设备变量",BusinessType = BusinessType.DELETE)]
        public async Task<AjaxResult> Delete([FromBody] long[] ids)
        {
            var data = await _service.DeleteAsync(ids);
            return AjaxResult.Success(data);
        }



        /// <summary>
        /// 获取设备所有点位的最新数据
        /// </summary>
        [HttpGet("latest/{deviceId}")]
        public async Task<AjaxResult> Latest(long deviceId)
        {
            var list = await _service.GetLatestListAsync(deviceId);
            return AjaxResult.Success(list);
        }


    }
}
