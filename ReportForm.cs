using System;

namespace AutoReportGenerator
{
    public class ReportForm
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string Cc { get; set; } = string.Empty;
        public string Bcc { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public byte[] FileData { get; set; }
        public string FileName { get; set; } = string.Empty;
    }
} 