using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinTail
{
    public class TailCoordinatorActor : UntypedActor
    {
        
        protected override void OnReceive(object message)
        {
            if (message is StartTail)
            {
                var msg = message as StartTail;

                // create TailActor instance as child
                Context.ActorOf(Props.Create(() => new TailActor(msg.ReporterActor, msg.FilePath)));

            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                10, //max number of retries
                TimeSpan.FromSeconds(30), //within time range
                x => //local only decider
                {
                    // Non appilication critical error, ignore and keep going.
                    if (x is ArithmeticException) return Directive.Resume;

                    // Error that we cannot recover from, stop the failing actor
                    else if (x is NotSupportedException) return Directive.Stop;

                    // In all other cases, just restart the failing actor
                    else return Directive.Restart;
                });
        }

        #region Message Types
        /// <summary>
        /// Start tailing the file at the specified path.
        /// </summary>
        public class StartTail
        {
            public StartTail(string filePath, IActorRef reporterActor)
            {
                FilePath = filePath;
                ReporterActor = reporterActor;
            }

            public string FilePath { get; private set; }
            public IActorRef ReporterActor { get; private set; }
        }

        /// <summary>
        /// Stop tailing file at the specified path.
        /// </summary>
        public class StopTail
        {
            public StopTail(string filePath)
            {
                FilePath = filePath;
            }

            public string FilePath { get; private set; }
        }
        #endregion
    }
}
