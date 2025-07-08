

using AspectCore.DynamicProxy;
using RuoYi.Common.Data;
using RuoYi.Common.Utils;
using RuoYi.Data.Entities;
using RuoYi.Zk.AC.Model.Dto;
using RuoYi.Zk.AC.Model.Entities;
using SqlSugar;

namespace RuoYi.Zk.AC.Dao
{
    /// <summary>
    /// 空调信息表 Repository
    /// </summary>
    public class ACInfoRepository : BaseRepository<AcAirConditioners,AcAirConditionersDto>
    {
        private readonly ISqlSugarClient _sqlSugarClient; // 注入 SqlSugarClient 实例  -- 异表操作

        public ACInfoRepository(ISqlSugarClient sqlSugarClient,ISqlSugarRepository<AcAirConditioners> sqlSugarRepository)
        {
            Repo = sqlSugarRepository;
            _sqlSugarClient = sqlSugarClient; // 注入 SqlSugarClient 实例

        }




        /// <summary>
        /// 查询所有空调设备信息 √
        /// </summary>
        public async Task<List<AcAirConditioners>> GetAllAirConditionersAsync( )
        {
            return await Repo.AsQueryable()
                             .Where(ac => ac.DelFlag == "0") // 软删除过滤
                             .ToListAsync();
        }




        /////////////////////////////////////////////////////必须实现的类/////////////////////////////////////////////////////////////////


        /// <summary>
        /// 带筛选的查询 -- 返回实体类
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public override ISugarQueryable<AcAirConditioners> Queryable(AcAirConditionersDto dto)
        {
            return Repo.AsQueryable()
                       //.WhereIF(dto.AgentId > 0, ac => ac.AgentId == dto.AgentId)     // 如果 AgentId 大于 0，则按代理商ID过滤
                       .WhereIF(dto.Status != null,ac => ac.Status == dto.Status)    // 如果 Status 不为空，则按空调状态过滤
                       .WhereIF(!string.IsNullOrEmpty(dto.Model),ac => ac.Model.Contains(dto.Model!))  // 如果 Model 不为空，则按空调型号进行模糊查询
                       .Where(ac => ac.DelFlag == "0");                               // 软删除过滤，仅查询未删除的记录
        }

        /// <summary>
        /// 带筛选的查询 -- 必须实现
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public override ISugarQueryable<AcAirConditionersDto> DtoQueryable(AcAirConditionersDto dto)
        {
            //预留
            return null;
        }


        /////////////////////////////////////////////////////重构/////////////////////////////////////////////////////////////////






        /// <summary>
        /// 获取代理商为父节点、公司为子节点的树形结构 - 备份
        /// </summary>
        public async Task<List<TreeNodeDto>> GetCompanyAgentTreeAsync( )
        {
            //// 修正 SQL 查询，代理商为父节点，公司为子节点
            //var query = @"
            //          SELECT 
            //        t1.tenant_id AS AgentId,
            //        t1.name AS AgentName,
            //        t1.code AS AgentCode,
            //        t2.tenant_id AS CompanyId,
            //        t2.name AS CompanyName,
            //        t2.code AS CompanyCode
            //    FROM tenant_user t1
            //    LEFT JOIN tenant_user t2 ON t1.tenant_id = t2.parent_id
            //    WHERE t1.is_deleted = '0' 
            //      AND t1.type = '2'  
            //      AND (t2.is_deleted = '0' OR t2.tenant_id IS NULL)
            //      AND (t2.type = '1' OR t2.type IS NULL)  
            //    ORDER BY t1.tenant_id, t2.tenant_id;
            //    ";

            //var results = await Repo.Context.Ado.SqlQueryAsync<dynamic>(query);

            //// 创建代理商树节点集合
            //var tree = new List<TreeNodeDto>();

            //// 使用字典来合并代理商与公司节点
            //var agentDictionary = new Dictionary<string, TreeNodeDto>();

            //// 遍历结果构建树结构
            //foreach (var result in results)
            //{
            //    // 如果代理商不在字典中，创建代理商节点
            //    if (!agentDictionary.ContainsKey(result.AgentCode))
            //    {
            //        var agentNode = new TreeNodeDto
            //        {
            //            Code = result.AgentCode,  // 代理商唯一编码
            //            Label = result.AgentName,  // 代理商名称
            //            Children = new List<TreeNodeDto>()  // 初始化子节点列表
            //        };
            //        agentDictionary[result.AgentCode] = agentNode;
            //        tree.Add(agentNode);  // 添加代理商节点到树中
            //    }

            //    // 如果存在公司信息，将公司节点加入代理商下
            //    if (result.CompanyId != null && !string.IsNullOrEmpty(result.CompanyCode))
            //    {
            //        var companyNode = new TreeNodeDto
            //        {
            //            Code = result.CompanyCode,  // 公司唯一编码
            //            Label = result.CompanyName,  // 公司名称
            //            Children = new List<TreeNodeDto>()  // 公司不需要子节点
            //        };
            //        agentDictionary[result.AgentCode].Children.Add(companyNode);
            //    }
            //}

            //return tree;


            long type = long.Parse(SecurityUtils.GetUserType());
            long tid = SecurityUtils.GetTenantId();

            List<TenantUser> tenantUsers;

            switch(type)
            {
                case 0L: // 系统
                         // 获取所有未删除、正常状态的租户用户
                    tenantUsers = await _sqlSugarClient.Queryable<TenantUser>()
                        .Where(t => t.IsDeleted == '0' && t.Status == '0')
                        .ToListAsync();
                    break;

                case 1L: // 公司的没必要显示了啊

                    // 获取指定租户ID的租户用户
                    //tenantUsers = await _sqlSugarClient.Queryable<TenantUser>()
                    //    .Where(t => t.IsDeleted == '0' && t.Status == '0')
                    //    .Where(t => t.TenantId == tid)
                    //    .ToListAsync();

                    return new List<TreeNodeDto>();
                    break;

                case 2L: // 代理
                         // 获取代理和其关联公司
                    tenantUsers = await _sqlSugarClient.Queryable<TenantUser>()
                        .Where(t => t.IsDeleted == '0' && t.Status == '0')
                        .Where(t => t.ParentId == tid || t.TenantId == tid)
                        .ToListAsync();
                    break;

                default:
                    // 其他情况返回空列表
                    return new List<TreeNodeDto>();
            }

            // 将列表转换为树形结构
            var treeNodes = BuildTree(tenantUsers,null);
            return treeNodes;
        }



        // 递归方法，用于构建树形结构,el-tree组件
        private List<TreeNodeDto> BuildTree(List<TenantUser> tenantUsers,long? parentId)
        {
            return tenantUsers
                .Where(t => t.ParentId == parentId)
                .Select(t => new TreeNodeDto
                {
                    TenantId = t.TenantId,
                    Label = t.Name,
                    Children = BuildTree(tenantUsers,t.TenantId) // 递归构建子节点
                })
                .ToList();
        }



        /// <summary>
        /// 分页查询空调列表，联查 TenantUser 表信息 旧版本运行正常
        /// </summary>
        //public async Task<SqlSugarPagedList<AcAirConditionerDto>> GetPagedAirConditionerListAsync001(AcAirConditionerDto dto)
        //{
        //    var query = Repo.AsQueryable()
        //        .LeftJoin<TenantUser>((ac,tenant) => ac.TenantUserId == tenant.TenantId)
        //        .Select((ac,tenant) => new AcAirConditionerDto
        //        {
        //            Id = ac.Id,
        //            Code = ac.Code,
        //            Brand = ac.Brand,
        //            Model = ac.Model,
        //            Status = ac.Status,
        //            Temperature = ac.Temperature,
        //            PipelinePressure = ac.PipelinePressure,
        //            ElectricityUsage = ac.ElectricityUsage,
        //            Province = ac.Province,
        //            City = ac.City,
        //            Address = ac.Address,
        //            Latitude = ac.Latitude,
        //            Longitude = ac.Longitude,
        //            TenantUserId = ac.TenantUserId,
        //            TenantName = tenant.Name,  // 映射租户名称
        //            IsDeleted = ac.IsDeleted,
        //            CreateBy = ac.CreateBy,
        //            CreateTime = ac.CreateTime,
        //            UpdateBy = ac.UpdateBy,
        //            UpdateTime = ac.UpdateTime,
        //            InstallationDate = ac.InstallationDate
        //        })
        //        .Distinct();

        //    // 调用分页查询方法并返回结果
        //    return await query.ToPagedListAsync(dto.PageNum,dto.PageSize);
        //}


        /// <summary>
        /// 设备列表 - 公司
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<SqlSugarPagedList<AcAirConditioners>> GetPagedAirConditionerListAsyncType1(AcAirConditionersDto dto)
        {

            /**         原始代码           **/
            //// 获取当前租户ID，默认值为0
            //long tid = SecurityUtils.GetTid() ?? 0;
            //// 获取用户所属组织类型
            //long type = SecurityUtils.GetUserType() ?? 0;                              // 获取第一条记录的 Type
            //// 管理员
            //ISugarQueryable <AcAirConditioner> query = _sqlSugarClient.Queryable<AcAirConditioner>()
            //    .Where(ac => ac.IsDeleted == "0"); // 筛选未删除的记录
            //// 根据不同类型的用户构建查询条件
            //if(type == 1) // 公司
            //{
            //    query = query.Where(ac => ac.TenantId == dto.TenantId); // 筛选指定 TenantId
            //}
            //else if(type == 2) // 代理
            //{
            //    // 获取代理下面所有公司 TenantId 列表 
            //    List<long> companyIds = await _sqlSugarClient.Queryable<TenantUser>()
            //        .Where(t => t.ParentId == tid && t.IsDeleted == '0') // 筛选 ParentId 为代理的 TenantId
            //        .Select(t => t.TenantId)  // 返回 TenantId 列表（long 类型）
            //        .ToListAsync();

            //    // 查询代理所管理的多个公司下的空调数据
            //    query = query.Where(ac => companyIds.Contains(ac.TenantId));
            //}



            //
            //long tid = SecurityUtils.GetTid() ?? dto.TenantId;

            ISugarQueryable<AcAirConditioners> query = _sqlSugarClient.Queryable<AcAirConditioners>()
                .Where(ac => ac.DelFlag == "0")
                .Where(ac => ac.TenantId == dto.TenantId); // 筛选指定 TenantId
            // 调用分页查询方法并返回结果
            return await query.ToPagedListAsync(dto.PageNum,dto.PageSize);
        }



        /// <summary>
        /// 设备列表 - 代理  - 非常正常
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<SqlSugarPagedList<AcAirConditioners>> GetPagedAirConditionerListAsyncType2(AcAirConditionersDto dto)
        {

            // 获取当前租户ID，默认值为0
            // long tid = SecurityUtils.GetTid() ?? dto.TenantId;
            // 获取用户所属组织类型
            long type = long.Parse(SecurityUtils.GetUserType());                              // 获取第一条记录的 Type


            // 获取代理下面所有公司 TenantId 列表 
            List<long> companyIds = await _sqlSugarClient.Queryable<TenantUser>()
                .Where(t => t.ParentId == dto.TenantId && t.IsDeleted == '0') // 筛选 ParentId 为代理的 TenantId
                .Select(t => t.TenantId)  // 返回 TenantId 列表（long 类型）
                .ToListAsync();

            // 分开组织和用户的设计思路  todo:

            // 还需要个获取公司下所有用户 createID列表



            // 查询代理所管理的多个公司下的空调数据
            ISugarQueryable<AcAirConditioners> query = _sqlSugarClient.Queryable<AcAirConditioners>()
                .Where(ac => companyIds.Contains(ac.TenantId));


            // 调用分页查询方法并返回结果
            return await query.ToPagedListAsync(dto.PageNum,dto.PageSize);
        }








        /// <summary>
        /// 设备列表 - 管理员
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<SqlSugarPagedList<AcAirConditioners>> GetPagedAirConditionerListAsyncType0(AcAirConditionersDto dto)
        {
 

            // 查询代理所管理的多个公司下的空调数据
            ISugarQueryable<AcAirConditioners> query = _sqlSugarClient.Queryable<AcAirConditioners>()
                .Where(t => t.DelFlag == "0");


            // 调用分页查询方法并返回结果
            return await query.ToPagedListAsync(dto.PageNum,dto.PageSize);
        }



    }
}
