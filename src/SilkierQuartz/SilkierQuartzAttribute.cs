﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SilkierQuartz
{
    public class SilkierQuartzAttribute : Attribute
    {
        public SilkierQuartzAttribute()
        {

        }
        public SilkierQuartzAttribute(double days, double hours, double minutes, double seconds, double milliseconds, string _identity, string _desciption) : this(days, hours, minutes, seconds, milliseconds, 0, _identity, _desciption)
        {
        }

        public SilkierQuartzAttribute(double hours, double minutes, double seconds, string _identity, string _desciption) : this(0, hours, minutes, seconds, 0, 0, _identity, _desciption)
        {
        }

        public SilkierQuartzAttribute(double minutes, double seconds, string _identity, string _desciption) : this(0, 0, minutes, seconds, 0, 0, _identity, _desciption)
        {
        }
        public SilkierQuartzAttribute(double seconds, string _identity, string _desciption) : this(0, 0, 0, seconds, 0, 0, _identity, _desciption)
        {
        }


        public SilkierQuartzAttribute(double days, double hours, double minutes, double seconds, double milliseconds) : this(days, hours, minutes, seconds, milliseconds, 0, null, null)
        {
        }

        public SilkierQuartzAttribute(double hours, double minutes, double seconds) : this(0, hours, minutes, seconds, 0, 0, null, null)
        {
        }

        public SilkierQuartzAttribute(double minutes, double seconds) : this(0, 0, minutes, seconds, 0, 0, null, null)
        {
        }
        public SilkierQuartzAttribute(double seconds) : this(0, 0, 0, seconds, 0, 0, null, null)
        {
        }



        public SilkierQuartzAttribute(double days, double hours, double minutes, double seconds, double milliseconds, long ticks, string _identity, string _desciption)
        {

            WithInterval = TimeSpan.FromTicks(ticks + (long)(days * TimeSpan.TicksPerDay
                                             + hours * TimeSpan.TicksPerHour
                                             + minutes * TimeSpan.TicksPerMinute
                                             + seconds * TimeSpan.TicksPerSecond
                                             + milliseconds + TimeSpan.TicksPerMillisecond));
        }
        public string Desciption { get; set; } = null;
        public string Identity { get; set; } = null;
        public TimeSpan WithInterval { get; set; }
        public DateTimeOffset StartAt { get; set; } = DateTimeOffset.MinValue;
        public int RepeatCount { get; set; } = 0;
        public string TriggerName { get; set; } = string.Empty;
        public string TriggerGroup { get; set; } = string.Empty;
        public string TriggerDescription { get; set; } = string.Empty;
        public int Priority { get; set; } = 0;
        public bool RequestRecovery { get; set; } = true;
    }
}
