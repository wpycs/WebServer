using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace SimpleWebServer.Web
{
    public class CallbackThread
    {
        private static readonly Lazy<CallbackThread> Lazy =
            new Lazy<CallbackThread>(() => new CallbackThread());

        public static CallbackThread Instance => Lazy.Value;
        
        private CallbackThread()
        {
            Execute();
        }

        private readonly SortedList<DateTime, LinkedList<Action>> _waitCallbackList =
            new SortedList<DateTime, LinkedList<Action>>(200);

        private readonly Random _random = new Random();

        private bool _stopped = true;

        private static readonly object LockObj = new object();
        public void Register(Action callback)
        {
            lock (LockObj)
            {
                var dateTime = DateTime.Now.AddMilliseconds(_random.Next(1000, 2000));
                if (_waitCallbackList.ContainsKey(dateTime))
                {
                    _waitCallbackList[dateTime].AddLast(callback);
                }
                else
                {
                    _waitCallbackList.Add(dateTime, new LinkedList<Action>(new[] {callback}));
                }
            }
        }

        public void Execute()
        {
            if (_stopped)
            {
                _stopped = false;
                var thread = new Thread((() =>
                {
                    while (!_stopped)
                    {
                        lock (LockObj)
                        {
                            if (_waitCallbackList.Count > 0)
                            {
                                if (_waitCallbackList.Keys[0] <= DateTime.Now)
                                {
                                    var actions = _waitCallbackList[_waitCallbackList.Keys[0]];
                                    foreach (var action in actions)
                                    {
                                        action();
                                    }
                                    _waitCallbackList.Remove(_waitCallbackList.Keys[0]);
                                }
                            }
                        }
                        Thread.Sleep(0);
                    }
                }))
                {
                    IsBackground = true
                };
                thread.Start();
            }
        }
    }
}