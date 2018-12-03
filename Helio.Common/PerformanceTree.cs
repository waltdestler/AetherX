using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Helio.Common
{
    public class PerformanceTree
    {
        Stopwatch PerformanceStopwatch = new Stopwatch();
        PerformanceRegion RootPerformanceRegion { get; set; }
        PerformanceRegion CurrentPerformanceRegion { get; set; }
        public bool Enabled = true;

        public void Start(string name = "Game Loop")
        {
            if (!this.Enabled)
                return;

            this.PerformanceStopwatch.Restart();

            // start the root region.
            this.RootPerformanceRegion = new PerformanceRegion(this.PerformanceStopwatch.ElapsedMilliseconds, name);
            this.CurrentPerformanceRegion = this.RootPerformanceRegion;
        }

        public void StartRegion(string name)
        {
            if (!this.Enabled)
                return;

            var newRegion = new PerformanceRegion(this.PerformanceStopwatch.ElapsedMilliseconds, name, this.CurrentPerformanceRegion);
            this.CurrentPerformanceRegion = newRegion;
        }

        public void EndRegion()
        {
            if (!this.Enabled)
                return;

            this.CurrentPerformanceRegion.End(this.PerformanceStopwatch.ElapsedMilliseconds);
            this.CurrentPerformanceRegion = this.CurrentPerformanceRegion.Parent;
        }

        public void End()
        {
            // end the root region.
            if (this.RootPerformanceRegion != null)
            {
                this.RootPerformanceRegion.End(this.PerformanceStopwatch.ElapsedMilliseconds);
            }

            this.StorePerformanceDetailLines();
            this.PerformanceStopwatch.Stop();
        }

        public List<string> PerformanceDetailLines = new List<string>();

        private void StorePerformanceDetailLines()
        {
            if (!this.Enabled || RootPerformanceRegion == null)
                return;

            var detailLines = new List<string>();
            this.RootPerformanceRegion.AddDescriptionLines(detailLines);
            this.PerformanceDetailLines = detailLines;
        }
    }
}
