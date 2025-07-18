using RuoYi.Common.Utils;
using RuoYi.Data;
using RuoYi.Data.Dtos;
using RuoYi.Data.Entities;
using RuoYi.Data.Models;

namespace RuoYi.System.Repositories
{
    /// <summary>
    ///  角色信息表 Repository
    ///  author ruoyi
    ///  date   2023-08-21 14:40:22
    /// </summary>
    public class SysRoleRepository : BaseRepository<SysRole,SysRoleDto>
    {
        public SysRoleRepository(ISqlSugarRepository<SysRole> sqlSugarRepository)
        {
            Repo = sqlSugarRepository;
        }

        public override ISugarQueryable<SysRole> Queryable(SysRoleDto dto)
        {
            var queryable = Repo.AsQueryable()
                .OrderBy((r) => r.RoleSort)
                //.Where((r) => r.DelFlag == DelFlag.No)
                .WhereIF(dto.RoleId > 0,(r) => r.RoleId == dto.RoleId)
//.WhereIF(dto.UserId > 0,(r) => r.RoleId == dto.UserId)  // 筛选角色  未经验证
                .WhereIF(!string.IsNullOrEmpty(dto.RoleName),(r) => r.RoleName!.Contains(dto.RoleName!)) // 增修的查重
                .WhereIF(!string.IsNullOrEmpty(dto.RoleKey),(r) => r.RoleKey!.Contains(dto.RoleKey!))  // 增修的查重
                .WhereIF(!string.IsNullOrEmpty(dto.Status),(r) => r.Status == dto.Status)
                //.WhereIF(dto.Params.BeginTime != null,(r) => r.CreateTime >= dto.Params.BeginTime)
                //.WhereIF(dto.Params.EndTime != null,(r) => r.CreateTime <= dto.Params.EndTime)
                .WhereIF(!string.IsNullOrEmpty(dto.Params.DataScopeSql),dto.Params.DataScopeSql); // 动态数据范围 SQL 条件

            //新增，按道理来说是自己查询自己的
            //.Where((r) => r.TenantId == dto.TenantId);

            //  打印sql
            var sqlInfo = queryable.ToSql();
            Console.WriteLine($"SQL语句: {sqlInfo.Key}");

            return queryable;
        }


        /// <summary>
        /// 多表：查询角色数据并返回 DTO 查询    修改： 去掉部门  增加组织筛选
        /// </summary>
        /// <param name="dto">角色 DTO 查询条件</param>
        /// <returns>查询结果</returns>
        //public override ISugarQueryable<SysRoleDto> DtoQueryable(SysRoleDto dto)
        //{
        //    int i = 0;
        //    // 构建查询   注意多表中r别名
        //    var queryable = Repo.AsQueryable()
        //        .LeftJoin<SysUserRole>((r,ur) => r.RoleId == ur.RoleId) // 左连接角色-用户表
        //        .LeftJoin<SysUser>((r,ur,u) => ur.UserId == u.UserId)  // 左连接用户表
        //        .OrderBy(r => r.RoleSort) // 按角色排序字段排序
        //        //.Where(r => r.DelFlag == DelFlag.No) // 过滤逻辑删除的记录
        //        .WhereIF(dto.RoleId > 0,r => r.RoleId == dto.RoleId) // 过滤角色ID
        //        .WhereIF(!string.IsNullOrEmpty(dto.RoleName),r => r.RoleName!.Contains(dto.RoleName!)) // 模糊匹配角色名
        //        .WhereIF(!string.IsNullOrEmpty(dto.RoleKey),r => r.RoleKey!.Contains(dto.RoleKey!)) // 模糊匹配角色Key
        //        .WhereIF(!string.IsNullOrEmpty(dto.Status),r => r.Status == dto.Status) // 过滤角色状态
        //                                                                                //.WhereIF(dto.Params.BeginTime != null, r => r.CreateTime >= dto.Params.BeginTime) // 起始时间过滤
        //                                                                                //.WhereIF(dto.Params.EndTime != null, r => r.CreateTime <= dto.Params.EndTime) // 截止时间过滤
        //                                                                                //.WhereIF(!string.IsNullOrEmpty(dto.UserName), (r, ur, u) => u.UserName == dto.UserName) // 过滤用户姓名
        //        .WhereIF(!string.IsNullOrEmpty(dto.Params.DataScopeSql),dto.Params.DataScopeSql) // 动态数据范围 SQL 条件
        //        .Select(r => new SysRoleDto
        //        {
        //            // 数据映射到 DTO
        //            CreateBy = r.CreateBy,
        //            CreateTime = r.CreateTime,
        //            UpdateBy = r.UpdateBy,
        //            UpdateTime = r.UpdateTime,
        //            RoleId = r.RoleId,
        //            RoleName = r.RoleName,
        //            RoleKey = r.RoleKey,
        //            RoleSort = r.RoleSort,
        //            DataScope = r.DataScope,
        //            MenuCheckStrictly = r.MenuCheckStrictly,
        //            DeptCheckStrictly = r.DeptCheckStrictly,
        //            Status = r.Status,
        //            DelFlag = r.DelFlag,
        //            Remark = r.Remark
        //        }).Distinct(); // 去重

        //    //  打印sql
        //    var sqlInfo = queryable.ToSql();
        //    Console.WriteLine($"Generated SQL: {sqlInfo.Key}");


        //    return queryable;
        //}



        // 单表测试
        public override ISugarQueryable<SysRoleDto> DtoQueryable(SysRoleDto dto)
        {
            // 原始的业务过滤复杂写法
            int i = 0;
            // 构建查询   注意多表中r别名
            var queryable = Repo.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(dto.Params.DataScopeSql),dto.Params.DataScopeSql) // 动态数据范围 SQL 条件
                 //.Where(r => r.TenantId == dto.TenantId)
                .Select(r => new SysRoleDto
                {
                    // 数据映射到 DTO
                    CreateBy = r.CreateBy,
                    CreateTime = r.CreateTime,
                    UpdateBy = r.UpdateBy,
                    UpdateTime = r.UpdateTime,
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    RoleKey = r.RoleKey,
                    RoleSort = r.RoleSort,
                    DataScope = r.DataScope,
                    MenuCheckStrictly = r.MenuCheckStrictly,
                    DeptCheckStrictly = r.DeptCheckStrictly,
                    Status = r.Status,
                    DelFlag = r.DelFlag,
                    Remark = r.Remark
                }).Distinct(); // 去重




            // 如果集团管理员则：
            LoginUser User = SecurityUtils.GetLoginUser();
            if(User.UserType == "GROUP_ADMIN")
            {
                queryable = GetSimpleFilteredRoles(dto);
            }
            else if(User.UserType == "COMPANY_ADMIN") // 如果公司管理员则：
            {
                queryable = GetSimpleFilteredRoles(dto);

            }



            //  打印sql
            var sqlInfo = queryable.ToSql();
            Console.WriteLine($"Generated SQL: {sqlInfo.Key}");


            return queryable;
        }


        /// //////////////////////////////////DtoQueryable过滤分支/////////////////////////////////////////////////////


        // 单独封装简单过滤条件的方法，返回 ISugarQueryable<SysRoleDto>
        private ISugarQueryable<SysRoleDto> GetSimpleFilteredRoles(SysRoleDto dto)
        {
            // 直接在 Repo 中查询 SysRole，增加简单的过滤条件 TenantId、Status='0'、DelFlag='0'
            return Repo.AsQueryable()
                   .Where(r => r.TenantId == dto.TenantId
                            && r.Status == "0"
                            && r.DelFlag == "0")
                   .Select(r => new SysRoleDto
                   {
                       CreateBy = r.CreateBy,
                       CreateTime = r.CreateTime,
                       UpdateBy = r.UpdateBy,
                       UpdateTime = r.UpdateTime,
                       RoleId = r.RoleId,
                       RoleName = r.RoleName,
                       RoleKey = r.RoleKey,
                       RoleSort = r.RoleSort,
                       DataScope = r.DataScope,
                       MenuCheckStrictly = r.MenuCheckStrictly,
                       DeptCheckStrictly = r.DeptCheckStrictly,
                       Status = r.Status,
                       DelFlag = r.DelFlag,
                       Remark = r.Remark
                       // 如果有其他需要映射的字段，请在此补充
                   })
                   .Distinct();
        }

        /// //////////////////////////////////////////////////// ///////////////////////////////////////////////////////////////


        protected override async Task FillRelatedDataAsync(IEnumerable<SysRoleDto> dtos)
        {
            await base.FillRelatedDataAsync(dtos);

            foreach(var d in dtos)
            {
                d.StatusDesc = Status.ToDesc(d.Status);
                d.DataScopeDesc = DataScope.ToDesc(d.DataScope);
            }
        }

        public SysRole GetRoleById(long roleId)
        {
            return this.FirstOrDefault(r => r.RoleId == roleId);
        }

        public async Task<SysRole> GetByRoleNameAsync(string roleName)
        {
            var query = new SysRoleDto { RoleName = roleName };
            return await base.GetFirstAsync(query);
        }

        public async Task<SysRole> GetByRoleKeyAsync(string roleKey)
        {
            var query = new SysRoleDto { RoleKey = roleKey };
            return await base.GetFirstAsync(query);
        }

        /// <summary>
        /// 按角色ID删除
        /// </summary>
        public async Task<int> DeleteByRoleIdsAsync(List<long> roleIds)
        {
            return await base.DeleteAsync(m => roleIds.Contains(m.RoleId));
        }
    }
}