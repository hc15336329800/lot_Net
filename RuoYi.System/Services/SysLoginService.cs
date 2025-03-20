using System.Security.Cryptography;
using Lazy.Captcha.Core;
using RuoYi.Common.Constants;
using RuoYi.Common.Enums;
using RuoYi.Data.Enums;
using RuoYi.Data.Models;
using RuoYi.Framework.Cache;
using RuoYi.Framework.Exceptions;
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


    public SysLoginService(ISqlSugarClient sqlSugarClient,ILogger<SysLoginService> logger,ICaptcha captcha,
        ICache cache,TokenService tokenService,
        SysUserService sysUserService,SysConfigService sysConfigService,
        SysLogininforService sysLogininforService,SysPasswordService sysPasswordService,
        SysPermissionService sysPermissionService)
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

    }

    /// <summary>
    /// 登录验证
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="code">验证码</param>
    /// <param name="uuid">唯一标识</param>
    /// <param name="tenantid">登录时下拉框的组织id</param>
    /// <param name="userType">登录的类型  后台还是业务</param>
    /// <returns>结果</returns>
    public async Task<string> LoginAsync(string username,string password,string code,string uuid,long tenantid,string usertype)
    {
        // 验证码校验
        ValidateCaptcha(username,code,uuid);
        // 登录前置校验
        LoginPreCheck(username,password);

        // 查询用户信息，映射到userDto
        var userDto = await _sysUserService.GetDtoByUsernameAsync(username);

        // 账户密码验证
        CheckLoginUser(username,password,userDto);

        // 记录登录成功
        await _sysLogininforService.AddAsync(username,Constants.LOGIN_SUCCESS,MessageConstants.User_Login_Success);

        //重要：创建Permissions权限 
        var loginUser = CreateLoginUser(userDto); // userDto 转化为loginUser


        //************************  User用户信息，写入缓存 ***************************************
 

        // 更新用户信息的最后登录手机和ip
        await RecordLoginInfoAsync(userDto.UserId);

        // 用户类型
        var ust = await _sysUserService.GetDtoByUsernameAsync(username); // 查询用户
        loginUser.UserType = ust.UserType;
        loginUser.User.UserType = ust.UserType;

       

        //组织id, 前端页面带过来的。
        // 修改逻辑：登录后默认使用集合组织的1号组织，可切换
        loginUser.TenantId = tenantid; //组织id  需要判断下
        loginUser.User.TenantId = tenantid; //组织id  需要判断下

        // 子部门子集写入  新增：
        long[] septstr = GetChildrenDeptByIdAsync(userDto.DeptId ?? 0);
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
                Task.Factory.StartNew(async ( ) =>
                {
                    await _sysLogininforService.AddAsync(username,Constants.LOGIN_FAIL,MessageConstants.Captcha_Invalid);
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
            Task.Factory.StartNew(async ( ) =>
            {
                await _sysLogininforService.AddAsync(username,Constants.LOGIN_FAIL,MessageConstants.Required);
            });
            throw new ServiceException(MessageConstants.Required);
        }
        // 密码如果不在指定范围内 错误
        if(password.Length < UserConstants.PASSWORD_MIN_LENGTH || password.Length > UserConstants.PASSWORD_MAX_LENGTH)
        {
            Task.Factory.StartNew(async ( ) =>
            {
                await _sysLogininforService.AddAsync(username,Constants.LOGIN_FAIL,MessageConstants.User_Passwrod_Not_Match);
            });
            throw new ServiceException(MessageConstants.User_Passwrod_Not_Match);
        }
        // 用户名不在指定范围内 错误
        if(username.Length < UserConstants.USERNAME_MIN_LENGTH || username.Length > UserConstants.USERNAME_MAX_LENGTH)
        {
            Task.Factory.StartNew(async ( ) =>
            {
                await _sysLogininforService.AddAsync(username,Constants.LOGIN_FAIL,MessageConstants.User_Passwrod_Not_Match);
            });
            throw new ServiceException(MessageConstants.User_Passwrod_Not_Match);
        }
        // IP黑名单校验
        string? blackStr = _cache.GetString("sys.login.blackIPList");
        if(IpUtils.IsMatchedIp(blackStr,App.HttpContext.GetRemoteIpAddressToIPv4()))
        {
            Task.Factory.StartNew(async ( ) =>
            {
                await _sysLogininforService.AddAsync(username,Constants.LOGIN_FAIL,MessageConstants.Login_Blocked);
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
            UserId = user.UserId ,
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
    public long[] GetChildrenDeptByIdAsync(long deptId)
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
