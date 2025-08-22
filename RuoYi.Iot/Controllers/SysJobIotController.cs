using System.Linq;
using RuoYi.Common.Enums;
using RuoYi.Framework;
using RuoYi.System;
using RuoYi.Quartz.Dtos;
using RuoYi.Quartz.Services;
using SqlSugar;
using RuoYi.Quartz.Enums;
using RuoYi.Quartz.Utils;
using RuoYi.Iot.Services;
using RuoYi.Data.Models;
using RuoYi.Common.Utils;
using RuoYi.Quartz.Constants;

namespace RuoYi.Iot.Controllers
{
    [ApiDescriptionSettings("Iot")]
    [Route("iot/job")]
    [AllowAnonymous] //匿名
    public class SysJobIotController : ControllerBase
    {
        private readonly SysJobIotService _service;
        private readonly IotProductPointService _productPointService;


        private readonly IotDeviceService _deviceService;


        public SysJobIotController(SysJobIotService service,IotProductPointService productPointService,IotDeviceService deviceService)
        {
            _service = service;
            _productPointService = productPointService;
            _deviceService = deviceService;
        }


        /////////////////////////////////////////////////////测试中/////////////////////////////////////////////////////

        // 获取任务详情
        [HttpGet("{jobId}")]
        public async Task<AjaxResult> Get(long jobId)
        {
            var data = await _service.GetDtoAsync(jobId);
            return AjaxResult.Success(data);
        }


        //  任务列表 根据设备id查询
        [HttpGet("listByDeviceId/{deviceId}")]
        public async Task<AjaxResult> GetByDevice(long deviceId)
        {
            var list = await _service.GetListByDeviceId(deviceId);
            return AjaxResult.Success(list);
        }

        // 所有任务列表 根据产品id查询
        [HttpGet("listByProduct/{productId}")]
        public async Task<AjaxResult> GetByProduct(long productId)
        {
            var list = await _service.GetListByProductId(productId);
            return AjaxResult.Success(list);
        }


        /// <summary>
        /// 根据产品查询点位列表，返回前端 el-select 需要的数据格式
        /// </summary>
        /// <param name="productId">产品ID</param>
        [HttpGet("pointList/{productId}")]
        public async Task<List<ElSelect>> PointList(long productId)
        {
            var list = await _productPointService.GetCachedListAsync(productId);
            return list.Select(p => new ElSelect { Label = p.PointName ?? string.Empty,Value = p.Id.ToString() }).ToList();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////// ///////////////////////////////////////////////


        // 所有任务列表
        [HttpGet("list")]
        public async Task<SqlSugarPagedList<SysJobIotDto>> List([FromQuery] SysJobIotDto dto)
        {
            return await _service.GetDtoPagedListAsync(dto);
        }
 
        [HttpPost("add")]
        [Log(Title = "定时任务",BusinessType = BusinessType.INSERT)]
        public async Task<AjaxResult> Add([FromBody] SysJobIotDto dto)
        {
            dto.JobId = NextId.Id13();
            dto.InvokeTarget = "iotTask.readAndWrite()";  //定时调用的方法   测试使用
            dto.MisfirePolicy ??= ScheduleConstants.MISFIRE_DEFAULT; // 执行策略  默认不执行任务
            dto.Concurrent ??= "0"; //是否并发执行（0允许 1禁止）
            dto.Status ??= "1"; // 状态（0正常 1暂停）

            //通用字段
            dto.CreateBy = SecurityUtils.GetUsername() ?? "system";
            dto.CreateTime = DateTime.Now;
            dto.UpdateBy = dto.CreateBy;
            dto.UpdateTime = dto.CreateTime;


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

        [HttpPost("changeStatus")]
        [Log(Title = "定时任务",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> ChangeStatus([FromBody] SysJobIotDto dto)
        {
            var success = await _service.ChangeStatusAsync(dto);
            return success ? AjaxResult.Success() : AjaxResult.Error();
        }


        // 运行一次任务
        [HttpPost("run")]
        [Log(Title = "定时任务 运行一次",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> Run([FromBody] SysJobIotDto dto)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 运行一次任务开始");

            if(dto.JobId != 0)
            {
                var job = await _service.GetDtoAsync(dto.JobId);
                if(job?.DeviceId != null)
                {
                    var dev = await _deviceService.GetAsync(job.DeviceId.Value);
                    if(!string.Equals(dev.DeviceStatus,"online1",StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[WARN] 设备 {job.DeviceId} 不在线");
                        return AjaxResult.Error("设备不在线");
                    }
                }
            }


            var result = await _service.Run(dto);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 运行一次任务结束 -> {result}");

            return result ? AjaxResult.Success() : AjaxResult.Error("任务不存在或已过期！");
        }

 
        /// <summary>
        ///  任务启停：启动则任务按 Cron 表达式执行  
        /// </summary>
        /// <param name="dto">任务对象</param>
        [HttpPost("runCron")]
        [Log(Title = "定时任务启停",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> RunCron([FromBody] SysJobIotDto dto)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 运行Cron任务开始");
            //return AjaxResult.Success();

            var start = dto.Status == "1";
            dto.Status = start ? ScheduleStatus.NORMAL.GetValue() : ScheduleStatus.PAUSE.GetValue();
            var success = await _service.ChangeStatusAsync(dto);
            if(success && start)
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