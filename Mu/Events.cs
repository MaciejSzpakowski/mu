using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mu
{
    public delegate int IntFunction();

    public enum EventType { PostEvent, PreEvent };

    public class Event
    {
        //public delegate void Function();
        public IntFunction zFunc;
        public TimeSpan zNextPulseTime;
        public TimeSpan zExpirationTime;
        public TimeSpan zTick;
        public string zName;
        public bool zSuspended;
        public bool zCollect;
        public EventType zType;

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
            zCollect = false;
            zSuspended = false;
            zNextPulseTime = Globals.GameTime.TotalGameTime + delay;
            if (life == TimeSpan.Zero)
                zExpirationTime = TimeSpan.MaxValue;
            else
                zExpirationTime = Globals.GameTime.TotalGameTime + life + delay;
            zFunc = func;
            zTick = tick;
            zName = name;
        }
    }

    public class EventManager
    {
        private List<Event> zEvents;
        private List<Event> zNewFrontEvents;

        /// <summary>
        /// Constructor
        /// </summary>
        public EventManager()
        {
            zEvents = new List<Event>();
            zNewFrontEvents = new List<Event>();
        }

        /// <summary>
        /// Add new event
        /// </summary>
        /// <param name="func">code to execute</param>
        /// <param name="name">name, used to remove event on demand</param>
        /// <param name="delay">how long to wait before start,0 to start immediately</param>
        /// <param name="life">how long before remove, 0 to never remove</param>
        /// <param name="tick">periodic triggering, 0 to trigger every frame</param>
        public Event AddEvent(IntFunction func, string name, bool front, TimeSpan delay, TimeSpan life, TimeSpan tick, EventType type = EventType.PostEvent)
        {
            Event e = new Event(func, name, delay, life, tick);
            e.zType = type;
            if (front)
                zNewFrontEvents.Add(e);
            else
                zEvents.Add(e);
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
        public Event AddEvent(IntFunction func, string name, bool front = false, float delay=0, float life=0, float tick=0, EventType type = EventType.PostEvent)
        {
            Event e = new Event(func, name, TimeSpan.FromSeconds(delay), TimeSpan.FromSeconds(life), TimeSpan.FromSeconds(tick));
            e.zType = type;
            if (front)
                zNewFrontEvents.Add(e);
            else
                zEvents.Add(e);
            return e;
        }

        /// <summary>
        /// Remove event by name, nothing happens if it doesnt exist
        /// </summary>
        /// <param name="name"></param>
        public void RemoveEvent(string name)
        {
            Event toRemove = zEvents.FirstOrDefault(e => e.zName == name);
            if(toRemove != null)
                toRemove.zCollect = true;
        }

        /// <summary>
        /// Remove event
        /// </summary>
        /// <param name="name"></param>
        public void RemoveEvent(Event e)
        {
            if(e != null)
                e.zCollect = true;
        }

        /// <summary>
        /// Remove all events
        /// </summary>
        public void Clear()
        {
            zEvents.Clear();
        }

        /// <summary>
        /// Routine method, that runs after screen.activity
        /// </summary>
        public void Activity(EventType type)
        {
            for (int i = zEvents.Count - 1; i >= 0; i--)
            {
                int result = 1;
                Event e = zEvents[i];
                //check for gc
                if (e.zCollect)
                {
                    zEvents.RemoveAt(i);
                    continue;
                }
                //check type
                if (type != e.zType)
                    continue;        
                //check for expired events
                if (Globals.GameTime.TotalGameTime >= e.zExpirationTime)
                {
                    zEvents.RemoveAt(i);
                    continue;
                }
                //check if ready to trigger
                if (Globals.GameTime.TotalGameTime >= e.zNextPulseTime)
                {
                    e.zNextPulseTime += e.zTick;
                    result = e.zFunc();
                }
                //check if it want to remove itself
                if (result == 0)
                {
                    zEvents.RemoveAt(i);
                    continue;
                }
            }
            InsertNewFrontEvents();
        }

        /// <summary>
        /// This function exists so the collection wont change in the front while executing events
        /// </summary>
        private void InsertNewFrontEvents()
        {
            foreach (Event e in zNewFrontEvents)
                zEvents.Add(e);
            zNewFrontEvents.Clear();
        }

        public void PreActivity()
        {
            Activity(EventType.PreEvent);
        }

        public void PostActivity()
        {
            Activity(EventType.PostEvent);
        }
    }
}
