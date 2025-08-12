using System.Linq;
using RuoYi.Common.Enums;
using RuoYi.Framework;
using RuoYi.System;
using RuoYi.Quartz.Dtos;
using RuoYi.Quartz.Services;
using SqlSugar;

namespace RuoYi.Iot.Controllers
{
    [ApiDescriptionSettings("Iot")]
    [Route("iot/job")]
    [AllowAnonymous]
    public class SysJobIotController : ControllerBase
    {
        private readonly SysJobIotService _service;

        public SysJobIotController(SysJobIotService service)
        {
            _service = service;
        }

        [HttpGet("list")]
        public async Task<SqlSugarPagedList<SysJobIotDto>> List([FromQuery] SysJobIotDto dto)
        {
            return await _service.GetDtoPagedListAsync(dto);
        }

        [HttpPost("add")]
        [Log(Title = "定时任务",BusinessType = BusinessType.INSERT)]
        public async Task<AjaxResult> Add([FromBody] SysJobIotDto dto)
        {
            var ok = await _service.InsertAsync(dto);
            return AjaxResult.Success(ok);
        }

        [HttpPost("edit")]
        [Log(Title = "定时任务",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> Edit([FromBody] SysJobIotDto dto)
        {
            var ok = await _service.UpdateAsync(dto);
            return AjaxResult.Success(ok);
        }

        [HttpPost("delete")]
        [Log(Title = "定时任务",BusinessType = BusinessType.DELETE)]
        public async Task<AjaxResult> Delete([FromBody] long[] ids)
        {
            await _service.DeleteAsync(ids.ToList());
            return AjaxResult.Success();
        }
    }
}