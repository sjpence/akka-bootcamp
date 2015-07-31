using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Akka.Actor;

namespace ChartApp.Actors
{
    #region Reporting
    /// <summary>
    /// Signal used to indicate it is time to sample all counters
    /// </summary>
    public class GatherMetrics
    {
    }

    /// <summary>
    /// Metric data at the time of sample
    /// </summary>
    public class Metric
    {
        public Metric(string series, float counterValue)
        {
            CounterValue = counterValue;
            Series = series;
        }

        public float CounterValue { get; private set; }
        public string Series { get; private set; }
    }

    #endregion

    #region Performance Counter Management
    /// <summary>
    /// Types of counters supported
    /// </summary>
    public enum CounterType
    {
        Cpu,
        Memory,
        Disk
    }

    /// <summary>
    /// Enables a counter and begins publishing values to <see cref="Subscriber"/>
    /// </summary>
    public class SubscriberCounter
    {
        public SubscriberCounter(CounterType counter, IActorRef subscriber)
        {
            Subscriber = subscriber;
            Counter = counter;
        }

        public CounterType Counter { get; private set; }
        public IActorRef Subscriber { get; private set; }
    }

    /// <summary>
    /// Unsubscribes <see cref="Subscriber"/> from receining updates for a given counter.
    /// </summary>
    public class UnsubscribeCounter
    {
        public UnsubscribeCounter(CounterType counter, IActorRef subscriber)
        {
            Subscriber = subscriber;
            Counter = counter;
        }

        public CounterType Counter { get; private set; }
        public IActorRef Subscriber { get; private set; }
    }
    #endregion
}
