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

namespace RuoYi.Iot.Controllers
{
    /// <summary>
    /// 物联网设备
    /// </summary>
    [ApiDescriptionSettings("Iot")]
    [Route("iot/device")]
    public class IotDeviceController : ControllerBase
    {
        private readonly ILogger<IotDeviceController> _logger;
        private readonly IotDeviceService _service;

        public IotDeviceController(ILogger<IotDeviceController> logger,IotDeviceService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet("list")]
        public async Task<SqlSugarPagedList<IotDeviceDto>> List([FromQuery] IotDeviceDto dto)
        {
            return await _service.GetDtoPagedListAsync(dto);
        }

        [HttpGet("{id}")]
        public async Task<AjaxResult> Get(long id)
        {
            var data = await _service.GetDtoAsync(id);
            return AjaxResult.Success(data);
        }

        [HttpPost]
        [Log(Title = "设备",BusinessType = BusinessType.INSERT)]
        public async Task<AjaxResult> Add([FromBody] IotDeviceDto dto)
        {
            var ok = await _service.InsertAsync(dto);
            return AjaxResult.Success(ok);
        }

        [HttpPut]
        [Log(Title = "设备",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> Edit([FromBody] IotDeviceDto dto)
        {
            var data = await _service.UpdateAsync(dto);
            return AjaxResult.Success(data);
        }

        [HttpDelete("{ids}")]
        [Log(Title = "设备",BusinessType = BusinessType.DELETE)]
        public async Task<AjaxResult> Delete(long[] ids)
        {
            var data = await _service.DeleteAsync(ids);
            return AjaxResult.Success(data);
        }
    }
}
