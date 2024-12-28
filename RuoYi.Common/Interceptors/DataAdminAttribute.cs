using AspectCore.DynamicProxy;
using RuoYi.Common.Data;
using RuoYi.Common.Utils;
using RuoYi.Data;
using RuoYi.Data.Dtos;
using RuoYi.Data.Models;
using RuoYi.Framework.Logging;
using RuoYi.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


//    整个流程是这样的:
//1. 标注了[DataScope] 的虚方法 被 DataScopeAttribute 拦截
//2. DataScopeAttribute 中的 DataScopeFilter 方法 拼接sql 放到 dto中的DataScopeSql字段中
//3. 虚方法调用 仓储的 Querable方法, Querable方法中 最后一个 WhereIF把 拦截器生成的sql(DataScopeSql) 拼接到整个查询sql中


//del_flag 是通用字段，每个表都有。如果查询涉及多表（如 sys_role、sys_user_role、sys_user），而 del_flag 没有明确指定来源表，就会导致 SQL 生成时的歧义问题。

namespace RuoYi.Common.Interceptors
{
    /// <summary>
    /// 自定义数据范围过滤特性
    /// </summary>
    public class DataAdminAttribute : AbstractInterceptorAttribute
    {
        public string? DeptAlias { get; set; } // 用于指定部门字段的别名
        public string? UserAlias { get; set; } // 用于指定用户字段的别名
        public string? RoleAlias { get; set; } // 用于指定角色字段的别名

        public string TableAlias { get; set; } = "r";  // 表别名默认值为 "r"  
        /// <summary>
        /// 拦截器的核心方法，在目标方法执行前后进行操作
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async override Task Invoke(AspectContext context,AspectDelegate next)
        {
            try
            {
                LoginUser loginUser = SecurityUtils.GetLoginUser();
                if(loginUser != null)
                {
                    SysUserDto currentUser = loginUser.User;
                    if(currentUser != null && !SecurityUtils.IsAdmin(currentUser))
                    {
                        ApplyScopeFilter(context,currentUser,DeptAlias,UserAlias);
                    }
                }

                await next(context);
            }
            catch(Exception ex)
            {
                Log.Error("DataRange Error",ex);
                throw;
            }
        }

        /// <summary>
        /// 根据用户的数据范围生成 SQL 过滤条件，通过 StringBuilder 动态构建权限 SQL 片段
        /// </summary>
        /// <param name="context"></param>
        /// <param name="user"></param>
        /// <param name="deptAlias"></param>
        /// <param name="userAlias"></param>
        private void ApplyScopeFilter(AspectContext context,SysUserDto user,string deptAlias,string userAlias)
        {
            StringBuilder sqlString = new StringBuilder();


            // 数据范围默认对应用户类型:
            // SUPER_ADMIN：超级管理员、
            // GROUP_ADMIN：集团管理员、
            // GROUP_USER：集团普通用户、
            // COMPANY_ADMIN：公司管理员、
            // COMPANY_USER：公司普通用户。
            // string dataScope = user.UserType ?? "COMPANY_USER"; // 默认为 "4" 表示个人范围


            string type = SecurityUtils.GetUserType(); //用户类型

            // 验证范围
            // switch(dataScope)
            switch(type)
            {
                case "SUPER_ADMIN": // 超级管理员  这里需要特殊修改逻辑，可能需要特殊的管理页面
                    sqlString.Clear(); // 查询所有数据，无需进一步过滤
                    break;

                case "GROUP_ADMIN": // 集团管理员

                    long[] res = SecurityUtils.GetTenantChildId(); // 当前代理tid+代理下公司子集
                    string inClause = string.Join(", ",res);  // 将数组转换为逗号分隔的字符串

                    if(StringUtils.IsNotBlank(userAlias))
                    {
                        // 多表操作，防止字段冲突
                        sqlString.Append($@"
                             {TableAlias}.del_flag = '0'
                            AND (
                                 {TableAlias}.tenant_id IN ({inClause})
                            )
                        ");
                    }
                    else
                    {
                        // 单表操作
                        sqlString.Append($@"
                             del_flag = '0'
                            AND (
                                 tenant_id IN ({inClause})
                            )
                        ");
                    }


                    break;

                case "COMPANY_ADMIN": // 公司管理员
                    sqlString.Append($" tenant_id = {user.TenantId} ");
                    break;

                default:
                    throw new ArgumentException("未知的数据范围类型");
            }



            if(sqlString.Length > 0)
            {
                string dataScopeSql = sqlString.ToString().TrimStart("OR ".ToCharArray());
                if(!context.AdditionalData.ContainsKey("dataScopeSql"))
                {
                    context.AdditionalData.Add("dataScopeSql","WHERE " + dataScopeSql);
                }
                else
                {
                    context.AdditionalData["dataScopeSql"] = "AND " + dataScopeSql;
                }

                // dto传递拼接语句
                // 将生成的 SQL 条件赋值给 dto 的 DataScopeSql 属性
                if(context.Parameters.Length > 0 && context.Parameters[0] is BaseDto baseDto)
                {
                    // 现在 dto 是一个 IDataScopeDto，您可以访问 dto.DataScopeSql
                    baseDto.Params.DataScopeSql = dataScopeSql;
                }

            }

        }
    }
}
