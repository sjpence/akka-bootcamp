using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Akka.Actor;
using System.Diagnostics;

namespace ChartApp.Actors
{
    public class PerformanceCounterActor : UntypedActor
    {
        private readonly Cancelable _cancelPublishing;
        private readonly Func<PerformanceCounter> _performanceCounterGenerator;
        private readonly string _seriesName;
        private readonly HashSet<IActorRef> _subscriptions;
        private PerformanceCounter _counter;

        public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> performanceCounterGenerator)
        {
            _seriesName = seriesName;
            _performanceCounterGenerator = performanceCounterGenerator;
            _subscriptions = new HashSet<IActorRef>();
            _cancelPublishing = new Cancelable(Context.System.Scheduler);
            
        }

        #region Actor Lifecycle Methods
        protected override void PreStart()
        {
            //create new instance of performance counter
            _counter = _performanceCounterGenerator();

            //schedule gathering of metrics
            Context.System.Scheduler.ScheduleTellRepeatedly(
                TimeSpan.FromMilliseconds(250), 
                TimeSpan.FromMilliseconds(250), 
                Self, 
                new GatherMetrics(), 
                Self, 
                _cancelPublishing);

        }
        protected override void PostStop()
        {
            try
            {
                _cancelPublishing.Cancel(false);
                _counter.Dispose();
            }
            catch
            {
            }
            finally
            {
                base.PostStop();
            }
        }
        #endregion

        protected override void OnReceive(object message)
        {
            if (message is GatherMetrics)
            {
                // publish latest counter to all subscribers
                var metric = new Metric(_seriesName, _counter.NextValue());
                foreach (var sub in _subscriptions)
                {
                    sub.Tell(metric);
                }
            }
            else if (message is SubscriberCounter)
            {
                // add a subscription (parent's job to filter counter type)
                var sc = message as SubscriberCounter;
                _subscriptions.Add(sc.Subscriber);
            }
            else if (message is UnsubscribeCounter)
            {
                // remove a subscription
                var uc = message as UnsubscribeCounter;
                _subscriptions.Remove(uc.Subscriber);
            }
        }
    }
}
