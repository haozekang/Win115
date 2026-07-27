namespace Win115.Services
{
    public static class UploadSettings
    {
        public const string MaxRetryKey = "upload_max_retry";
        public const string MaxConcurrentTasksKey = "upload_max_concurrent_tasks";

        public const int DefaultMaxRetry = 3;
        public const int DefaultMaxConcurrentTasks = 5;
        public const int MaxRetry = 10;
        public const int MaxConcurrentTasks = 10;
    }
}
