namespace RuoYi.Data
{
    public class CacheConstants
    {
        /// <summary>
        /// 登录用户 redis key
        /// </summary>
        public const string LOGIN_TOKEN_KEY = "login_tokens:";

        /// <summary>
        /// 参数管理 cache key
        /// </summary>
        public const string SYS_CONFIG_KEY = "sys_config:";

        /// <summary>
        /// 验证码 redis key
        /// </summary>
        public const string CAPTCHA_CODE_KEY = "captcha_codes:";

        /// <summary>
        /// 登录账户密码错误次数 redis key
        /// </summary>
        public const string PWD_ERR_CNT_KEY = "pwd_err_cnt:";

        /// <summary>
        /// 字典管理 cache key
        /// </summary>
        public const string SYS_DICT_KEY = "sys_dict:";



        /// <summary>
        /// 脱硝的matt消息列表暂存
        /// </summary>
        public const string TX_MQTT_Message = "tx_mqtt_message:";

        /// <summary>
        /// 脱硝的大屏信息暂存
        /// </summary>
        public const string TX_DataView_Info = "tx_DataView_info:";

        /// <summary>
        /// 设备点位列表缓存前缀
        /// </summary>
        public const string IOT_POINT_MAP_KEY = "iot_point_map:";

        /// <summary>
        /// 设备变量映射缓存前缀
        /// </summary>
        public const string IOT_VAR_MAP_KEY = "iot_var_map:";

    }
}
