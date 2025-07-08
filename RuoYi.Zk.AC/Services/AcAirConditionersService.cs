using Mapster;
using Microsoft.Extensions.Logging;
using RuoYi.Common.Data;
using RuoYi.Common.Interceptors;
using RuoYi.Data;
using RuoYi.Data.Dtos;
using RuoYi.Data.Entities;
using RuoYi.Framework.DependencyInjection;
using RuoYi.Zk.AC.Dao;
using RuoYi.Zk.AC.Model.Dto;
using RuoYi.Zk.AC.Model.Entities;
using RuoYi.Zk.AC.Repositories;
using SqlSugar;

namespace RuoYi.Zk.AC.Services
{
    /// <summary>
    ///  空调信息 Service
    ///  author ruoyi.net
    ///  date   2024-11-13 16:30:22
    /// </summary>
    public class AcAirConditionersService : BaseService<AcAirConditioners, Model.Dto.AcAirConditionersDto>, ITransient
    {
        private readonly ILogger<AcAirConditionersService> _logger;
        private readonly AcAirConditionersRepository _acAirConditionersRepository;

        public AcAirConditionersService(ILogger<AcAirConditionersService> logger,
            AcAirConditionersRepository acAirConditionersRepository)
        {
            BaseRepo = acAirConditionersRepository;

            _logger = logger;
            _acAirConditionersRepository = acAirConditionersRepository;
        }
        /// <summary>
        /// 分页查询用户列表
        /// </summary>
        //[DataScope(DeptAlias = "d")]
        [DataAdmin]
        public virtual async Task<SqlSugarPagedList<AcAirConditionersDto>> GetAcAirConditionersPagedList(AcAirConditionersDto dto)
        {
            //return await _acAirConditionersRepository.GetDtoPagedListAsync(dto); 



            // 第一种：手动插入动态语句
            //var queryable = _acAirConditionersRepository.DtoQueryable(dto); // 此处执行了 强制函数
            //if(!string.IsNullOrEmpty(dto.Params.DataScopeSql))
            //{
            //    queryable = queryable.Where(dto.Params.DataScopeSql);
            //}
            //string finalSql = queryable.ToSql().Key;
            //Console.WriteLine("完整的 SQL 查询语句: " + finalSql);
            //return await queryable.ToPagedListAsync(dto.PageNum,dto.PageSize);



            // 第二种：在DtoQueryable方法中插入动态语句
            var queryable = _acAirConditionersRepository.DtoQueryable(dto); // 此处执行了 强制函数
     
            // 打印完整语句
            string finalSql = queryable.ToSql().Key;
            Console.WriteLine("完整的 SQL 查询语句: " + finalSql);

            return await queryable.ToPagedListAsync(dto.PageNum,dto.PageSize);

        }



        /// <summary>
        /// 查询全部数据  过滤 测试
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        //[DataRange(DeptAlias = "d")]
        public virtual async Task<SqlSugarPagedList<AcAirConditioners>> GetAcAirConditionersPagedList001(AcAirConditionersDto dto)
        {
            return await GetPagedListAsync(dto);
           // return   _acAirConditionersRepository.GetAcAirConditionersPagedList001(dto);
        }



        /// <summary>
        /// 查询 空调信息 详情
        /// </summary>
        public async Task<AcAirConditioners> GetAsync(int id)
        {
            var entity = await base.FirstOrDefaultAsync(e => e.Id == id);
            return entity;
        }

        /// <summary>
        /// 查询 空调信息 详情
        /// </summary>
        public async Task<AcAirConditionersDto> GetDtoAsync(int id)
        {
            var entity = await base.FirstOrDefaultAsync(e => e.Id == id);
            var dto = entity.Adapt<AcAirConditionersDto>();
            // TODO 填充关联表数据
            return dto;
        }
    }
}