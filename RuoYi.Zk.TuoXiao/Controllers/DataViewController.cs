using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;
 using RuoYi.Data;
using RuoYi.Data.Dtos;
using RuoYi.Data.Entities;
using RuoYi.Data.Models;
using RuoYi.Framework;
using RuoYi.Framework.Cache;
using RuoYi.Mqtt.Model;
using RuoYi.Mqtt.Services;
using RuoYi.System.Controllers;
using RuoYi.Zk.TuoXiao.Dtos;
using RuoYi.Zk.TuoXiao.Entities;
using RuoYi.Zk.TuoXiao.Model.MqttModel;
using RuoYi.Zk.TuoXiao.Services;
using StackExchange.Redis;
 

/// 有人网关 mqtt 服务接口 。 使用中。
namespace RuoYi.Zk.TuoXiao.Controllers
{

    //[ApiDescriptionSettings("TuoXiao")]
    [Route("Zk/TuoXiao")]
    public class DataViewController : ControllerBase
    {
        private readonly ILogger<SysUserController> _logger;
        private readonly IMqttService _mqttService; // MQTT 服务接口, 服务注入，就不要要引入项目了
        private readonly TxSensorsDataviewService _txSensorsDataviewService;

        private readonly ICache _cache;

        public DataViewController(ICache cache,ILogger<SysUserController> logger,IMqttService mqttService,TxSensorsDataviewService txSensorsDataviewService)
        {
            _cache = cache;
            _logger = logger;
            _mqttService = mqttService;
            _txSensorsDataviewService = txSensorsDataviewService;
        }


        /// <summary>
        /// 测试
        /// </summary>
        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserList([FromQuery] SysUserDto dto)
        {

            // 构建要返回的 JSON 数据
            var jsonResult = new
            {
                name = "zhangsan",
                sex = "男",
            };

            // 返回 JSON 结果
            return new JsonResult(jsonResult);
        }

        // 缓存目录构建 -测试
        private string GetCacheKey(string configKey)
        {
            return CacheConstants.TX_MQTT_Message + configKey;
        }

        // 缓存目录构建 -测试 -写入成功
        [HttpGet("Setradis")]
        [AllowAnonymous]
        public void Setradis( )
        {

            //MqttWriteData sc = new MqttWriteData();
            //sc.Name = "name666";
            //sc.Value = "value666";
            // 使用命名空间存储令牌
            //var tokenKey = GetNamespaceKey("tokens666","user123");
            //_cache.Set(tokenKey,sc,30); // 存储到 tokens/user123 键中

            // 写入成功
            _cache.SetString(GetCacheKey("T6666"),"T6666value");

        }

        // 缓存目录构建 -测试 -读取成功
        [HttpGet("Getradis")]
        [AllowAnonymous]
        public string Getradis( )
        {

            string? configValue = _cache.GetString(GetCacheKey("T6666"));

            return configValue;

        }
        //////////////////////////////////////////////////////////连接服务器 （必须调用）√ //////////////////////////////////////////////////////////////////////


        /// <summary>
        /// 连接到 MQTT 服务器
        /// </summary>
        [HttpGet("connect")]
        [AllowAnonymous]
        public async Task<AjaxResult> MqttConnect( )
        {
            try
            {
                // 替换为实际的 MQTT Broker 参数
                string brokerHost = "47.113.186.237";//"106.52.194.56";
                int brokerPort = 1883;
                string username = "admin";
                string password = "admin";
                string clientId = "ry_net_tuoxiao1"; //可以省略会自动生成

                // 调用 MQTT 服务的连接方法
                await _mqttService.ConnectToMqttServerAsync(brokerHost,brokerPort,username,password,clientId);
                _logger.LogInformation("成功连接到 MQTT 服务器");

                // 更新连接状态
                if(_mqttService.IsConnected)
                {
                    _logger.LogInformation("成功连接到 MQTT 服务器");
                }
                else
                {
                    _logger.LogWarning("连接到 MQTT 服务器后状态仍未更新为已连接");
                }


                // 调用订阅主动上报主题的方法
                var autoDataResult = await MqttSubscribeAutoData();
                if((int)autoDataResult[AjaxResult.CODE_TAG] != 200)
                {
                    _logger.LogWarning($"订阅主动上报主题失败: {autoDataResult[AjaxResult.MSG_TAG]}");
                }
                else
                {
                    _logger.LogInformation($"订阅主动上报主题成功: {autoDataResult[AjaxResult.MSG_TAG]}");
                }

                // 调用订阅回复主题的方法
                var replyTopicResult = await MqttSubscribeReplyTopic();
                if((int)autoDataResult[AjaxResult.CODE_TAG] != 200)
                {
                    _logger.LogWarning($"订阅回复主题失败: {autoDataResult[AjaxResult.MSG_TAG]}");
                }
                else
                {
                    _logger.LogInformation($"订阅回复主题成功: {autoDataResult[AjaxResult.MSG_TAG]}");
                }


                return AjaxResult.Success("成功连接到 MQTT 服务器并完成主题订阅");
            }
            catch(Exception ex)
            {
                _logger.LogError($"连接到 MQTT 服务器失败: {ex.Message}");
                return AjaxResult.Error("连接到 MQTT 服务器失败",ex.Message);
            }
        }


        //////////////////////////////////////////////////////订阅有人主动上报主题（必须调用） √ //////////////////////////////////////////////////////////////////////



        /// <summary>
        /// 订阅主题（SubscribeAutoData） -- 有人主动上报主题 
        /// </summary>
        /// <param name="topic">ReplyTopicAutoData</param>
        /// <returns></returns>
        [HttpGet("SubscribeAutoData")]
        [AllowAnonymous]
        public async Task<AjaxResult> MqttSubscribeAutoData( )
        {
            // 有人主动上报主题
            try
            {
              

                // 如果传入的 topic 参数为空，设置默认值为 "ReplyTopicAutoData"
                var topic = "ReplyTopicAutoData";

                // 调用 MQTT 服务的订阅方法
                await _mqttService.SubscribeToTopicAsync(topic,async (receivedTopic,message) =>
                {
                    // 记录接收到的消息
                    _logger.LogInformation($"从主题 {receivedTopic} 接收到消息: {message}");

                    // 反序列化为字典以便处理类型不匹配问题
                    var tempData = JsonSerializer.Deserialize<Dictionary<string,object>>(message);
                    if(tempData != null)
                    {

                        // 接收的数值转为字符串
                        TxSensorsDataviewDto data = new TxSensorsDataviewDto
                        {
                            SiloTemp1 = tempData.ContainsKey("SiloTemp1") ? tempData["SiloTemp1"].ToString() : null,
                            SiloTemp2 = tempData.ContainsKey("SiloTemp2") ? tempData["SiloTemp2"].ToString() : null,
                            FanTemp = tempData.ContainsKey("FanTemp") ? tempData["FanTemp"].ToString() : null,
                            OxygenContent = tempData.ContainsKey("OxygenContent") ? tempData["OxygenContent"].ToString() : null,
                            BoilerLoad = tempData.ContainsKey("BoilerLoad") ? tempData["BoilerLoad"].ToString() : null,
                            Nox = tempData.ContainsKey("Nox") ? tempData["Nox"].ToString() : null,
                            InletPressure = tempData.ContainsKey("InletPressure") ? tempData["InletPressure"].ToString() : null,
                            OutletPressure = tempData.ContainsKey("OutletPressure") ? tempData["OutletPressure"].ToString() : null,
                            Weight = tempData.ContainsKey("Weight") ? tempData["Weight"].ToString() : null,
                            ElectricHeatingTemp = tempData.ContainsKey("ElectricHeatingTemp") ? tempData["ElectricHeatingTemp"].ToString() : null,
                            HeaterStart = tempData.ContainsKey("HeaterStart") ? tempData["HeaterStart"].ToString() : null,
                            HeaterMode = tempData.ContainsKey("HeaterMode") ? tempData["HeaterMode"].ToString() : null,
                            HeaterTempMax = tempData.ContainsKey("HeaterTempMax") ? tempData["HeaterTempMax"].ToString() : null,
                            HeaterTempMin = tempData.ContainsKey("HeaterTempMin") ? tempData["HeaterTempMin"].ToString() : null,
                            VibratorStart = tempData.ContainsKey("VibratorStart") ? tempData["VibratorStart"].ToString() : null,
                            VibratorMode = tempData.ContainsKey("VibratorMode") ? tempData["VibratorMode"].ToString() : null,
                            VibratorDuration = tempData.ContainsKey("VibratorDuration") ? tempData["VibratorDuration"].ToString() : null,
                            VibratorInterval = tempData.ContainsKey("VibratorInterval") ? tempData["VibratorInterval"].ToString() : null,
                            FeederStart = tempData.ContainsKey("FeederStart") ? tempData["FeederStart"].ToString() : null,
                            FeederMode = tempData.ContainsKey("FeederMode") ? tempData["FeederMode"].ToString() : null,
                            FeederSetHz = tempData.ContainsKey("FeederSetHz") ? tempData["FeederSetHz"].ToString() : null,
                            FeederActualHz = tempData.ContainsKey("FeederActualHz") ? tempData["FeederActualHz"].ToString() : null,
                            FanStart = tempData.ContainsKey("FanStart") ? tempData["FanStart"].ToString() : null,
                            FanMode = tempData.ContainsKey("FanMode") ? tempData["FanMode"].ToString() : null,
                            FanSetHz = tempData.ContainsKey("FanSetHz") ? tempData["FanSetHz"].ToString() : null,
                            FanActualHz = tempData.ContainsKey("FanActualHz") ? tempData["FanActualHz"].ToString() : null,
                        };



                        // 将解析后的数据存入 Redis
                        await _txSensorsDataviewService.StoreDtoInRedis(receivedTopic,data);
                    }
                    else
                    {
                        _logger.LogWarning($"从主题 {receivedTopic} 接收到的消息解析失败: {message}");
                    }
                });

                _logger.LogInformation($"成功订阅主题: {topic}");
                return AjaxResult.Success($"成功订阅主题: {topic}");
            }
            catch(Exception ex)
            {
                _logger.LogError($"订阅主题失败: {ex.Message}");
                return AjaxResult.Error("订阅主题失败",ex.Message);
            }
        }



     

        /// <summary>
        /// 订阅主题（ReplyTopic） 
        /// 将消息分组存入 _cache缓存 ，设置过期时间为 60 分钟
        /// </summary>
        /// <param name="topic">ReplyTopic</param>
        /// <returns></returns>
        [HttpGet("SubscribeReplyTopic")]
        [AllowAnonymous]
        public async Task<AjaxResult> MqttSubscribeReplyTopic( )
        {
            try
            {
                 

                // 如果没有传入 topic 参数，设置默认的订阅主题
                var topic = "ReplyTopic";

                // 调用 MQTT 服务的订阅方法
                await _mqttService.SubscribeToTopicAsync(topic,async (receivedTopic,message) =>
                {
                    try
                    {
                        // 记录收到的消息
                        _logger.LogInformation($"从主题 {receivedTopic} 接收到消息: {message}");

                        // 解析 JSON 数据为 MqttReplyWrapper 对象
                        var wrapper = JsonSerializer.Deserialize<MqttReplyWrapper>(message);
                        if(wrapper == null || wrapper.RwProt == null || string.IsNullOrEmpty(wrapper.RwProt.Id))
                        {
                            _logger.LogWarning($"消息解析失败或 ID 为空: {message}");
                            return;
                        }


                        //// 原生radis操作方式。
                        //// 构造 Redis 键，存入 ReplyTopic 文件夹中
                        //string cacheKey = $"ReplyTopic/{wrapper.RwProt.Id}";
                        //// 获取消息的 ID
                        var replyId = wrapper.RwProt.Id;
                        //// 将消息存入 Redis，设置过期时间为 5 分钟
                        //var redis = App.GetService<IConnectionMultiplexer>();
                        //var db = redis.GetDatabase();
                        //// string cacheKey = $"reply_{replyId}"; // Redis 键值基于消息 ID
                        //await db.StringSetAsync(cacheKey,message,TimeSpan.FromMinutes(5));

                        _cache.SetString(GetCacheKey(wrapper.RwProt.Id),message,60);



                        // 记录日志
                        _logger.LogInformation($"ID 为 {replyId} 的消息已存入 Redis");
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError($"处理回复消息时发生异常: {ex.Message}");
                    }
                });

                _logger.LogInformation($"成功订阅主题: {topic}");
                return AjaxResult.Success($"成功订阅主题: {topic}");
            }
            catch(Exception ex)
            {
                _logger.LogError($"订阅主题失败: {ex.Message}");
                return AjaxResult.Error("订阅主题失败",ex.Message);
            }
        }


        //////////////////////////////////////////////////////////自定义发布消息//////////////////////////////////////////////////////////////////////


        /// <summary>
        /// 自定义发布消息 -- 写入指令
        /// </summary>
        [HttpPost("publish")]
        [AllowAnonymous]
        public async Task<AjaxResult> MqttPublish([FromBody] PublishRequest request)
        {
            try
            {
               

                // 如果未指定主题，则设置默认主题
                string topic = request.Topic ?? "WriteTopic";
                string message = request.Message ?? "默认消息";

                // 调用 MQTT 服务的发布方法
                await _mqttService.PublishMessageAsync(topic,message);

                _logger.LogInformation($"消息已发布到主题: {topic}，内容: {message}");
                return AjaxResult.Success($"消息已发布到主题: {topic}");
            }
            catch(Exception ex)
            {
                _logger.LogError($"发布消息失败: {ex.Message}");
                return AjaxResult.Error("发布消息失败",ex.Message);
            }
        }




        ///////////////////////////////////////////////////////////////////下发数据给设备//////////////////////////////////////////////////////////////////////

        //  设备正确格式
        //          {
        //              "topic": "WriteTopic", // 可省略
        //              "wData":
        //                [
        //                   {  "name": "node0101", "value": "35" },
        //                   { "name": "node0102",  "value": "52" }
        //                ]
        //           }


        // 接口写入的正确格式
        //      [{
        //  "name": "FanSetHz",
        //  "value": "22"
        //}]



        /// <summary>
        /// 一、主动向设备下发指令，并读取设备回复。
        /// </summary>
        /// <param name="request">包含要下发的指令数据的请求对象</param>
        /// <returns>包含操作结果的 AjaxResult 对象</returns>
        [HttpPost("WriteToDeviceAndReadReply")]
        [AllowAnonymous]
        public async Task<AjaxResult> WriteToDeviceAndReadReply([FromBody] MqttWriteData[] request)
        {

            // 更新连接状态
            if(!_mqttService.IsConnected)
            {
                return AjaxResult.Error("通讯服务器连接失败");
 
            }

            // 调用WriteToDevice
            var writeResult = await WriteToDevice(request);
            if((int)writeResult[AjaxResult.CODE_TAG] != 200)
            {
                return AjaxResult.Error("写入设备指令失败",writeResult[AjaxResult.MSG_TAG]);
            }

            ////////////////////////////////////////////// 获取生成的唯一 ID////////////////////////////////////////////////////////////////////////////////////////////
            string idValue = "";
            // 获取 ID 值
            if(writeResult.TryGetValue("id",out var requestId))
            {
                idValue = requestId?.ToString() ?? string.Empty; // 转换为字符串
                _logger.LogInformation($"写入指令返回的 ID: {idValue}");
            }
            else
            {
                _logger.LogWarning("返回结果中未找到 ID 字段");
                return AjaxResult.Error("未找到消息ID");
            }

            // 等待一秒再取缓存
            // 后台反馈的返回数据必须在1S以上，否则前端会出现数据不及时的情况（因为设置主动上传最低1S）
            Thread.Sleep(1000);  


            //////////////////////////////////////////////验证/////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
            // 调用ReadReplyTopic
            var readResult = await ReadReplyTopic(idValue);
            if((int)readResult[AjaxResult.CODE_TAG] != 200)
            {
                return AjaxResult.Error("操作失败，请检查网络或设备",readResult[AjaxResult.MSG_TAG]);
            }

            return AjaxResult.Success("写入并读取设备回复成功",readResult[AjaxResult.DATA_TAG]);
        }




        /// <summary>
        /// 二、主动向设备下发指令，执行写入操作。
        /// 默认主题：WriteTopic
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("writeToDevice")]
        [AllowAnonymous]
        public async Task<AjaxResult> WriteToDevice([FromBody] MqttWriteData[] request)
        {
            try
            {
                

                if(request == null || request.Length == 0)
                {
                    return AjaxResult.Error("请求数据无效，缺少 w_data");
                }

                // 为当前请求生成唯一 ID
                string requestId = Guid.NewGuid().ToString("N").Substring(0,8); // 短 ID;
                var writeRequest = new
                {
                    rw_prot = new
                    {
                        Ver = "1.0.1",
                        dir = "down",
                        id = requestId,
                        w_data = request.Select(r => new { name = r.Name,value = r.Value }).ToList()
                    }
                };


 
                // 将指令序列化为 JSON
                string jsonPayload = JsonSerializer.Serialize(writeRequest);

                // 发布指令到 MQTT 主题
                string topic = "WriteTopic";// request.Topic ?? "WriteTopic";
                await _mqttService.PublishMessageAsync(topic,jsonPayload);

                _logger.LogInformation($"写入指令已发送，ID: {requestId}，Topic: {topic}, 数据: {jsonPayload}");

                // 返回生成的唯一 ID，供后续验证
                AjaxResult keyValuePairs = AjaxResult.Success($"写入指令已发送，ID: {requestId}");
                keyValuePairs.Add("id",requestId);

                return keyValuePairs;
            }
            catch(Exception ex)
            {
                _logger.LogError($"写入设备指令失败: {ex.Message}");
                return AjaxResult.Error("写入设备指令失败",ex.Message);
            }
        }



        /// <summary>
        /// 三、检查设备是否成功执行指令（去缓存根据id查询err标志）。
        /// 默认主题：ReplyTopic
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        [HttpGet("ReadReplyTopic")]
        [AllowAnonymous]
        public async Task<AjaxResult> ReadReplyTopic([FromQuery] string id)
        {
            try
            {
                _logger.LogInformation($"开始处理读取设备回复的请求，ID: {id}");

               

                if(string.IsNullOrEmpty(id))
                {
                    return AjaxResult.Error("参数 ID 不能为空");
                }

                string? configValue = await Task.Run(( ) => _cache.GetString(GetCacheKey(id)));

                if(string.IsNullOrEmpty(configValue))
                {
                    _logger.LogWarning($"未找到 ID {id} 的回复消息");
                    return AjaxResult.Error($"未找到 ID {id} 的回复消息");
                }

                // 解析 Redis 中的消息
                var cachedWrapper = JsonSerializer.Deserialize<MqttReplyWrapper>(configValue,new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if(cachedWrapper == null || cachedWrapper.RwProt == null)
                {
                    _logger.LogError($"ID {id} 的回复消息解析失败");
                    return AjaxResult.Error($"ID {id} 的回复消息解析失败");
                }


                // 检查 w_data 中的 err 字段
                if(cachedWrapper.RwProt.WData != null && cachedWrapper.RwProt.WData.Any(data => data.Err == "1"))
                {
                    _logger.LogWarning($"ID {id} 的回复消息中包含错误码 err: 1");
                    return AjaxResult.Error($"ID {id} 的修改成功，但存在错误",new
                    {
                        Message = "操作失败，请检查网络或设备！",
                        ReplyData = cachedWrapper.RwProt
                    });
                }

                // 如果没有错误，返回成功的修改信息
                return AjaxResult.Success($"ID {id} 的修改成功",cachedWrapper.RwProt);
             }
            catch(Exception ex)
            {
                _logger.LogError($"订阅主题失败: {ex.Message}");
                return AjaxResult.Error("订阅主题失败",ex.Message);
            }
        }



        ///////////////////////////////////////////////////////////////////获取缓存数据//////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 获取 Redis 中的最新一条传感器数据 
        /// </summary>
        [HttpGet("getInfoForRadis")]
        public async Task<ActionResult<TxSensorsDataviewDto007>> GetLatestSensorData(string? topic)
        {
            try
            {
                // 如果 topic 为空，设置默认值
                if(string.IsNullOrEmpty(topic))
                {
                    topic = "ReplyTopicAutoData"; // 设置你的默认主题
                }

                var data = _txSensorsDataviewService.GetLatestSensorData(topic);

                if(data == null)
                {
                    return Ok(new { message = $"没有读取到传感器数据，主题: {topic}" });
                }

                return Ok(data);
            }
            catch(Exception ex)
            {
                _logger.LogError($"处理请求时发生异常: {ex.Message}");
                return StatusCode(500,new { message = "服务器内部错误，请稍后重试。" });
            }
        }

       

    }
}
