using System.Linq;
using RuoYi.Common.Data;
using RuoYi.Data;
using RuoYi.Zk.AC.Dao;
using RuoYi.Zk.AC.Model.Dto;
using RuoYi.Zk.AC.Model.Entities;
using SqlSugar;

namespace RuoYi.Zk.AC.Repositories
{
    /// <summary>
    ///  空调信息 Repository
    ///  author ruoyi.net
    ///  date   2024-11-13 16:30:22
    /// </summary>
    public class AcAirConditionersRepository : BaseRepository<AcAirConditioners, AcAirConditionersDto>
    {
        public AcAirConditionersRepository(ISqlSugarRepository<AcAirConditioners> sqlSugarRepository)
        {
            Repo = sqlSugarRepository;
        }


        //  测试  调用封装中方法执行自定义语句
        public  List<AcAirConditioners> GetAcAirConditionersPagedList001(AcAirConditionersDto dto) 
        {
 
            // 获取基于 dto 的查询
            var queryable = Queryable(dto);
            // 检查 dto 是否实现了 IDataScopeDto 接口，并且 DataScopeSql 不为空
            //if(dto is IDataScopeDto dataScopeDto && !string.IsNullOrEmpty(dataScopeDto.DataScopeSql))
            //{
            //    // 将 DataScopeSql 条件应用到查询中
            //    queryable = queryable.Where(dataScopeDto.DataScopeSql);
            //}

 
            // 输出完整 SQL 语句（用于调试）
            string finalSql = queryable.ToSql().Key;
            Console.WriteLine("Complete SQL Query: " + finalSql);

            // 执行查询并返回结果 ， 自定义的查询
            return queryable.ToList();

        }




       // 这两个方法的设计目的是为特定的查询提供灵活的条件构建，让子类能够基于业务需求构造符合条件的查询语句。
       // 【注意】以下需要拼装sql片段  ，不然数据权限不生效

        public override ISugarQueryable<AcAirConditioners> Queryable(AcAirConditionersDto dto)
        {
            ISugarQueryable < AcAirConditioners > qs = Repo.AsQueryable()

                // 强制过滤
                 .Where((d) => d.IsDeleted == "0")
                //动态条件过滤
                .WhereIF(dto.Id > 0,(t) => t.Id == dto.Id)
                .WhereIF(!string.IsNullOrEmpty(dto.Params.DataScopeSql),dto.Params.DataScopeSql);

            return qs;
 
        }
        
        // 动态拼接中间拼接层  已完成 √
        public override ISugarQueryable<AcAirConditionersDto> DtoQueryable(AcAirConditionersDto dto)
        {

            //ISugarQueryable< AcAirConditionersDto > qs = Repo.AsQueryable()
            //     // 强制过滤
            //     .Where((d) => d.IsDeleted == "0")
            //    //动态条件过滤
            //    .WhereIF(dto.Id > 0, (t) => t.Id == dto.Id)
            //    .Select((t) => new AcAirConditionersDto
            //    {
            //        Id = t.Id
            //    }, true);
            //return qs;


            // 动态条件拼接
            string strSql = string.Empty;
            if(!string.IsNullOrEmpty(dto.Params.DataScopeSql))
            {
                strSql = dto.Params.DataScopeSql;
            }

            // 查询实体类，并映射到 DTO
            ISugarQueryable<AcAirConditionersDto> qs = Repo.AsQueryable()
                // 基础过滤条件：软删除标记
                .Where(d => d.IsDeleted == "0")
                // 动态条件过滤
                .WhereIF(dto.Id > 0,d => d.Id == dto.Id)
                // 拼接 DataScopeSql（如果存在）
                .WhereIF(!string.IsNullOrEmpty(strSql),strSql)
                // 映射实体到 DTO
                .Select(d => new AcAirConditionersDto
                {
                    Id = d.Id,
                    //Type = d.Type,
                    TenantId = d.TenantId,
                    Code = d.Code,
                    Brand = d.Brand,
                    Model = d.Model,
                    Status = d.Status,
                    Temperature = d.Temperature,
                    PipelinePressure = d.PipelinePressure,
                    ElectricityUsage = d.ElectricityUsage,
                    Province = d.Province,
                    City = d.City,
                    Address = d.Address,
                    Latitude = d.Latitude,
                    Longitude = d.Longitude,
                    IsDeleted = d.IsDeleted,
                    CreateBy = d.CreateBy,
                    CreateTime = d.CreateTime,
                    UpdateBy = d.UpdateBy,
                    UpdateTime = d.UpdateTime,
                    InstallationDate = d.InstallationDate,
                    //DataScopeSql = dto.DataScopeSql, // 保留动态条件
                    PageNum = dto.PageNum,
                    PageSize = dto.PageSize
                });

            return qs;

        }
    }
}