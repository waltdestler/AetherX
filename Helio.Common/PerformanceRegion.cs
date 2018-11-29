using System;
using System.Collections.Generic;
using System.Text;

namespace Helio.Common
{
    public class PerformanceRegion
    {
        public PerformanceRegion Parent;
        public List<PerformanceRegion> Children = new List<PerformanceRegion>();

        public string Name { get; set; }
        public float StartTotalElapsedSeconds { get; set; }
        public float DurationElapsedSeconds { get; set; }

        public PerformanceRegion(float currentTotalElapsedSeconds, string name, PerformanceRegion parent = null) {
            this.Parent = parent;
            this.Name = name;
            this.StartTotalElapsedSeconds = currentTotalElapsedSeconds;
        }

        public void End(float currentTotalElapsedSeconds)
        {
            this.DurationElapsedSeconds = this.StartTotalElapsedSeconds - currentTotalElapsedSeconds;
        }
    }
}
