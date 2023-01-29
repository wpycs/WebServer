using System;
using System.Collections.Concurrent;
using System.Threading;

namespace TestProject.WebServer
{
    public class SimpleThreadPool
    {
        static SimpleThreadPool()
        {
            Start(2);
        }
        private static readonly BlockingCollection<Action> Queue = new BlockingCollection<Action>(10);
        public static void Push(Action action)
        {
            Queue.Add(action);
        }
        private static void Start(int max)
        {
            for (int i = 0; i < max; i++)
            {
                var thread = new Thread(ThreadStart)
                {
                    IsBackground = true
                };
                thread.Start();
            }
        }
        private static void ThreadStart()
        {
            while (true)
            {
                var action = Queue.Take();
                action();
            }
        }
    }
}