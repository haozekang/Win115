namespace Win115.Services
{
    public static class ApiSettings
    {
        public const string RateLimitKey = "api_rate_limit";

        public const int DefaultRateLimit = 2;
        public const int MaxRateLimit = 20;
        public const int MaxRateLimitRetries = 5;
        public const int BackoffBaseMilliseconds = 3000;
        public const int BackoffMaxMilliseconds = 60000;
    }
}
