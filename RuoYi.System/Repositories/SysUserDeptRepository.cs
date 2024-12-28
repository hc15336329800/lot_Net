using RuoYi.Data;
using RuoYi.Data.Dtos;
using RuoYi.Data.Entities;

namespace RuoYi.System.Repositories
{
    /// <summary>
    ///  用户和部门关联表 Repository
    ///  author ruoyi.net
    ///  date   2023-08-23 09:43:52
    /// </summary>
    public class SysUserDeptRepository : BaseRepository<SysUserDept, SysUserDeptDto>
    {
        public SysUserDeptRepository(ISqlSugarRepository<SysUserDept> sqlSugarRepository)
        {
            Repo = sqlSugarRepository;
        }

        public override ISugarQueryable<SysUserDept> Queryable(SysUserDeptDto dto)
        {
            return Repo.AsQueryable()
                .WhereIF(dto.UserId > 0, (t) => t.UserId == dto.UserId)
                .WhereIF(dto.DeptId > 0, (t) => t.DeptId == dto.DeptId)
            ;
        }

        public override ISugarQueryable<SysUserDeptDto> DtoQueryable(SysUserDeptDto dto)
        {
            return Repo.AsQueryable()
                .WhereIF(dto.UserId > 0, (t) => t.UserId == dto.UserId)
                .WhereIF(dto.DeptId > 0, (t) => t.DeptId == dto.DeptId)
                .Select((t) => new SysUserDeptDto
                {
                    UserId = t.UserId,
                    DeptId = t.DeptId
                });
        }



        /// <summary>
        /// 按部门ID删除
        /// </summary>
        public async Task<int> DeleteByDeptIdAsync(long deptId)
        {
            return await base.DeleteAsync(rd => rd.DeptId == deptId);
        }
    }
}