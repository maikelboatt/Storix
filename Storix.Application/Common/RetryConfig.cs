using System;

namespace Storix.Application.Common
{
    public class RetryConfig
    {
        public int MaxRetries { get; set; } = 3;
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(500);
        public double BackoffMultiplier { get; set; } = 2.0;
        public bool EnableRetryForTimeouts { get; set; } = true;
        public bool EnableRetryForConnectionFailures { get; set; } = true;
    }
}
