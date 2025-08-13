using RuoYi.Framework.Attributes;
using RuoYi.Iot.Services;
using RuoYi.Quartz.Dtos;
using RuoYi.Quartz.Utils;

namespace RuoYi.Iot.Tasks;

/// <summary>
/// 测试用：定时读取并写入设备点位
/// </summary>
[Task("iotTask")]
public class IotTask
{
    /// <summary>
    /// 示例：读取点位当前值并加 1 写回（直接传参）
    /// </summary>
    public async Task ReadAndWriteWithParams(long deviceId,string pointKey)
    {
        var logger = App.GetService<ILogger<IotTask>>();
        var deviceSvc = App.GetService<IotDeviceService>();
        var varSvc = App.GetService<IotDeviceVariableService>();

        var device = await deviceSvc.GetDtoAsync(deviceId);
        if(device == null)
        {
            logger.LogWarning($"Device {deviceId} not found");
            return;
        }

        var map = await varSvc.GetVariableMapAsync(deviceId);
        if(!map.TryGetValue(pointKey,out var variable) || !variable.VariableId.HasValue)
        {
            logger.LogWarning($"Point {pointKey} not found on device {deviceId}");
            return;
        }

        logger.LogInformation($"Current {pointKey} value: {variable.CurrentValue}");

        var newVal = (int.Parse(variable.CurrentValue ?? "0") + 1).ToString();
        await varSvc.SaveValueAsync(deviceId,variable.VariableId.Value,pointKey,newVal);

        logger.LogInformation($"Updated {pointKey} to {newVal}");
    }

    /// <summary>
    /// 示例：根据任务配置自动读取并写入设备点位
    /// </summary>
    public async Task ReadAndWrite( )
    {

        Console.WriteLine("测试 ：定时任务ReadAndWrite执行");
        return;

 


        var logger = App.GetService<ILogger<IotTask>>();
        var job = JobContext.CurrentJob as SysJobIotDto;
        if(job?.DeviceId == null || string.IsNullOrWhiteSpace(job.SelectPoints))
        {
            logger.LogWarning("Job configuration missing device or point information");
            return;
        }

        var pointKey = job.SelectPoints.Split(',',StringSplitOptions.RemoveEmptyEntries).First();
        var deviceSvc = App.GetService<IotDeviceService>();
        var varSvc = App.GetService<IotDeviceVariableService>();

        var device = await deviceSvc.GetDtoAsync(job.DeviceId.Value);
        if(device == null)
        {
            logger.LogWarning($"Device {job.DeviceId} not found");
            return;
        }

        var map = await varSvc.GetVariableMapAsync(job.DeviceId.Value);
        if(!map.TryGetValue(pointKey,out var variable) || !variable.VariableId.HasValue)
        {
            logger.LogWarning($"Point {pointKey} not found on device {job.DeviceId}");
            return;
        }

        logger.LogInformation($"Current {pointKey} value: {variable.CurrentValue}");

        var newVal = (int.Parse(variable.CurrentValue ?? "0") + 1).ToString();
        await varSvc.SaveValueAsync(job.DeviceId.Value,variable.VariableId.Value,pointKey,newVal);

        logger.LogInformation($"Updated {pointKey} to {newVal}");
    }
}