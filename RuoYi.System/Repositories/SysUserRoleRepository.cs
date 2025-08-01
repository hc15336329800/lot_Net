using RuoYi.Data;
using RuoYi.Data.Dtos;
using RuoYi.Data.Entities;

namespace RuoYi.System.Repositories
{
    /// <summary>
    ///  用户和角色关联表 Repository
    ///  author ruoyi.net
    ///  date   2023-08-23 09:43:52
    /// </summary>
    public class SysUserRoleRepository : BaseRepository<SysUserRole, SysUserRoleDto>
    {
        public SysUserRoleRepository(ISqlSugarRepository<SysUserRole> sqlSugarRepository)
        {
            Repo = sqlSugarRepository;
        }

        // 增加： tid隔离
        public override ISugarQueryable<SysUserRole> Queryable(SysUserRoleDto dto)
        {
            return Repo.AsQueryable() 
                //.Where((d) => d.TenantId == dto.TenantId) //tid
                .WhereIF(dto.UserId > 0, (t) => t.UserId == dto.UserId)
                .WhereIF(dto.RoleId > 0, (t) => t.RoleId == dto.RoleId)
            ;
        }

        public override ISugarQueryable<SysUserRoleDto> DtoQueryable(SysUserRoleDto dto)
        {
            return Repo.AsQueryable()
                //.Where((d) => d.TenantId == dto.TenantId) //tid
                .WhereIF(dto.UserId > 0, (t) => t.UserId == dto.UserId)
                .WhereIF(dto.RoleId > 0, (t) => t.RoleId == dto.RoleId)
                .Select((t) => new SysUserRoleDto
                {
                    UserId = t.UserId,
                    RoleId = t.RoleId
                });
        }

        public int DeleteUserRoleByUserId(long userId)
        {
            return Repo.Delete(r => r.UserId == userId);
        }

        public int DeleteUserRole(List<long> userIds)
        {
            return Repo.Delete(r => userIds.Contains(r.UserId));
        }

        /// <summary>
        /// 按 角色ID 查询数量
        /// </summary>
        public async Task<int> CountUserRoleByRoleIdAsync(long roleId)
        {
            return await Repo.CountAsync(ur => ur.RoleId == roleId);
        }

        /// <summary>
        /// 按用户+角色删除
        /// </summary>
        public async Task<int> DeleteUserRoleInfoAsync(long roleId, long userId)
        {
            return await Repo.DeleteAsync(r => r.RoleId == roleId && r.UserId == userId);
        }
        public async Task<int> DeleteUserRoleInfoAsync(long roleId, List<long> userIds)
        {
            return await Repo.DeleteAsync(r => r.RoleId == roleId && userIds.Contains(r.UserId));
        }


        /// <summary>
        /// 整改：根据用户ID查询角色名称列表
        /// </summary>
        public async Task<List<string>> GetRoleNamesByUserIdAsync(long userId)
        {
            return await Repo.AsQueryable()
                .InnerJoin<SysRole>((ur,r) => ur.RoleId == r.RoleId) // 关联 sys_role
                .Where((ur,r) => ur.UserId == userId) // 筛选指定 user_id
                .Select((ur,r) => r.RoleName) // 只查询 role_name
                .ToListAsync();
        }


    }
}