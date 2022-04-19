using System;

namespace PTZAPP
{
    internal class PeriodicTimer
    {
        private TimeSpan timeSpan;

        public PeriodicTimer(TimeSpan timeSpan)
        {
            this.timeSpan = timeSpan;
        }
    }
}