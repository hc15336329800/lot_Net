using System.Diagnostics;
using System.Security.Cryptography;
using Lazy.Captcha.Core;
using RuoYi.Common.Constants;
using RuoYi.Common.Enums;
using RuoYi.Common.Utils;
using RuoYi.Data.Enums;
using RuoYi.Data.Models;
using RuoYi.Framework.Cache;
using RuoYi.Framework.Exceptions;
using RuoYi.System.Slave.Services;
using SqlSugar;

namespace RuoYi.System.Services;

public class SysLoginService : ITransient
{
    private readonly ILogger<SysLoginService> _logger;
    private readonly ICaptcha _captcha;
    private readonly ICache _cache;
    private readonly TokenService _tokenService;
    private readonly SysUserService _sysUserService;
    private readonly SysConfigService _sysConfigService;
    private readonly SysLogininforService _sysLogininforService;
    private readonly SysPasswordService _sysPasswordService;
    private readonly SysPermissionService _sysPermissionService;
    private readonly ISqlSugarClient _sqlSugarClient; // 强行注入 SqlSugarClient 实例 

    private readonly SysTenantService _sysTenantService;
    private readonly SysUserTenantService _sysUserTenantService;


    public SysLoginService(ISqlSugarClient sqlSugarClient,ILogger<SysLoginService> logger,ICaptcha captcha,
        ICache cache,TokenService tokenService,
        SysUserService sysUserService,SysConfigService sysConfigService,
        SysLogininforService sysLogininforService,SysPasswordService sysPasswordService,
        SysPermissionService sysPermissionService,SysTenantService sysTenantService,SysUserTenantService sysUserTenantService)
    {
        _logger = logger;
        _captcha = captcha;
        _cache = cache;
        _tokenService = tokenService;
        _sysUserService = sysUserService;
        _sysConfigService = sysConfigService;
        _sysLogininforService = sysLogininforService;
        _sysPasswordService = sysPasswordService;
        _sysPermissionService = sysPermissionService;
        _sqlSugarClient = sqlSugarClient; // 注入 SqlSugarClient 实例
        _sysTenantService = sysTenantService;
        _sysUserTenantService = sysUserTenantService;


    }



    //==================================================================   登录 ======================================================================================




    // 登录接口测试
    public async Task<string> LoginAsync(string username,string password,string code,string uuid,long tenantId,string userType)
    {
        var sw = Stopwatch.StartNew();
        long lastElapsed = 0;

        // 辅助本地函数：记录某一步耗时
        void LogStep(string stepName)
        {
            var now = sw.ElapsedMilliseconds;
            var delta = now - lastElapsed;
            _logger.LogInformation("LoginAsync 步骤[{Step}] 耗时 {Delta} ms",stepName,delta);
            lastElapsed = now;
        }

        try
        {
            // =============================================== 验证 & 日志 ===============================================

            // 验证码校验
            ValidateCaptcha(username,code,uuid);
            LogStep("验证码校验");

            // 登录前置校验
            LoginPreCheck(username,password);
            LogStep("登录前置校验");


            // 查询用户信息，映射到 userDto 【此查询耗时700ms】
            var userDto = await _sysUserService.GetDtoByUsernameAsync(username);
            LogStep("查询用户信息");

            // 账户密码验证
            CheckLoginUser(username,password,userDto);
            LogStep("账户密码验证");


            // 异步记录登录成功（fire-and-forget，不阻塞主流程）
            _ = Task.Run(async ( ) =>
            {
                try
                {
                    await _sysLogininforService.AddAsync(
                        username,
                        Constants.LOGIN_SUCCESS,
                        MessageConstants.User_Login_Success);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex,"异步记录登录成功日志时出错");
                }
            });


            // ================================================= 权限 & 缓存 =================================================

            // 创建 LoginUser 对象并加载权限
            var loginUser = CreateLoginUser(userDto);
            LogStep("创建 LoginUser 对象并加载权限");

            // 更新用户最后登录信息（IP、时间等）
            await RecordLoginInfoAsync(userDto.UserId);

            LogStep("更新用户最后登录信息");

            // 重用 userDto.UserType
            loginUser.UserType = userDto.UserType;
            loginUser.User.UserType = userDto.UserType;

            // 如果前端未传 tenantId，则根据用户关联租户取第一个
            var tidList = _sysUserTenantService.GetTenantIdsListByUserId(userDto.UserId);
            long actualTenantId = tidList.FirstOrDefault();
            loginUser.TenantId = actualTenantId;
            loginUser.User.TenantId = actualTenantId;
            LogStep("获组织tid第一个");

            // ========================================= 部门、租户子集 =========================================

            // 部门子集
            var deptChildIds = GetChildrenDeptById(userDto.DeptId ?? 0);
            loginUser.DeptChildId = deptChildIds;
            loginUser.User.DeptChildId = deptChildIds;
            LogStep("部门子集");

            // 租户子集
            long[] tenantChildIds;
            switch(loginUser.UserType)
            {
                case "SUPER_ADMIN":
                    // 超级管理员：可访问所有租户
                    tenantChildIds = GetChildrenTenantById(userDto.TenantId);
                    LogStep("租户子集-超级管理员");
                    break;

                case "GROUP_ADMIN":
                    // 集团管理员：直属子租户
                    tenantChildIds = GetChildrenTenantByIdAdminGroup(userDto.TenantId);
                    loginUser.TenantChildId = tenantChildIds;
                    loginUser.User.TenantChildId = tenantChildIds;
                    LogStep("租户子集-集团管理员");
                    //return await _tokenService.CreateToken(loginUser);

                    Task<String> tokenG = _tokenService.CreateToken(loginUser);
                    LogStep("创建令牌，返回Token");
                    return await tokenG;


                case "COMPANY_ADMIN":
                    // 公司管理员：只需生成 Token
                    LogStep("租户子集-公司管理员");

                    return await _tokenService.CreateToken(loginUser);

                default:
                    // 普通用户：同超级管理员处理
                    tenantChildIds = GetChildrenTenantById(userDto.TenantId);
                    LogStep("租户子集-普通用户");
                    break;
            }

            loginUser.TenantChildId = tenantChildIds;
            loginUser.User.TenantChildId = tenantChildIds;

            // TODO: 缺少角色信息的加载

            Task<String>  token =   _tokenService.CreateToken(loginUser);
            LogStep("创建令牌，返回Token");


            // 生成并返回 Token
            return await token;

        }
        finally
        {
            sw.Stop();
            _logger.LogInformation(
                "LoginAsync 总耗时 {Total} ms",
                sw.ElapsedMilliseconds);
        }



    }







    // 登录接--暂时没用到，备用误删
    public async Task<string> LoginAsyncV2(string username,string password,string code,string uuid,long tenantId,string userType)
    {

        // =============================================== 验证 & 日志 ===============================================

        // 验证码校验
        ValidateCaptcha(username,code,uuid);

        // 登录前置校验
        LoginPreCheck(username,password);

        // 查询用户信息，映射到 userDto
        var userDto = await _sysUserService.GetDtoByUsernameAsync(username);

        // 账户密码验证
        CheckLoginUser(username,password,userDto);

        // 异步记录登录成功（fire-and-forget，不阻塞主流程）
        _ = Task.Run(async ( ) =>
        {
            try
            {
                await _sysLogininforService.AddAsync(
                    username,
                    Constants.LOGIN_SUCCESS,
                    MessageConstants.User_Login_Success);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,"异步记录登录成功日志时出错");
            }
        });


        // ================================================= 权限 & 缓存 =================================================

        // 创建 LoginUser 对象并加载权限
        var loginUser = CreateLoginUser(userDto);

        // 更新用户最后登录信息（IP、时间等）
        await RecordLoginInfoAsync(userDto.UserId);

        // 重用 userDto.UserType
        loginUser.UserType = userDto.UserType;
        loginUser.User.UserType = userDto.UserType;

        // 如果前端未传 tenantId，则根据用户关联租户取第一个
        var tidList = _sysUserTenantService.GetTenantIdsListByUserId(userDto.UserId);
        long actualTenantId = tidList.FirstOrDefault();
        loginUser.TenantId = actualTenantId;
        loginUser.User.TenantId = actualTenantId;

        // ========================================= 部门、租户子集 =========================================

        // 部门子集
        var deptChildIds = GetChildrenDeptById(userDto.DeptId ?? 0);
        loginUser.DeptChildId = deptChildIds;
        loginUser.User.DeptChildId = deptChildIds;

        // 租户子集
        long[] tenantChildIds;
        switch(loginUser.UserType)
        {
            case "SUPER_ADMIN":
                // 超级管理员：可访问所有租户
                tenantChildIds = GetChildrenTenantById(userDto.TenantId);
                break;

            case "GROUP_ADMIN":
                // 集团管理员：直属子租户
                tenantChildIds = GetChildrenTenantByIdAdminGroup(userDto.TenantId);
                loginUser.TenantChildId = tenantChildIds;
                loginUser.User.TenantChildId = tenantChildIds;
                return await _tokenService.CreateToken(loginUser);

            case "COMPANY_ADMIN":
                // 公司管理员：只需生成 Token
                return await _tokenService.CreateToken(loginUser);

            default:
                // 普通用户：同超级管理员处理
                tenantChildIds = GetChildrenTenantById(userDto.TenantId);
                break;
        }

        loginUser.TenantChildId = tenantChildIds;
        loginUser.User.TenantChildId = tenantChildIds;

        // TODO: 缺少角色信息的加载

        // 生成并返回 Token
        return await _tokenService.CreateToken(loginUser);




    }






    /// <summary>
    /// 登录验证--暂时没用到，备用误删
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="code">验证码</param>
    /// <param name="uuid">唯一标识</param>
    /// <param name="tenantid">登录时下拉框的组织id</param>
    /// <param name="userType">登录的类型  后台还是业务</param>
    /// <returns>结果</returns>
    public async Task<string> LoginAsyncV1(string username,string password,string code,string uuid,long tenantid,string usertype)
    {

        // =============================================== 验证 & 日志 ===============================================

        // 验证码校验
        ValidateCaptcha(username,code,uuid);
        // 登录前置校验
        LoginPreCheck(username,password);

        // 查询用户信息，映射到userDto
        var userDto = await _sysUserService.GetDtoByUsernameAsync(username);

        // 账户密码验证
        CheckLoginUser(username,password,userDto);


        //await _sysLogininforService.AddAsync(username,Constants.LOGIN_SUCCESS,MessageConstants.User_Login_Success);
        // 记录登录成功（fire-and-forget，不阻塞）
        _ = Task.Run(async ( ) =>
         {
             try
             {
                 await _sysLogininforService.AddAsync(username,Constants.LOGIN_SUCCESS,MessageConstants.User_Login_Success);
             }
             catch(Exception ex)
             {
                 _logger.LogError(ex,"异步记录登录成功日志时出错");
             }
         });


        // ================================================= 权限 & 缓存 =================================================


        //重要：创建Permissions权限 
        var loginUser = CreateLoginUser(userDto); // userDto 转化为loginUser


        // 更新用户信息的最后登录手机和ip
        await RecordLoginInfoAsync(userDto.UserId);


        // 用户类型：直接复用第一次查询的 userDto
        loginUser.UserType = userDto.UserType;
        loginUser.User.UserType = userDto.UserType;


        // 前端未传递 tid, 根据当前用户关联的组织获取
        //long currentUserId = SecurityUtils.GetUserId();
        List<long> tidList = _sysUserTenantService.GetTenantIdsListByUserId(userDto.UserId);
        long tid = tidList.FirstOrDefault();

        loginUser.TenantId = tid; //组织id  需要判断下
        loginUser.User.TenantId = tid; //组织id  需要判断下


        // =========================================  权限、部门、租户子集 =========================================


        // 子部门子集写入  新增：
        long[] septstr = GetChildrenDeptById(userDto.DeptId ?? 0);
        loginUser.DeptChildId = septstr;
        loginUser.User.DeptChildId = septstr;


        // 将组织子集写入  新增：
        long[] tidstr = new long[5];

        // 登录类型判断  备用
        if(loginUser.UserType == "SUPER_ADMIN") //超级管理员1
        {
        }
        else if(loginUser.UserType == "GROUP_ADMIN") //集团管理员2   已验证
        {
            // 整改  查询子集
            tidstr = GetChildrenTenantByIdAdminGroup(userDto.TenantId);
            loginUser.TenantChildId = tidstr;
            loginUser.User.TenantChildId = tidstr;
            return await _tokenService.CreateToken(loginUser);

        }
        else if(loginUser.UserType == "COMPANY_ADMIN") //公司管理员3
        {
            return await _tokenService.CreateToken(loginUser);

        }
        else // 普通用户4
        {
        }

        tidstr = GetChildrenTenantById(userDto.TenantId);
        loginUser.TenantChildId = tidstr;
        loginUser.User.TenantChildId = tidstr;

        // todo: 缺少角色信息

        // 将用户信息生成token,并将token和用户信息存radis缓存。
        return await _tokenService.CreateToken(loginUser);

    }




    //========================================================================================================================================================



    private void CheckLoginUser(string username,string password,SysUserDto user)
    {
        if(user == null)
        {
            _logger.LogInformation($"登录用户：{username} 不存在.");
            throw new ServiceException(MessageConstants.User_Passwrod_Not_Match);
        }
        else if(UserStatus.DELETED.GetValue().Equals(user.DelFlag))
        {
            _logger.LogInformation($"登录用户：{username} 已被删除.");
            throw new ServiceException(MessageConstants.User_Deleted);
        }
        else if(UserStatus.DISABLE.GetValue().Equals(user.Status))
        {
            _logger.LogInformation($"登录用户：{username} 已被停用.");
            throw new ServiceException(MessageConstants.User_Blocked);
        }

        // 密码验证
        _sysPasswordService.Validate(username,password,user);
    }

    /// <summary>
    /// 校验验证码
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="code">验证码</param>
    /// <param name="uuid">唯一标识</param>
    private void ValidateCaptcha(string username,string code,string uuid)
    {
        bool captchaEnabled = _sysConfigService.IsCaptchaEnabled();
        if(captchaEnabled)
        {
            // 无论验证是否通过, 都删除缓存的验证码
            var isValidCaptcha = _captcha.Validate(uuid,code,true,true);
            if(!isValidCaptcha)
            {
                _ = Task.Run(async ( ) =>
                            {
                                try
                                {
                                    await _sysLogininforService.AddAsync(username,Constants.LOGIN_FAIL,MessageConstants.Captcha_Invalid);
                                }
                                catch(Exception ex)
                                {
                                    _logger.LogError(ex,"异步记录验证码校验失败日志出错");
                                }
                            });
                throw new ServiceException(MessageConstants.Captcha_Invalid);
            }
        }
    }

    /// <summary>
    /// 登录前置校验
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">用户密码</param>
    private void LoginPreCheck(string username,string password)
    {
        // 用户名或密码为空 错误
        if(string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _ = Task.Run(async ( ) =>
         {
             try
             {
                 await _sysLogininforService.AddAsync(username,Constants.LOGIN_FAIL,MessageConstants.Required);
             }
             catch(Exception ex)
             {
                 _logger.LogError(ex,"异步记录登录校验失败日志（Required）出错");
             }
         });
            throw new ServiceException(MessageConstants.Required);
        }
        // 密码如果不在指定范围内 错误
        if(password.Length < UserConstants.PASSWORD_MIN_LENGTH || password.Length > UserConstants.PASSWORD_MAX_LENGTH)
        {
            _ = Task.Run(async ( ) =>
          {
              try
              {
                  await _sysLogininforService.AddAsync(username,Constants.LOGIN_FAIL,MessageConstants.User_Passwrod_Not_Match);
              }
              catch(Exception ex)
              {
                  _logger.LogError(ex,"异步记录登录校验失败日志（Password Length）出错");
              }
          });
            throw new ServiceException(MessageConstants.User_Passwrod_Not_Match);
        }
        // 用户名不在指定范围内 错误
        if(username.Length < UserConstants.USERNAME_MIN_LENGTH || username.Length > UserConstants.USERNAME_MAX_LENGTH)
        {
            _ = Task.Run(async ( ) =>
                    {
                        try
                        {
                            await _sysLogininforService.AddAsync(username,Constants.LOGIN_FAIL,MessageConstants.User_Passwrod_Not_Match);
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError(ex,"异步记录登录校验失败日志（Username Length）出错");
                        }
                    });
            throw new ServiceException(MessageConstants.User_Passwrod_Not_Match);
        }
        // IP黑名单校验
        string? blackStr = _cache.GetString("sys.login.blackIPList");
        if(IpUtils.IsMatchedIp(blackStr,App.HttpContext.GetRemoteIpAddressToIPv4()))
        {
            _ = Task.Run(async ( ) =>
         {
             try
             {
                 await _sysLogininforService.AddAsync(username,Constants.LOGIN_FAIL,MessageConstants.Login_Blocked);
             }
             catch(Exception ex)
             {
                 _logger.LogError(ex,"异步记录登录校验失败日志（IP Blacklist）出错");
             }
         });
            throw new ServiceException(MessageConstants.Login_Blocked);
        }
    }

    /// <summary>
    /// 重要：创建Permissions权限，也就是查看操作页面是否有权限。
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    private LoginUser CreateLoginUser(SysUserDto user)
    {
        var permissions = _sysPermissionService.GetMenuPermission(user);
        return new LoginUser
        {
            UserId = user.UserId,
            DeptId = user.DeptId ?? 0,
            UserName = user.UserName ?? "",
            Password = user.Password ?? "",
            User = user,
            Permissions = permissions,
            //新增
            //TenantId = user.TenantId,
            //UserType = user.UserType,

        };
    }

    /// <summary>
    /// 记录登录信息
    /// </summary>
    public async Task RecordLoginInfoAsync(long userId)
    {
        SysUserDto sysUser = new SysUserDto();
        sysUser.UserId = userId;
        sysUser.LoginIp = IpUtils.GetIpAddr();
        sysUser.LoginDate = DateTime.Now;
        await _sysUserService.UpdateUserLoginInfoAsync(sysUser);
    }

    /////////////////////////////new//////////////////////////////////////

    /// <summary>
    /// 获取本部门及部门下所有部门
    /// </summary>
    /// <param name="dept"></param>
    /// <returns></returns>
    public long[] GetChildrenDeptById(long deptId)
    {
        // 使用 LIKE 查询，包含三种可能性以及精确匹配 ancestors 的逻辑
        string sql = @"
        SELECT * 
        FROM sys_dept 
        WHERE 
            ancestors LIKE CONCAT('%,', @DeptId, ',%') OR 
            ancestors LIKE CONCAT(@DeptId, ',%') OR 
            ancestors LIKE CONCAT('%,', @DeptId) OR 
            ancestors = @DeptId";


        // 执行 SQL 查询并获取结果
        var list = _sqlSugarClient.Ado.SqlQuery<long>(sql,new { DeptId = deptId });
        list.Add(deptId);
        long[] res = list.ToArray();

        // 将结果转为数组并返回
        return res;
    }


    /// <summary>
    /// 获取组织子集（包括自身）
    /// </summary>
    /// <param name="tenantId">组织ID</param>
    /// <returns>子集ID数组</returns>
    public long[] GetChildrenTenantById(long tenantId)
    {
        // 使用 LIKE 查询，包含三种可能性以及精确匹配 ancestors 的逻辑
        string sql = @"
                SELECT id 
                FROM sys_tenant 
                WHERE 
                    ancestors LIKE CONCAT('%,', @TenantId , ',%') OR 
                    ancestors LIKE CONCAT(@TenantId , ',%') OR 
                    ancestors LIKE CONCAT('%,', @TenantId) OR 
                    ancestors = @TenantId OR 
                    id = @TenantId"; // 包括自身 ID

        // 执行 SQL 查询并获取结果
        var list = _sqlSugarClient.Ado.SqlQuery<long>(sql,new { TenantId = tenantId });

        // 转换为数组并返回
        return list.ToArray();
    }


    /// <summary>
    /// 获取组织子集 ，直查parent_id = tenantId
    /// </summary>
    /// <param name="tenantId">组织ID</param>
    /// <returns>子集ID数组</returns>
    public long[] GetChildrenTenantByIdAdminGroup(long tenantId)
    {
        // 修改为直接查询：满足自身（id = tenantId）或直属下级（parent_id = tenantId）
        string sql = @"SELECT id FROM sys_tenant WHERE id = @TenantId OR parent_id = @TenantId";

        var list = _sqlSugarClient.Ado.SqlQuery<long>(sql,new { TenantId = tenantId });

        return list.ToArray();
    }
}
