 

## 平台简介
 
 
## 注意事项
- 目前默认一个用户一个角色！ 多角色还没有正式测试和优化！



## 关于数据库批量新增报错问题
- SqlSugar 的 MySQL BulkCopy 依赖于该LOAD DATA LOCAL INFILE功能。
- 因此，当 MySQL 的全局变量被禁用（设置为 0）时，就会发生故障local_infile。使用 SqlSugar 进行批量插入需要连接字符串选项AllowLoadLocalInfile=true和服务器变量local_infile=1。运行：
 


## 多租户改造

 
	/getRouters 方法根据登录用户的 userType 来给路由的 Component 字段添加前缀，从而让前端在 src/views 不同文件夹下加载对应的页面。例如：

	SUPER_ADMIN 会给组件路径加上 sys_manage/

	GROUP_ADMIN 加上 sys_manage_group/

	COMPANY_ADMIN 加上 sys_manage_company/

	普通用户则保留默认路径

							
	测试账号：
	集团管理：GROUP_ADMIN 、 123456
	公司管理：jinniu    、123456
    公司员工：张三三  、 123456
 
	【平台（超级管理员）】
	 └─【集团（租户）】
		  ├─【集团管理员】
		  ├─【集团用户】
		  ├─【公司A（总公司）】
		  │    ├─【公司管理员】
		  │    └─【公司用户】
		  └─【公司B】
			   ├─【公司管理员】
			   └─【公司用户】



		   -----------------------------------------


## 账号/密码: admin/admin123
数据库： RuoYi.Admin下的appsettings.json中的appsettings.Development设置（目前腾讯云106.52.194.56）
新模块： 需要在RuoYi.Admin中引入依赖(新建控制器的访问 internal，而不是 public，否则404)
新模块： 目前新模块serve等服务 ，不需要在admin中Startup中的容器添加实例！


## 设置匿名：
- 1、添加特性[AllowAnonymous]
- 2、访问后端端口，而不是前端端口8081！ http://localhost:5000/Zk/TuoXiao/list

## 定时任务：
- 需要去 RuoYi.Quartz.Tasks文件下写定时调用方法，其他模块不行，应该是依赖注入需要修改。
- 前端添加定时器时候，后端必须在RuoYi.Quartz.Tasks文件夹有必须有个类 并且添加了注释 [Task("ryTaskcs")]
- 然后需要和 前端调用方法输入框中填入的ryTaskcs.RyNoParams1 映射一致，不然就报白名单错误！
- 白名单设置： "JobWhiteListAssembly": "RuoYi",//白名单
- 定时器中的任务服务，比如Task下的RyTask，都是通过反射创建的，并不是容器！！！
- 定时器调用其他模块的东西 ，因为设计反射，有点棘手啊
- 千万不要使用返回，不然定时器会停止！！！！！！！！！具体我也不明白
- 如果实例是反射的 ，应该从DI容器中取。

## 发布：
- 发布需要设置RuoYi.Admi下Startup中AddHttpClient的ip http地址!


## 结构

RuoYi.Admin ： 主启动及配置等
RuoYi.Common： 主通用的一些方法（可能不是一个类可能是配着特定的文件夹才能实现功能），枚举等
RuoYi.Data  ： 静态表，静态类


## RuoYi.Admin中的配置文件

- Program.cs：程序的入口点，负责创建 WebApplication / Host，进行宿主级别的配置（例如 Kestrel 设置、使用自定义 ServiceProvider 工厂、注册后台服务等），最后调用 Run 启动应用。
- Startup.cs：负责应用层的配置，主要包含 ConfigureServices（在这里向依赖注入容器注册服务）以及 Configure（设置 HTTP 请求管道，包括各种中间件的顺序）


-  ### 设备数据历史记录
新的设备读数保存在`iot_device_variable_history`中，而最新值保留在`iot_device_variable.current_value`中。每次更新都会插入历史记录并更新当前值。


 
## 内置功能(同若依)

1.  用户管理：用户是系统操作者，该功能主要完成系统用户配置。
2.  部门管理：配置系统组织机构（公司、部门、小组），树结构展现支持数据权限。
3.  岗位管理：配置系统用户所属担任职务。
4.  菜单管理：配置系统菜单，操作权限，按钮权限标识等。
5.  角色管理：角色菜单权限分配、设置角色按机构进行数据范围权限划分。
6.  字典管理：对系统中经常使用的一些较为固定的数据进行维护。
7.  参数管理：对系统动态配置常用参数。
8.  通知公告：系统通知公告信息发布维护。
9.  操作日志：系统正常操作日志记录和查询；系统异常信息日志记录和查询。
10. 登录日志：系统登录日志记录查询包含登录异常。
11. 在线用户：当前系统中活跃用户状态监控。
12. 定时任务：在线（添加、修改、删除)任务调度包含执行结果日志。
13. 代码生成：前后端代码的生成（.net、html、js、sql）支持CRUD下载 。
14. 系统接口：根据业务代码自动生成相关的api接口文档。
15. 服务监控：监视当前系统CPU、内存、磁盘、堆栈等相关信息。
16. 缓存监控：对系统的缓存查询，删除、清空等操作。
17. 在线构建器：拖动表单元素生成相应的HTML代码。
18. 连接池监视：监视当前系统数据库连接池状态，可进行分析SQL找出系统性能瓶颈。(暂无)



