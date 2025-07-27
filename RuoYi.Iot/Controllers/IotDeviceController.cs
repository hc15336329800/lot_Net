using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
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
    /// <summary>
    /// 物联网设备
    /// </summary>
    [ApiDescriptionSettings("Iot")]
    [Route("iot/device")]
    [AllowAnonymous] //匿名
    public class IotDeviceController : ControllerBase
    {
        private readonly ILogger<IotDeviceController> _logger;
        private readonly IotDeviceService _service;
        private readonly ITcpSender _tcpService;
        private readonly IotProductPointService _pointService;
        private readonly IotDeviceVariableService _variableService;


        public IotDeviceController(ILogger<IotDeviceController> logger,IotDeviceService service,ITcpSender tcpService,
      IotProductPointService pointService,IotDeviceVariableService variableService)
        {
            _logger = logger;
            _service = service;
            _tcpService = tcpService;
            _pointService = pointService;
            _variableService = variableService;

        }

        /// <summary>
        /// 主动向设备发送 Modbus RTU 读寄存器指令 (01 04 01F4 0002)
        /// 用于测试数据库写入功能
        /// </summary>
        [HttpGet("TestRead")]
        public async Task<AjaxResult> TestRead(long id)
        {

            if(id==0 )
            {
                id = 99100001250627L; //测试 
            }
             var device = await _service.GetDtoAsync(id);
           
            if(device.TcpHost == null || device.TcpPort == null)
            {
                return AjaxResult.Error("设备未配置 TCP 主机或端口");
            }

            byte slave = 0x01;
            byte func = 0x04;
            ushort startAddress = 0x01F4;
            ushort quantity = 0x0002;

            ConcurrentDictionary<byte,ushort>? lastDict = null;
            var svcType = Type.GetType("RuoYi.Tcp.Services.ModbusRtuService, RuoYi.Tcp"); //通过反射拿到 ModbusRtuService 这个类型的定义。
            if(svcType != null)
            {
                dynamic? svc = HttpContext.RequestServices.GetService(svcType);  //动态从容器中获取 ModbusRtuService 单例或实例对象。
                if(svc != null)
                {
                    try
                    {
                        lastDict = svc.LastReadStartAddrs as ConcurrentDictionary<byte,ushort>;
                    }
                    catch { }
                }
            }

            var frame = ModbusUtils.BuildReadFrame(slave,func,startAddress,quantity,lastDict);

            try
            {
                byte[]? resp = await _tcpService.SendAsync(id,frame,HttpContext.RequestAborted);


                if(resp == null)
                {
                    return AjaxResult.Error("无可用连接或发送失败");
                }

                // 反射获取 ModbusRtuService 的 ParseValue 私有静态方法
                var parseMethod = svcType?.GetMethod("ParseValue",global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static);

                // 获取产品点和变量映射
                var points = device.ProductId.HasValue ?
                    await _pointService.GetDtoListAsync(new IotProductPointDto { ProductId = device.ProductId,Status = "0",DelFlag = "0" }) :
                    new List<IotProductPointDto>();

                var pointMap = points
                    .Where(p => p.RegisterAddress.HasValue && p.SlaveAddress.HasValue)
                    .GroupBy(p => ((byte)p.SlaveAddress!.Value, (ushort)p.RegisterAddress!.Value))
                    .ToDictionary(g => g.Key,g => g.ToList());

                var varMap = await _variableService.GetVariableMapAsync(device.Id);

                var result = new Dictionary<string,string>();

                int byteCount = resp[2];
                var dataBytes = resp.Skip(3).Take(byteCount).ToArray();
                ushort realStart = startAddress;
                lastDict?.TryGetValue(slave,out realStart);

                for(int i = 0; i < byteCount / 2; i++)
                {
                    ushort addr = (ushort)(realStart + i);
                    var key = ((byte)slave, addr);
                    if(pointMap.TryGetValue(key,out var plist))
                    {
                        foreach(var p in plist)
                        {
                            if(p.PointKey != null && varMap.TryGetValue(p.PointKey,out var v) && v.VariableId.HasValue)
                            {
                                var regBytes = dataBytes.Skip(i * 2).Take(2).ToArray();
                                var valueObj = parseMethod != null ? parseMethod.Invoke(null,new object?[] { regBytes,p.DataType,p.ByteOrder,p.Signed ?? false }) : null;
                                var value = valueObj?.ToString() ?? BitConverter.ToString(regBytes);
                                await _variableService.SaveValueAsync(device.Id,v.VariableId.Value,p.PointKey,value);
                                result[p.PointKey] = value;
                            }
                        }
                    }
                }

                return AjaxResult.Success(result);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,"Modbus 读指令发送失败");
                return AjaxResult.Error("发送失败",ex.Message);
            }
        }



 
    

        [HttpGet("infobyid/{id}")]
        public async Task<AjaxResult> Get(long id)
        {
            var data = await _service.GetDtoAsync(id);
            return AjaxResult.Success(data);
        }


        [HttpPost("add")]
        [Log(Title = "设备",BusinessType = BusinessType.INSERT)]
        public async Task<AjaxResult> Add([FromBody] IotDeviceDto dto)
        {
            dto.Id = NextId.Id13(); //必须使用这个生成id
            dto.Status = "0";
            dto.DelFlag = "0";
            var ok = await _service.InsertAsync(dto);
            return AjaxResult.Success(ok);
        }

        [HttpPost("edit")]
        [Log(Title = "设备",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> Edit([FromBody] IotDeviceDto dto)
        {
            var data = await _service.UpdateAsync(dto);
            return AjaxResult.Success(data);
        }


        [HttpPost("delete")]
        [Log(Title = "设备",BusinessType = BusinessType.DELETE)]
        public async Task<AjaxResult> Delete([FromBody] long[] ids)
        {
            var data = await _service.DeleteAsync(ids);
            return AjaxResult.Success(data);
        }






    }







}
