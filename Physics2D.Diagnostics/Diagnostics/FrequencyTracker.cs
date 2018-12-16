﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace tainicom.Aether.Physics2D.Diagnostics
{
    public class FrequencyTracker
    {
        public FrequencyTracker()
        {

        }

        public long TotalOccurences { get; private set; }
        public float TotalSeconds { get; private set; }
        public float AverageOccurrencesPerSecond { get; private set; }
        public float CurrentOccurrencesPerSecond { get; private set; }

        public const int MAXIMUM_SAMPLES = 100;

        private Queue<float> _sampleBuffer = new Queue<float>();
        Stopwatch OccurrenceStopwatch = new Stopwatch();

        public bool Update()
        {
            float elapsedSeconds = this.OccurrenceStopwatch.ElapsedMilliseconds / 1000f;

            // restart frame stopwatch
            this.OccurrenceStopwatch.Restart();

            this.CurrentOccurrencesPerSecond = 1.0f / elapsedSeconds;

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
            this.TotalSeconds += elapsedSeconds;
            return true;
        }
    }
}