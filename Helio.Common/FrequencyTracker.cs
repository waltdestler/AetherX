using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Helio.Common
{
    public class FrequencyTracker
    {
        public FrequencyTracker()
        {

        }

        public long TotalOccurences { get; private set; }
        public float TotalElapsedSeconds { get; private set; }
        public float AverageOccurrencesPerSecond { get; private set; }
        public float CurrentOccurrencesPerSecond { get; private set; }

        public const int MAXIMUM_SAMPLES = 100;

        private Queue<float> _sampleBuffer = new Queue<float>();
        Stopwatch OccurrenceStopwatch = new Stopwatch();

        public float CurrentElapsedSeconds
        {
            get
            {
                return this.OccurrenceStopwatch.ElapsedMilliseconds / 1000f;
            }
        }

        public bool Update()
        {
            // restart frame stopwatch
            this.OccurrenceStopwatch.Restart();

            this.CurrentOccurrencesPerSecond = 1.0f / this.CurrentElapsedSeconds;

            _sampleBuffer.Enqueue(this.CurrentOccurrencesPerSecond);

            if (_sampleBuffer.Count > MAXIMUM_SAMPLES)
            {
                _sampleBuffer.Dequeue();
                this.AverageOccurrencesPerSecond = _sampleBuffer.Average(i => i);
            }
            else
            {
                this.AverageOccurrencesPerSecond = this.CurrentOccurrencesPerSecond;
            }

            this.TotalOccurences++;
            this.TotalElapsedSeconds += this.CurrentElapsedSeconds;
            return true;
        }
    }

    // allow adding nested "regions" with names. start has name, end doesn't.
    // when start called... remember elapsed seconds... when end... see new elapsed, take diff...
    // each level's % is taking (my time) / (parent total elapsed sec)
    // going to have to be a tree of sorts... each region can have sub regions...

    // in game loop....
    // performanceTree.start()

        // in draw update....
        // performanceTree.start("draw")


        // at end of draw...
        // performanceTree.end()

        // in update....
        // performanceTree.start("update")
           
            // in update broadphase
            // performanceTree.start("update")

            // at end update broadphase
            // performanceTree.end()

            // remainder shown too as "other"

        // at end of update...
        // performanceTree.end()

    // end game loop....
    // performanceTree.end()

    // most useful would be % of overall frame for every tier.

}
