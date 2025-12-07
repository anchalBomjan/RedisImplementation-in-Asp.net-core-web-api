namespace RedisCrudAPI.Settings
{
    public class RedisSettings
    {
        public string InstanceName { get; set; } = string.Empty;
        public int SlidingExpirationMinutes { get; set; } = 5;
        public int AbsoluteExpirationMinutes { get; set; } = 60;
        public int CacheTimeoutSeconds { get; set; } = 30;
    }
}
