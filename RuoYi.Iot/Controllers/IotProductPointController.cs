using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RuoYi.Common.Enums;
using RuoYi.Common.Utils;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Framework;
using RuoYi.Iot.Services;
using RuoYi.System;
using SqlSugar;

namespace RuoYi.Iot.Controllers
{
    [ApiDescriptionSettings("Iot")]
    [Route("iot/productPoint")]
    [AllowAnonymous] //匿名
    public class IotProductPointController : ControllerBase
    {
        private readonly ILogger<IotProductPointController> _logger;
        private readonly IotProductPointService _service;

        public IotProductPointController(ILogger<IotProductPointController> logger,IotProductPointService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet("list")]
        public async Task<SqlSugarPagedList<IotProductPointDto>> List([FromQuery] IotProductPointDto dto)
        {
            return await _service.GetDtoPagedListAsync(dto);
        }

        [HttpGet("infobyid/{id}")]
        public async Task<AjaxResult> Get(long id)
        {
            var data = await _service.GetDtoAsync(id);
            return AjaxResult.Success(data);
        }

        [HttpPost("add")]
        [Log(Title = "产品点位新增",BusinessType = BusinessType.INSERT)]
        public async Task<AjaxResult> Add([FromBody] IotProductPointDto dto)
        {
            dto.Id = NextId.Id13(); //必须使用这个生成id
            dto.Status = "0";
            dto.DelFlag = "0";
            var ok = await _service.InsertAsync(dto);
            return AjaxResult.Success(ok);
        }

        [HttpPost("edit")]
        [Log(Title = "产品点位",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> Edit([FromBody] IotProductPointDto dto)
        {
            var data = await _service.UpdateAsync(dto);
            return AjaxResult.Success(data);
        }

        [HttpPost("delete")]
        [Log(Title = "产品点位",BusinessType = BusinessType.DELETE)]
        public async Task<AjaxResult> Delete(long[] ids)
        {
            var data = await _service.DeleteAsync(ids);
            return AjaxResult.Success(data);
        }
    }
}
