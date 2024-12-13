using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.System.Repositories
{
    /// <summary>
    /// 用户租户关联表 Repository
    /// </summary>
    public class SysUserTenantRepository : BaseRepository<SysUserTenant,SysUserTenantDto>
    {

        public SysUserTenantRepository(ISqlSugarRepository<SysUserTenant> sqlSugarRepository)
        {
            Repo = sqlSugarRepository; // 1、必须实现
        }



        // ------------------------------ 2、 必须实现接口 ------------------------------------------- 

        /// <summary> 
        ///  获取Dto查询对象 ,动态查询
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public override ISugarQueryable<SysUserTenantDto> DtoQueryable(SysUserTenantDto dto)
        {

            var query = Repo.Context.Queryable<SysUserTenant>()
                .WhereIF(dto.TenantId > 0,it => it.TenantId == dto.TenantId)
                .Select(it => new SysUserTenantDto
                {
                    UserId = it.UserId,
                    TId = it.TId,
                    TenantId = it.TenantId
                });
            return query;
        }

        /// <summary>
        /// 获取查询对象 ,动态查询
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public override ISugarQueryable<SysUserTenant> Queryable(SysUserTenantDto dto)
        {
            var query = Repo.Context.Queryable<SysUserTenant>()
                .WhereIF(dto.TenantId > 0,it => it.TenantId == dto.TenantId);
            return query;
        }



        // ------------------------------ 3、 业务方法 ------------------------------------------- 


        /// <summary>
        /// 删除用户组织中间表
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public int DeleteUserTenantByUserId(long userId)
        {
            return Repo.Delete(up => up.UserId == userId);
        }

    }
}
