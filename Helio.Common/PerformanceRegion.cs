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
        public float StartTotalElapsedMilliseconds { get; set; }
        public float DurationElapsedMilliseconds { get; set; }

        public PerformanceRegion(float currentTotalElapsedSeconds, string name, PerformanceRegion parent = null) {

            if (parent != null)
            {
                this.Parent = parent;
                this.Parent.Children.Add(this);
            }
            this.Name = name;
            this.StartTotalElapsedMilliseconds = currentTotalElapsedSeconds;
        }

        public void End(float currentTotalElapsedMilliseconds)
        {
            this.DurationElapsedMilliseconds = currentTotalElapsedMilliseconds - this.StartTotalElapsedMilliseconds;
        }

        public void AddDescriptionLines(List<string> details)
        {
            // add my lines! 
            string myDescription = string.Format( "{0} - {1}ms", this.Name, this.DurationElapsedMilliseconds);
            details.Add(myDescription);

            foreach( var child in this.Children)
            {
                // recursively call children
                child.AddDescriptionLines(details);
            }
        }
    }
}
