using System;
using System.Collections.Generic;
using System.Text;

namespace Helio.Common
{
    public class PerformanceTree
    {
        FrequencyTracker fps = new FrequencyTracker();

        private float CurrentTotalSeconds { get; set; }
        PerformanceRegion RootPerformanceRegion { get; set; }
        PerformanceRegion CurrentPerformanceRegion { get; set; }

        public void Start(string name = "Game Loop")
        {
            fps.Update();

            this.RootPerformanceRegion = new PerformanceRegion(this.fps.TotalElapsedSeconds, name);

            this.CurrentPerformanceRegion = this.RootPerformanceRegion;
        }

        public void StartRegion(string name)
        {

            var newRegion = new PerformanceRegion(this.fps.TotalElapsedSeconds, name, this.CurrentPerformanceRegion);
            this.CurrentPerformanceRegion = newRegion;
        }

        public void EndRegion()
        {
            this.CurrentPerformanceRegion.End(this.fps.TotalElapsedSeconds);
            this.CurrentPerformanceRegion = this.CurrentPerformanceRegion.Parent;
        }

        public void End()
        {
            
        }
    }
}
