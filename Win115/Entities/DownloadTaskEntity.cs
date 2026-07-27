using System;
using System.Collections.Generic;
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
        public int? ParentTaskId { get; set; }
        public bool IsFolder { get; set; }
        public int? TotalFiles { get; set; }
        public List<DownloadSegmentEntity>? Segments { get; set; }
        public double? Progress { get; set; }
        public string? SavePath { get; set; }
        public string? Url { get; set; }
        public string? PickCode { get; set; }
        public DownloadTaskStateEnum? State { get; set; }
        public DateTime? CreateTime { get; set; }
    }

    public class DownloadSegmentEntity
    {
        private long _downloaded;

        public int Index { get; set; }
        public long Start { get; set; }
        public long End { get; set; }
        public long Downloaded
        {
            get => System.Threading.Interlocked.Read(ref _downloaded);
            set => System.Threading.Interlocked.Exchange(ref _downloaded, value);
        }
    }
}
