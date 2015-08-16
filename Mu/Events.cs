using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mu
{
    public delegate int IntFunction();

    public enum EventType { PostEvent, PreEvent };

    public class EventManager
    {
        public class Event
        {
            //public delegate void Function();
            public IntFunction zFunc;
            public TimeSpan zNextPulseTime;
            public TimeSpan zExpirationTime;
            public TimeSpan zTick;
            public string zName;
            public IList<Event> zOwner;

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
                zNextPulseTime = Globals.GameTime.TotalGameTime + delay;
                if (life == TimeSpan.Zero)
                    zExpirationTime = TimeSpan.MaxValue;
                else
                    zExpirationTime = Globals.GameTime.TotalGameTime + life + delay;
                zFunc = func;
                zTick = tick;
                zName = name;
            }

            public void MoveToFront()
            {
                int index = zOwner.IndexOf(this);
                Event temp = zOwner[0];
                zOwner[0] = this;
                zOwner[index] = temp;
            }
        }

        private List<Event> mPostEvents;
        private List<Event> mPreEvents;
        //private List<Event> mEvents;

        /// <summary>
        /// Constructor
        /// </summary>
        public EventManager()
        {
            mPostEvents = new List<Event>();
            mPreEvents = new List<Event>();
        }

        /// <summary>
        /// Add new event
        /// </summary>
        /// <param name="func">code to execute</param>
        /// <param name="name">name, used to remove event on demand</param>
        /// <param name="delay">how long to wait before start,0 to start immediately</param>
        /// <param name="life">how long before remove, 0 to never remove</param>
        /// <param name="tick">periodic triggering, 0 to trigger every frame</param>
        public Event AddEvent(IntFunction func, string name, TimeSpan delay, TimeSpan life, TimeSpan tick, EventType type = EventType.PostEvent)
        {
            Event e = new Event(func, name, delay, life, tick);
            if (type == EventType.PostEvent)
            {
                e.zOwner = mPostEvents;
                mPostEvents.Add(e);
            }
            else if (type == EventType.PreEvent)
            {
                e.zOwner = mPreEvents;
                mPreEvents.Add(e);
            }
            return e;
        }

        /// <summary>
        /// Add new event
        /// </summary>
        /// <param name="func">code to execute</param>
        /// <param name="name">name, used to remove event on demand</param>
        /// <param name="delay">how long to wait before start,0 to start immediately</param>
        /// <param name="life">how long before remove, 0 to never remove</param>
        /// <param name="tick">periodic triggering, 0 to trigger every frame</param>
        public Event AddEvent(IntFunction func, string name, float delay=0, float life=0, float tick=0, EventType type = EventType.PostEvent)
        {
            Event e = new Event(func, name, TimeSpan.FromSeconds(delay), TimeSpan.FromSeconds(life), TimeSpan.FromSeconds(tick));
            if (type == EventType.PostEvent)
            {
                e.zOwner = mPostEvents;
                mPostEvents.Add(e);
            }
            else if (type == EventType.PreEvent)
            {
                e.zOwner = mPreEvents;
                mPreEvents.Add(e);
            }
            return e;
        }

        /// <summary>
        /// Remove event by name
        /// </summary>
        /// <param name="name"></param>
        public void RemoveEvent(string name)
        {
            Event toRemove = mPostEvents.FirstOrDefault(e => e.zName == name);
            if(toRemove == null)
                toRemove = mPreEvents.FirstOrDefault(e => e.zName == name);
            if(toRemove != null)
                toRemove.zOwner.Remove(toRemove);
        }

        /// <summary>
        /// Remove all events
        /// </summary>
        public void Clear()
        {
            mPostEvents.Clear();
            mPreEvents.Clear();
        }

        /// <summary>
        /// Routine method, that runs after screen.activity
        /// </summary>
        public void Activity(IList<Event> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Event e = list[i];
                int result = 1;
                //check for expired events
                if (Globals.GameTime.TotalGameTime >= e.zExpirationTime)
                {
                    list.RemoveAt(i);
                    continue;
                }
                //check if ready to trigger
                if (Globals.GameTime.TotalGameTime >= list[i].zNextPulseTime)
                {
                    e.zNextPulseTime += e.zTick;
                    result = list[i].zFunc();
                }
                //check if it want to remove itself
                if (result == 0)
                {
                    list.RemoveAt(i);
                    continue;
                }
            }
        }

        public void PreActivity()
        {
            Activity(mPreEvents);
        }

        public void PostActivity()
        {
            Activity(mPostEvents);
        }
    }
}
