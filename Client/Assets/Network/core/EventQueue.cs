using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public delegate void OnEventFunc(NetGameObject target);

    public class EventQueue
    {
        public class EventNode
        {
            public float mTimeToEvent;
            public OnEventFunc eventCall;
        }


        public List<EventNode> event_queue = new List<EventNode>();


        public void Enqueue(OnEventFunc func, float inTimestamp)
        {
            event_queue.Add(new EventNode() { mTimeToEvent = inTimestamp, eventCall = func });
        }

        public void Dequeue(NetGameObject target)
        {
            int i = 0;
            while (event_queue.Count > i)
            {
                if (event_queue[i].mTimeToEvent <= Timing.sInstance.GetFrameStartTime())
                {
                    event_queue[i].eventCall(target);
                    event_queue.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }
        }
    
    }
}
