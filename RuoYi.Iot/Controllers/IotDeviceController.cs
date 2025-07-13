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
    public class IotDeviceController : ControllerBase
    {
        private readonly ILogger<IotDeviceController> _logger;
        private readonly IotDeviceService _service;

        public IotDeviceController(ILogger<IotDeviceController> logger,IotDeviceService service)
        {
            _logger = logger;
            _service = service;
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
            var svcType = Type.GetType("RuoYi.Tcp.Services.ModbusRtuService, RuoYi.Tcp");
            if(svcType != null)
            {
                dynamic? svc = HttpContext.RequestServices.GetService(svcType);
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
                using var client = new TcpClient();
                await client.ConnectAsync(device.TcpHost,device.TcpPort.Value);
                var stream = client.GetStream();
                await stream.WriteAsync(frame,0,frame.Length);
                var buffer = new byte[256];
                var len = await stream.ReadAsync(buffer,0,buffer.Length);
                var resp = buffer.Take(len).ToArray();
                var hex = BitConverter.ToString(resp).Replace("-"," ");
                return AjaxResult.Success(hex);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,"Modbus 读指令发送失败");
                return AjaxResult.Error("发送失败",ex.Message);
            }
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
            dto.AutoRegPacket = _service.BuildAutoRegPacket(dto);
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
