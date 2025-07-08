using RuoYi.Common.Data;
using RuoYi.Data.Dtos;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Zk.AC.Model.Dto
{
    /// <summary>
    /// 空调信息数据传输对象 (DTO)
    /// </summary>
    public class AcAirConditionersDto : BaseDto
    {
        /// <summary>
        /// 空调ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// type 标志
        /// </summary>
        public long Type { get; set; }

        /// <summary>
        /// TenantId
        /// </summary>
        public long TenantId { get; set; }

        /// <summary>
        /// 空调唯一编码
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// 型号
        /// </summary>
        public string? Brand { get; set; }

        /// <summary>
        /// 空调型号
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// 空调状态 (如运行中、停止等)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 出风口温度
        /// </summary>
        public decimal? Temperature { get; set; }

        /// <summary>
        /// 管道压力
        /// </summary>
        public decimal? PipelinePressure { get; set; }

        /// <summary>
        /// 电度使用情况
        /// </summary>
        public decimal? ElectricityUsage { get; set; }

        /// <summary>
        /// 安装省份
        /// </summary>
        public string? Province { get; set; }

        /// <summary>
        /// 安装城市
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// 安装地址
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// 纬度
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// 经度
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// 软删除标记 (0:未删除, 1:已删除)
        /// </summary>
        public string DelFlag { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string? CreateBy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// 更新人
        /// </summary>
        public string? UpdateBy { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 查询的起始时间 (可选)
        /// </summary>
        public DateTime? BeginTime { get; set; }

        /// <summary>
        /// 查询的结束时间 (可选)
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 安装日期
        /// </summary>
        public DateTime? InstallationDate { get; set; }

        // 添加分页相关的属性
        public int PageNum { get; set; }    // 当前页码


        public int PageSize { get; set; }   // 每页大小
 

        /** 所属公司或代理 */
         public long TenantUserId { get; set; }


        public string? TenantName { get; set; }   // 租户公司名称


        public List<AcAirConditionersDto> Children { get; set; } = new List<AcAirConditionersDto>(); // 添加 Children 属性



        // 如果需要从拦截器传入 dataScopeSql，可以添加此属性
        public string DataScopeSql { get; set; }
    }
}
