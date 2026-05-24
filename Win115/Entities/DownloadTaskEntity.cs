using System;
using Win115.Enums;

namespace Win115.Entities
{
    public class DownloadTaskEntity
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Name { get; set; }
        public long? Size { get; set; }
        public long? DownloadedSize { get; set; }
        public double? Progress { get; set; }
        public string? SavePath { get; set; }
        public string? Url { get; set; }
        public string? PickCode { get; set; }
        public DownloadTaskStateEnum? State { get; set; }
        public DateTime? CreateTime { get; set; }
    }
}
