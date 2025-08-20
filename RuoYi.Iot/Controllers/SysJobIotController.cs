using System.Linq;
using RuoYi.Common.Enums;
using RuoYi.Framework;
using RuoYi.System;
using RuoYi.Quartz.Dtos;
using RuoYi.Quartz.Services;
using SqlSugar;
using RuoYi.Quartz.Enums;
using RuoYi.Quartz.Utils;

namespace RuoYi.Iot.Controllers
{
    [ApiDescriptionSettings("Iot")]
    [Route("iot/job")]
    [AllowAnonymous] //匿名
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

        [HttpGet("listByDeviceId/{deviceId}")]
        public async Task<List<SysJobIotDto>> GetByDevice(long deviceId)
        {
            return await _service.GetListByDeviceId(deviceId);
        }

        [HttpGet("listByProduct/{productId}")]
        public async Task<List<SysJobIotDto>> GetByProduct(long productId)
        {
            return await _service.GetListByProductId(productId);
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

        [HttpPut("changeStatus")]
        [Log(Title = "定时任务",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> ChangeStatus([FromBody] SysJobIotDto dto)
        {
            var success = await _service.ChangeStatusAsync(dto);
            return success ? AjaxResult.Success() : AjaxResult.Error();
        }


        // 运行一次任务
        [HttpPut("run")]
        [Log(Title = "定时任务 运行一次",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> Run([FromBody] SysJobIotDto dto)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 运行一次任务开始");


            var result = await _service.Run(dto);
            return result ? AjaxResult.Success() : AjaxResult.Error("任务不存在或已过期！");
        }

 
        /// <summary>
        ///  任务启停：启动则任务按 Cron 表达式执行  
        /// </summary>
        /// <param name="dto">任务对象</param>
        [HttpPut("runCron")]
        [Log(Title = "定时任务启停",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> RunCron([FromBody] SysJobIotDto dto)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 运行Cron任务开始");
            //return AjaxResult.Success();

            dto.Status = dto.Star == 1 ? ScheduleStatus.NORMAL.GetValue() : ScheduleStatus.PAUSE.GetValue();
            var success = await _service.ChangeStatusAsync(dto);
            if(success && dto.Star == 1)
            {
                var scheduler = await ScheduleUtils.GetDefaultScheduleAsync();
                if(!scheduler.IsStarted || scheduler.InStandbyMode)
                {
                    await scheduler.Start();
                }
            }
            return success ? AjaxResult.Success() : AjaxResult.Error();
        }
    }
}