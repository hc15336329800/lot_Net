using Microsoft.Extensions.Logging;
using RuoYi.Common.Enums;
using RuoYi.Framework;
using RuoYi.Framework.Extensions;
using RuoYi.Framework.Utils;
using RuoYi.Data.Dtos;
using Microsoft.AspNetCore.Mvc;
using RuoYi.Zk.AC.Dao;
using RuoYi.Zk.AC.Services;
using Microsoft.AspNetCore.Authorization;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using RuoYi.Zk.AC.Model.Entities;
using RuoYi.Zk.AC.Model.Dto;
 

namespace RuoYi.Zk.AC.Controllers
{
    /// <summary>
    /// 空调信息
    /// </summary>
    [ApiDescriptionSettings("Zk")]
    [Route("zk/acAirConditioners")]
    public class AcAirConditionersController : ControllerBase
    {
        private readonly ILogger<AcAirConditionersController> _logger;
        private readonly AcAirConditionersService _acAirConditionersService;

 



        public AcAirConditionersController(ILogger<AcAirConditionersController> logger,
            AcAirConditionersService acAirConditionersService)
        {
            _logger = logger;
            _acAirConditionersService = acAirConditionersService;
        }


        // —— 新增这两行   随机树—— 

        //private：表示这个字段只能在当前类里访问。
        //static：表示对整个类只有一份，不管这个控制器被实例化多少次，大家都共用同一个 _randLock。
        //readonly：初始化之后就不能再赋值，保证它在程序运行期间始终指向同一个对象。
        //创建了一个“空壳”对象实例，用来作为 lock(_randLock){ … } 的锁标记。
        //简而言之，_randLock 就是给 Random 实例上锁用的“锁钥匙”，保证多线程环境下安全地取随机数。!!
        private static readonly object _randLock = new object();
        private static readonly Random _rnd = new Random();  //随机值



        //============================================TCP小车============================================



        /// <summary>
        /// 获取所有已注册并上报过位置的小车状态
        /// “小车与导轨不绑定”版本
        /// </summary>
        [HttpGet("sensorCars")]
        public Task<AjaxResult> GetSensorCars( )
        {
            // 拿到两个全局字典
            var posDict = SensorDataListenerService.SensorPositions;
            var railDict = SensorDataListenerService.SensorRails;

            // 扁平化每辆车
            var list = posDict.Select(kvp =>
            {
                var id = kvp.Key;             // 4位ID,如 "0001"
                var posIndex = kvp.Value;           // 1..5
                                                    // 如果没上报过rail，则默认1


                //设置轨道
                railDict.TryGetValue(id,out int rail);
                    const int maxRails = 4;                     
                 rail = Math.Clamp(rail,1,maxRails);

                //// 计算偏移百分比   
                //const int carsPerRail = 5;
                //double offset = (posIndex - 1) / (double)(carsPerRail - 1) * 100;

                //// 计算 y 坐标（10,50,90）
                //const double minY = 10, maxY = 90;
                //double stepY = (maxY - minY) / (3 - 1);
                //double y = minY + (rail - 1) * stepY;

                return new
                {
                    id,
                    name = $"小车 {id}",
                    rail,
                    posIndex,
                    //offset,  //扔给前端计算
                    //y
                };
            })
            .ToList();

            return Task.FromResult(AjaxResult.Success(list));
        }


        /// <summary>
        /// 获取实时小车位置（基于 SensorDataListenerService 中最新上报的数据）
        /// 版本： 小策绑定导轨
        /// </summary>
        [HttpGet("sensorCarsV2")]
        public Task<AjaxResult> GetSensorCarsV2( )
        {
            const int railCount = 3;
            const int carsPerRail = 5;
            const double minY = 10;
            const double maxY = 90;
            // 计算三条轨道在 y 轴上的等分位置
            double stepY = (maxY - minY) / Math.Max(railCount - 1,1);

            var rails = new List<object>();
            for(int i = 0; i < railCount; i++)
            {
                double y = minY + stepY * i;
                var cars = new List<object>();

                for(int j = 0; j < carsPerRail; j++)
                {
                    // sensorId 与前面注册包映射一致
                    var sensorId = $"rail{i + 1}-car{j + 1}";

                    // 取最新上报的定位点索引，默认 1
                    SensorDataListenerService.SensorPositions
                        .TryGetValue(sensorId,out int posIndex);
                    posIndex = Math.Clamp(posIndex,1,carsPerRail);

                    // 把 1–5 的定位点映射到 0–100 百分比
                    double offset = (posIndex - 1) / (double)(carsPerRail - 1) * 100;

                    cars.Add(new
                    {
                        id = sensorId,
                        name = $"小车 {i + 1}-{j + 1}",
                        offset
                    });
                }

                rails.Add(new
                {
                    id = $"rail{i + 1}",
                    y,
                    cars
                });
            }

            return Task.FromResult(AjaxResult.Success(rails));
        }

        // ============================================ 实时小车接口（基于传感器上报） ============================================

        /// <summary>
        /// 获取实时小车位置（基于 SensorDataListenerService 中最新上报的数据）
        /// </summary>
        [HttpGet("sensorCars01")]
        public Task<AjaxResult> GetSensorCars01( )
        {
            const int railCount = 3;
            const int carsPerRail = 5;
            const double minY = 10;
            const double maxY = 90;
            // 计算三条轨道在 y 轴上的等分位置
            var stepY = (maxY - minY) / Math.Max(railCount - 1,1);

            var rails = new List<object>();
            for(int i = 0; i < railCount; i++)
            {
                double y = minY + stepY * i;
                var cars = new List<object>();
                for(int j = 0; j < carsPerRail; j++)
                {
                    // 对应的 sensorId，与后台 TCP 服务注册包映射保持一致
                    var sensorId = $"rail{i + 1}-car{j + 1}";
                    // 从后台服务获取最新的定位点索引，未上报则默认为 1
                    SensorDataListenerService.SensorPositions.TryGetValue(sensorId,out int posIndex);
                    posIndex = Math.Clamp(posIndex,1,carsPerRail);
                    // 将 1–5 的定位点线性映射到 0–100 的偏移百分比
                    double offset = (posIndex - 1) / (double)(carsPerRail - 1) * 100;

                    cars.Add(new
                    {
                        id = sensorId,
                        name = $"小车 {i + 1}-{j + 1}",
                        offset
                    });
                }
                rails.Add(new
                {
                    id = $"rail{i + 1}",
                    y,
                    cars
                });
            }

            // 直接返回，不走服务层
            return Task.FromResult(AjaxResult.Success(rails));
        }



        //============================================模拟小车============================================



        /// <summary>
        /// 模拟返回轨道及其小车数据（动态随机 offset，便于测试）
        /// </summary>
        [HttpGet("cars")]
        public Task<AjaxResult> GetCars( )
        {
            const int railCount = 3;
            const int carsPerRail = 3;
            const double minY = 10;
            const double maxY = 90;
            var stepY = (maxY - minY) / Math.Max(railCount - 1,1);

            var rails = new List<object>();
            for(int i = 0; i < railCount; i++)
            {
                double y = minY + stepY * i;
                var cars = new List<object>();
                for(int j = 0; j < carsPerRail; j++)
                {
                    double offset;
                    lock(_randLock) //锁对象，在取随机数时用:来确保同一时间只有一个线程能进入这段代码，避免并发冲突。
                    {
                        // 每次从同一个 Random 实例里取新值
                        offset = _rnd.NextDouble() * 100;
                    }
                    cars.Add(new
                    {
                        id = $"rail{i + 1}-car{j + 1}",
                        name = $"小车 {i + 1}-{j + 1}",
                        offset
                    });
                }

                rails.Add(new
                {
                    id = $"rail{i + 1}",
                    y,
                    cars
                });
            }

            return Task.FromResult(AjaxResult.Success(rails));  // 直接返回，不走服务层
        }
   



        /// <summary>
        /// 模拟返回轨道及其小车数据  固定值
        /// </summary>
        [HttpGet("cars10")]
        public Task<AjaxResult> GetCars10( )
        {
            // 模拟 3 条轨道，每条 3 辆小车
            var rnd = new Random();
            const int railCount = 3;
            const int carsPerRail = 3;
            const double minY = 10;
            const double maxY = 90;
            var stepY = (maxY - minY) / (railCount - 1);

            var rails = new List<object>();
            for(int i = 0; i < railCount; i++)
            {
                double y = minY + stepY * i;
                var cars = new List<object>();
                for(int j = 0; j < carsPerRail; j++)
                {
                    cars.Add(new
                    {
                        id = $"rail{i + 1}-car{j + 1}",
                        name = $"小车 {i + 1}-{j + 1}",
                        offset = rnd.NextDouble() * 100
                    });
                }

                rails.Add(new
                {
                    id = $"rail{i + 1}",
                    y,
                    cars
                });
            }

            return Task.FromResult(AjaxResult.Success(rails));
        }




        //================================================================================================






        /// <summary>
        /// 查询空调信息列表
        /// </summary>
        [HttpGet("list")]
        //[AppAuthorize("system:conditioners:list")]
        //[DataRange(DeptAlias = "d")]
        //[DataScope(DeptAlias = "d")]
        public async Task<SqlSugarPagedList<AcAirConditionersDto>> GetAcAirConditionersPagedList([FromQuery] AcAirConditionersDto dto)
        {


            // GetAcAirConditionersPagedList001
            //Task<SqlSugarPagedList<AcAirConditioners>> result = _acAirConditionersService.GetAcAirConditionersPagedList001(dto);



           var result = await _acAirConditionersService.GetAcAirConditionersPagedList(dto);


            // 这里可以在调试时查看 result 的值
            return   result;
        }

        /// <summary>
        /// 获取 空调信息 详细信息
        /// </summary>
        [HttpGet("")]
        [HttpGet("{id}")]
        [AppAuthorize("system:conditioners:query")]
        public async Task<AjaxResult> Get(int id)
        {
            var data = await _acAirConditionersService.GetDtoAsync(id);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 新增 空调信息
        /// </summary>
        [HttpPost("")]
        [AppAuthorize("system:conditioners:add")]
        [TypeFilter(typeof(Framework.DataValidation.DataValidationFilter))]
        [RuoYi.System.Log(Title = "空调信息", BusinessType = BusinessType.INSERT)]
        public async Task<AjaxResult> Add([FromBody] AcAirConditionersDto dto)
        {
            var data = await _acAirConditionersService.InsertAsync(dto);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 修改 空调信息
        /// </summary>
        [HttpPut("")]
        [AppAuthorize("system:conditioners:edit")]
        [TypeFilter(typeof(Framework.DataValidation.DataValidationFilter))]
        [RuoYi.System.Log(Title = "空调信息", BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> Edit([FromBody] AcAirConditionersDto dto)
        {
            var data = await _acAirConditionersService.UpdateAsync(dto);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 删除 空调信息
        /// </summary>
        [HttpDelete("{ids}")]
        [AppAuthorize("system:conditioners:remove")]
        [RuoYi.System.Log(Title = "空调信息", BusinessType = BusinessType.DELETE)]
        public async Task<AjaxResult> Remove(string ids)
        {
            var idList = ids.SplitToList<long>();
            var data = await _acAirConditionersService.DeleteAsync(idList);
            return AjaxResult.Success(data);
        }
 
 
    }
}