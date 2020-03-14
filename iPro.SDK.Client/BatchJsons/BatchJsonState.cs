using System;
using System.Collections.Generic;
using System.Linq;

namespace iPro.SDK.Client.BatchJsons
{
    public class BatchJsonState
    {
        public string LogFilePath { get; set; }
        public string ApiEndpoint { get; set; }

        public bool IsScanning { get; set; }
        public int ScannedCount { get; set; }
        public Exception ScannedError { get; set; }

        public bool IsRunning { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public int TotalCount { get; set; }
        public int FailedCount { get; set; }
        public int SuccessCount { get; set; }

        public IEnumerable<string> SelectedFiles { get; set; }
        public bool HasSelectedFiles => SelectedFiles != null && SelectedFiles.Any();
    }
}
