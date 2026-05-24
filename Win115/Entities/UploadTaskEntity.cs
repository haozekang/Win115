using System;
using Win115.Enums;

namespace Win115.Entities
{
    public class UploadTaskEntity
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? FileId { get; set; }
        public string? ParentId { get; set; }
        public string? Name { get; set; }
        public long? Size { get; set; }
        public double? Progress { get; set; }
        public string? FilePath { get; set; }
        public string? Bucket { get; set; }
        public string? Object { get; set; }
        public string? Endpoint { get; set; }
        public string? Region { get; set; }
        public string? PickCode { get; set; }
        public UploadTaskStateEnum? State { get; set; }
        public DateTime? CreateTime { get; set; }
    }
}
