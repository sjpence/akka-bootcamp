using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Akka.Actor;
using System.Diagnostics;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;

namespace ChartApp.Actors
{
    /// <summary>
    /// Actor Responsible for translatin UI Calls into ActorSystem Messages
    /// </summary>
    public class PerformanceCounterCoordinatorActor : ReceiveActor
    {
        #region Message Types
        /// <summary>
        /// Subscribe the <see cref="ChartingActor"/> to updates for <see cref="Counter"/>
        /// </summary>
        public class Watch
        {
            public Watch(CounterType counter)
            {
                Counter = counter;
            }

            public CounterType Counter { get; private set; }
        }

        /// <summary>
        /// Unsubscribe the <see cref="ChartingActor"/> from updates for <see cref="Counter"/>
        /// </summary>
        public class UnWatch
        {
            public UnWatch(CounterType counter)
            {
                Counter = counter;
            }

            public CounterType Counter { get; private set; }
        }
        #endregion

        /// <summary>
        /// Dictionary of methods for generating new instances of all <see cref="PerformanceCounter"/>s for monitoring.
        /// </summary>
        private static readonly Dictionary<CounterType, Func<PerformanceCounter>> CounterGenerators =
            new Dictionary<CounterType, Func<PerformanceCounter>>()
            {
                {CounterType.Cpu, () => new PerformanceCounter("Processor", "% Processor Time", "_Total", true)},
                {CounterType.Memory, () => new PerformanceCounter("Memory", "% Committed Bytes In Use", true)},
                {CounterType.Disk, () => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true)}
            };

        private static readonly Dictionary<CounterType, Func<Series>> CounterSeries =
            new Dictionary<CounterType, Func<Series>>()
            {
                {CounterType.Cpu, () =>
                new Series(CounterType.Cpu.ToString()) { ChartType=SeriesChartType.SplineArea, Color=Color.DarkGreen}},
                {CounterType.Memory, () =>
                new Series(CounterType.Memory.ToString()) { ChartType=SeriesChartType.FastLine, Color=Color.MediumBlue}},
                {CounterType.Disk, () =>
                new Series(CounterType.Disk.ToString()) { ChartType=SeriesChartType.SplineArea, Color=Color.DarkRed }}
            };

        private IActorRef _chartingActor;
        private Dictionary<CounterType, IActorRef> _counterActors;

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor) :
            this(chartingActor, new Dictionary<CounterType, IActorRef>())
        {

        }
        public PerformanceCounterCoordinatorActor(IActorRef chartingActor, Dictionary<CounterType, IActorRef> counterActors)
        {
            _chartingActor = chartingActor;
            _counterActors = counterActors;

            Receive<Watch>(watch => 
            {
                if (!_counterActors.ContainsKey(watch.Counter))
                {
                    //creat a child actor to monitor this conter if it doesn't already exist
                    var counterActor = Context.ActorOf(Props.Create(() => 
                        new PerformanceCounterActor(watch.Counter.ToString(), CounterGenerators[watch.Counter])));

                    //add new counter to index
                    _counterActors[watch.Counter] = counterActor;
                }

                //register series with the ChartingActor
                _chartingActor.Tell(new ChartingActor.AddSeries(CounterSeries[watch.Counter]()));

                //tell counter actor to begin publishing its statistics to the ChartingActor
                _counterActors[watch.Counter].Tell(new SubscriberCounter(watch.Counter, _chartingActor));
            });

            Receive<UnWatch>(unwatch =>
            {
                if (!_counterActors.ContainsKey(unwatch.Counter))
                {
                    // counter not in index
                    return;
                }

                // unsubscribe the ChartingActor from the counter updates
                _counterActors[unwatch.Counter].Tell(new UnsubscribeCounter(unwatch.Counter, _chartingActor));

                //remove series from ChartingActor
                _chartingActor.Tell(new ChartingActor.RemoveSeries(unwatch.Counter.ToString()));
            });
        }
    }
}
