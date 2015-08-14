using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mu
{
    public delegate int IntFunction();    

    public class EventManager
    {
        private class Event
        {
            //public delegate void Function();
            public IntFunction Func;
            public TimeSpan NextPulseTime;
            public TimeSpan ExpirationTime;
            public TimeSpan Tick;
            public string Name;

            /// <summary>
            /// Generic constructor
            /// </summary>
            /// <param name="func">code to execute</param>
            /// <param name="name">name, used to remove event on demand</param>
            /// <param name="delay">how long to wait before start,0 to start immediately</param>
            /// <param name="life">how long before remove, 0 to never remove</param>
            /// <param name="tick">periodic triggering, 0 to trigger every frame</param>
            public Event(IntFunction func, string name, TimeSpan delay, TimeSpan life, TimeSpan tick)
            {
                NextPulseTime = Globals.GameTime.TotalGameTime + delay;
                if (life == TimeSpan.Zero)
                    ExpirationTime = TimeSpan.MaxValue;
                else
                    ExpirationTime = Globals.GameTime.TotalGameTime + life + delay;
                Func = func;
                Tick = tick;
                Name = name;
            }
        }

        private List<Event> mEvents;

        /// <summary>
        /// Constructor
        /// </summary>
        public EventManager()
        {
            mEvents = new List<Event>();
        }

        /// <summary>
        /// Add new event
        /// </summary>
        /// <param name="func">code to execute</param>
        /// <param name="name">name, used to remove event on demand</param>
        /// <param name="delay">how long to wait before start,0 to start immediately</param>
        /// <param name="life">how long before remove, 0 to never remove</param>
        /// <param name="tick">periodic triggering, 0 to trigger every frame</param>
        public void AddEvent(IntFunction func, string name, TimeSpan delay, TimeSpan life, TimeSpan tick)
        {
            mEvents.Add(new Event(func, name, delay, life, tick));
        }

        /// <summary>
        /// Remove event by name
        /// </summary>
        /// <param name="name"></param>
        public void RemoveEvent(string name)
        {
            Event toRemove = mEvents.First(e => e.Name == name);
            mEvents.Remove(toRemove);
        }

        /// <summary>
        /// Remove all events
        /// </summary>
        public void Clear()
        {
            mEvents.Clear();
        }

        /// <summary>
        /// Routine method
        /// </summary>
        public void Activity()
        {
            for (int i = mEvents.Count - 1; i >= 0; i--)
            {
                Event e = mEvents[i];
                int result = 1;
                //check for expired events
                if (Globals.GameTime.TotalGameTime >= e.ExpirationTime)
                {
                    mEvents.RemoveAt(i);
                    continue;
                }
                //check if ready to trigger
                if (Globals.GameTime.TotalGameTime >= mEvents[i].NextPulseTime)
                {
                    e.NextPulseTime += e.Tick;
                    result = mEvents[i].Func();
                }
                //check if it want to remove itself
                if (result == 0)
                {
                    mEvents.RemoveAt(i);
                    continue;
                }
            }
        }
    }
}
