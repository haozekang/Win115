namespace Win115.Services
{
    public static class DownloadSettings
    {
        public const string ConcurrentTasksKey = "download_concurrent_tasks";
        public const string SegmentCountKey = "download_segment_count";
        public const string SpeedLimitKey = "download_speed_limit_kbps";

        public const int DefaultConcurrentTasks = 2;
        public const int DefaultSegmentCount = 4;
        public const int MaxConcurrentTasks = 5;
        public const int MaxSegmentCount = 8;
    }
}
