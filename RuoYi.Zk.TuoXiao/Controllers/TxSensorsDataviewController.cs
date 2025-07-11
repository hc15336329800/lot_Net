using Microsoft.Extensions.Logging;
using RuoYi.Common.Enums;
using RuoYi.Framework;
using RuoYi.Framework.Extensions;
using RuoYi.Framework.Utils;
using RuoYi.Data.Dtos;
using RuoYi.System.Services;
using RuoYi.System;
using RuoYi.Zk.TuoXiao.Dtos;
using Microsoft.AspNetCore.Mvc;
using RuoYi.Zk.TuoXiao.Services;
using RuoYi.Common.Utils;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SqlSugar;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;


/// 备用： modbus Rtu 通讯协议  .

namespace RuoYi.Zk.TuoXiao.Controllers
{
    /// <summary>
    /// 工厂设备_传感器大屏数据
    /// </summary>
    [Route("Zk/TxSensorsDataview")]
    [AllowAnonymous]
    public class TxSensorsDataviewController : ControllerBase
    {
        private readonly ILogger<TxSensorsDataviewController> _logger;
        private readonly TxSensorsDataviewService _txSensorsDataviewService;
        private readonly IConnectionMultiplexer _redis;

        private readonly HttpRequestService _httpRequestService;// 使用url 实现模块间通信


        public TxSensorsDataviewController(ILogger<TxSensorsDataviewController> logger,
            TxSensorsDataviewService txSensorsDataviewService, IConnectionMultiplexer redis, HttpRequestService httpRequestService)
        {
            _logger = logger;
            _txSensorsDataviewService = txSensorsDataviewService;
            _redis = redis;
            _httpRequestService = httpRequestService;
        }




        ///////////////////////////////////////表的基础操作////////////////////////////////////////////////////////////////////////////



        // 连接服务器 - 请求模型类
        public class MqttConnectionRequest
        {
            public string Host { get; set; } = "106.52.194.56";
            public int Port { get; set; } = 1883;
            public string Topic { get; set; } = "zktx/siemens/plc01/TxSensorsDataview";
            public string Message { get; set; } = "0001";
        }


        //  请求模型类
        public class SendMqttToPlcRequest
        {
            public string Type { get; set; } = "0";  //0电加热   1振打电机  2下料器  3罗茨风机
            public string Start { get; set; } = "0";  //0停止  1启动
            public string Mode { get; set; } = "0";  //0手动  1自动
            public string Var1 { get; set; } = "0"; //上限
            public string Var2 { get; set; } = "0"; //下限
            public string Topic { get; set; } = "zktx/siemens/plc01/TxSensorsDataview"; // 默认主题
        }


        /// <summary>
        /// 连接服务器  注意：client.BaseAddress = new Uri("http://172.18.182.33:5000/"); // 替换为实际服务地址
        /// </summary>
        /// <param name="request"> mqtt连接字符</param>
        /// <returns></returns>
        [HttpPost("mqttConnectServer")]
        public async Task<ActionResult<string>> mqttConnectServer([FromBody] MqttConnectionRequest request)
        {
            // 如果 request 为 null，使用默认参数
            request ??= new MqttConnectionRequest
            {
                Host = "106.52.194.56",
                Port = 1883
            };

            // 目标服务的 URL
            string targetUrl = "/mqtt/mqttConn/connect"; // 替换为实际的目标服务接口路径


            // 调用 HttpRequestService 的 PostAsync 并获取返回结果
            var (isSuccess, data, errorMessage) = await _httpRequestService.PostAsync(targetUrl, request);

            // 根据请求结果返回响应
            if (!isSuccess)
            {
                return BadRequest(new
                {
                    message = errorMessage
                });
            }

            return Ok(new { message = "请求成功", data });
        }


        /// <summary>
        /// 订阅一个主题
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("mqttSubscribeServer")]
        public async Task<ActionResult<string>> mqttSubscribeServer([FromBody] MqttConnectionRequest request)
        {
            // 如果 request 为 null，使用默认参数
            request ??= new MqttConnectionRequest
            {
                Topic = "zktx/siemens/plc01/TxSensorsDataview" // 设置默认的主题
            };

            // 目标服务的 URL
            string targetUrl = "/mqtt/mqttConn/subscribe";


            // 调用 HttpRequestService 的 PostAsync 并获取返回结果
            var (isSuccess, data, errorMessage) = await _httpRequestService.PostAsync(targetUrl, request);

            // 根据请求结果返回响应
            if (!isSuccess)
            {
                return BadRequest(new { message = errorMessage });
            }

            return Ok(new { message = "请求成功", data });
        }


        /// <summary>
        /// 发布一个主题
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("mqttPublishServer")]
        public async Task<ActionResult<string>> mqttPublishServer([FromBody] MqttConnectionRequest request)
        {
            // 如果 request 为 null，使用默认参数
            request ??= new MqttConnectionRequest
            {
                Topic = "zktx/siemens/plc01/TxSensorsDataview",// 设置默认的主题
                Message = "33.3"
            };

            // 目标服务的 URL
            string targetUrl = "/mqtt/mqttConn/publish";

            // 调用 HttpRequestService 的 PostAsync 并获取返回结果
            var (isSuccess, data, errorMessage) = await _httpRequestService.PostAsync(targetUrl, request);

            // 根据请求结果返回响应
            if (!isSuccess)
            {
                return BadRequest(new { message = errorMessage });
            }

            return Ok(new { message = "请求成功", data });
        }





        /// <summary>
        /// 发布控制指令  √  
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("sendMqttToPlc")]
        public async Task<AjaxResult> SendMqttToPlc([FromBody] SendMqttToPlcRequest request)
        {
            // 如果 request 为 null，使用默认参数
            request ??= new SendMqttToPlcRequest
            {
                Topic = "zktx/siemens/plc01/TxSensorsDataview" // 设置默认的主题
            };

            // 定义地址映射
            var addressMapping = new Dictionary<string, Dictionary<string, string>>
        {
            { "0", new Dictionary<string, string> { { "Start", "40119" }, { "Mode", "40121" }, { "Var1", "40123" }, { "Var2", "40125" } } },
            { "1", new Dictionary<string, string> { { "Start", "40127" }, { "Mode", "40129" }, { "Var1", "40131" }, { "Var2", "40133" } } },
            { "2", new Dictionary<string, string> { { "Start", "40135" }, { "Mode", "40137" }, { "Var1", "40139" }, { "Var2", "40141" } } },
            { "3", new Dictionary<string, string> { { "Start", "40143" }, { "Mode", "40145" }, { "Var1", "40147" }, { "Var2", "40149" } } }
        };

            // 确定Type是否在地址映射中存在
            if (!addressMapping.TryGetValue(request.Type, out var addressForType))
            {

                return AjaxResult.Error("无效的Type参数。");

                //return BadRequest(new { message = "无效的Type参数" });

            }

            // 初始化指令列表
            var modbusCommands = new List<string>();

            // 生成写入指令的辅助函数

            // 生成写入指令的辅助函数  浮点型需要写入两个寄存器！！
            string GenerateModbusCommand(string address, string value)
            {
                var addressInt = Convert.ToInt32(address);

                // 尝试将字符串转换为浮点型
                if (float.TryParse(value, out float floatValue))
                {
                    // 将浮点型转换为 IEEE 754 格式的字节数组
                    byte[] floatBytes = BitConverter.GetBytes(floatValue); 

                    // 根据 Modbus 大小端要求调整字节顺序
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(floatBytes);
                    }

                    // 生成一次性写入两个寄存器的 Modbus 指令
                    var addressHex = addressInt.ToString("X4");
                    var byteCount = "04"; // 两个寄存器共 4 字节
                    var valueHex = BitConverter.ToString(floatBytes).Replace("-", ""); // 将字节数组转换为十六进制字符串

                    // 生成指令：功能码 0x10，地址，寄存器数量，字节数，数据
                    //return $"01 10 {addressHex} 0002 {byteCount} {valueHex}";


                    // 生成指令：功能码 0x10，地址，寄存器数量，字节数，数据
                    var commandWithoutCRC = $"01 10 {addressHex} 0002 {byteCount} {valueHex}";

                    // 将指令字符串转换为字节数组  报错
                    //var commandBytes = commandWithoutCRC.Split(' ')
                    //                                    .Select(hex => Convert.ToByte(hex, 16))
                    //                                    .ToArray();


                    // 将指令字符串转换为字节数组
                    var commandBytes = new List<byte>();
                    foreach (var hex in commandWithoutCRC.Split(' '))
                    {
                        // 检查字符串的长度，并逐个字节解析
                        for (int i = 0; i < hex.Length; i += 2)
                        {
                            string byteString = hex.Substring(i, 2); // 每次取两个字符
                            if (byte.TryParse(byteString, NumberStyles.HexNumber, null, out byte byteValue))
                            {
                                commandBytes.Add(byteValue);
                            }
                            else
                            {
                                throw new FormatException($"Invalid byte value: {byteString}");
                            }
                        }
                    }



                    // 计算 CRC16 校验码
                    byte[] crcBytes = CalculateCRC16(commandBytes.ToArray());

                    // 将 CRC 校验码附加到指令的末尾
                    commandBytes.AddRange(crcBytes);

                    // 将完整指令转换为十六进制字符串并返回
                    return BitConverter.ToString(commandBytes.ToArray()).Replace("-", " ");
                }
                else
                {
                    throw new FormatException("Invalid format for value; expected a floating-point number.");
                }
            }

            // 检查各个参数，生成相应的 Modbus 指令
            if (request.Start != "0")
            {
                var command = GenerateModbusCommand(addressForType["Start"], request.Start);
                modbusCommands.Add(command);
            }
            if (request.Mode != "0")
            {
                var command = GenerateModbusCommand(addressForType["Mode"], request.Mode);
                modbusCommands.Add(command);
            }
            if (request.Var1 != "0")
            {
                var command = GenerateModbusCommand(addressForType["Var1"], request.Var1);
                modbusCommands.Add(command);
            }
            if (request.Var2 != "0")
            {
                var command = GenerateModbusCommand(addressForType["Var2"], request.Var2);
                modbusCommands.Add(command);
            }

            // 如果没有生成任何指令，返回
            if (modbusCommands.Count == 0)
            {
                return AjaxResult.Error("无效的请求，所有参数均为0。");

                // return BadRequest(new { message = "无效的请求，所有参数均为0" });
            }

            // 将指令发送给目标服务
            string targetUrl = "/mqtt/mqttConn/publish";

            foreach (var command in modbusCommands)
            {
                var mqttRequest = new MqttConnectionRequest
                {
                    Topic = request.Topic,
                    Message = command
                };

                var (isSuccess, data, errorMessage) = await _httpRequestService.PostAsync(targetUrl, mqttRequest);

                if (!isSuccess)
                {
                    return AjaxResult.Error("发送指令失败。", errorMessage);

                    // return BadRequest(new { message = $"发送指令失败: {errorMessage}" });
                }
                // 添加延迟，例如 500 毫秒
                await Task.Delay(500);
            }

            return AjaxResult.Success("请求成功，指令已发送");

            //return Ok(new { message = "请求成功，指令已发送" });
        }







        /// <summary>
        /// 获取 Redis 中的最新一条传感器数据
        /// </summary>
        [HttpGet("getInfoForRadis")]
        public async Task<ActionResult<TxSensorsDataviewDto007>> GetLatestSensorData()
        {
            var db = _redis.GetDatabase();
            string cacheKey = "tx_sensors_data";

            // 获取 Redis 列表中的最新一条数据,读取后就没了
            //var latestData = await db.ListLeftPopAsync(cacheKey); 

            // 获取 Redis 列表中的最新一条数据，不删除
            var latestData = await db.ListGetByIndexAsync(cacheKey, 0);
            if (latestData.IsNullOrEmpty)
            {
                _logger.LogWarning("Redis 中没有找到传感器数据");
                return NotFound("Redis 中没有找到传感器数据");
            }

            // 反序列化为 TxSensorsDataviewDto 对象
            var dto = JsonSerializer.Deserialize<TxSensorsDataviewDto007>(latestData);
            return Ok(dto);
        }




        ///////////////////////////////////////表的基础操作////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 查询工厂设备_传感器大屏数据列表
        /// </summary>
        [HttpGet("list")]
        public async Task<SqlSugarPagedList<TxSensorsDataviewDto007>> GetTxSensorsDataviewPagedList([FromQuery] TxSensorsDataviewDto007 dto)
        {
            return await _txSensorsDataviewService.GetDtoPagedListAsync(dto);
        }

        /// <summary>
        /// 获取 工厂设备_传感器大屏数据 详细信息
        /// </summary>
        [HttpGet("")]
        [HttpGet("{id}")]
        public async Task<AjaxResult> Get(long id)
        {
            var data = await _txSensorsDataviewService.GetDtoAsync(id);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 新增 工厂设备_传感器大屏数据
        /// </summary>
        [HttpPost("")]
        [TypeFilter(typeof(Framework.DataValidation.DataValidationFilter))]
        [Log(Title = "工厂设备_传感器大屏数据", BusinessType = BusinessType.INSERT)]
        public async Task<AjaxResult> Add([FromBody] TxSensorsDataviewDto007 dto)
        {
            var data = await _txSensorsDataviewService.InsertAsync(dto);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 修改 工厂设备_传感器大屏数据
        /// </summary>
        [HttpPut("")]
        [TypeFilter(typeof(Framework.DataValidation.DataValidationFilter))]
        [Log(Title = "工厂设备_传感器大屏数据", BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> Edit([FromBody] TxSensorsDataviewDto007 dto)
        {
            var data = await _txSensorsDataviewService.UpdateAsync(dto);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 删除 工厂设备_传感器大屏数据
        /// </summary>
        [HttpDelete("{ids}")]
        [Log(Title = "工厂设备_传感器大屏数据", BusinessType = BusinessType.DELETE)]
        public async Task<AjaxResult> Remove(string ids)
        {
            var idList = ids.SplitToList<long>();
            var data = await _txSensorsDataviewService.DeleteAsync(idList);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 导入 工厂设备_传感器大屏数据
        /// </summary>
        [HttpPost("import")]
        [Log(Title = "工厂设备_传感器大屏数据", BusinessType = BusinessType.IMPORT)]
        public async Task Import([Required] IFormFile file)
        {
            var stream = new MemoryStream();
            file.CopyTo(stream);
            var list = await ExcelUtils.ImportAsync<TxSensorsDataviewDto007>(stream);
            await _txSensorsDataviewService.ImportDtoBatchAsync(list);
        }

        /// <summary>
        /// 导出 工厂设备_传感器大屏数据
        /// </summary>
        [HttpPost("export")]
        [Log(Title = "工厂设备_传感器大屏数据", BusinessType = BusinessType.EXPORT)]
        public async Task Export(TxSensorsDataviewDto007 dto)
        {
            var list = await _txSensorsDataviewService.GetDtoListAsync(dto);
            await ExcelUtils.ExportAsync(App.HttpContext.Response, list);
        }





        /// <summary>
        /// 自定义 CRC16 算法实现
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] CalculateCRC16(byte[] data)
        {
            ushort crc = 0xFFFF;

            foreach (byte b in data)
            {
                crc ^= b;

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 1) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return BitConverter.GetBytes(crc);
        }
    }
}