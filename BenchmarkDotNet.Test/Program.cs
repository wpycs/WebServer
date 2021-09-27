using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            ThreadPool.SetMaxThreads(8, 8);
            ThreadPool.SetMinThreads(8, 8);
            var summary = BenchmarkRunner.Run<ThreadBenchmark>();
        }
    }

    [MemoryDiagnoser]
    public class ThreadBenchmark
    {
        private int _loopTime = 10000;
        [Benchmark(Description = "多线程")]
        public void ThreadTest()
        {
            var success = 0;
            for (int i = 0; i < _loopTime; i++)
            {
                var thread = new Thread(() =>
                    {
                        Fib(10);
                        Interlocked.Increment(ref success);
                    })
                { IsBackground = true };
                thread.Start();
            }
            while (success != _loopTime)
            {
                Thread.Sleep(0);
            }
        }

        [Benchmark(Description = "线程池")]
        public void ThreadPoolTest()
        {
            var success = 0;
            for (int i = 0; i < _loopTime; i++)
            {
                Task.Run(() =>
                {
                    Fib(10);
                    Interlocked.Increment(ref success);
                });
            }
            while (success != _loopTime)
            {
                Thread.Sleep(0);
            }
        }

        [Benchmark(Description = "简易线程池")]
        public void SimpleThreadPoolTest()
        {
            var success = 0;
            for (int i = 0; i < _loopTime; i++)
            {
                SimpleThreadPool.Push(() =>
                {
                    Fib(10);
                    Interlocked.Increment(ref success);
                });
            }
            while (success != _loopTime)
            {
                Thread.Sleep(0);
            }
        }

        private long Fib(long number)
        {
            if (number == 1 || number == 2)
            {
                return 1;
            }
            else
            {
                return Fib(number - 1) + Fib(number - 2);
            }
        }
    }

    public class SimpleThreadPool
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        static SimpleThreadPool()
        {
            Start(8);
        }

        private static readonly BlockingCollection<Action> Queue = new BlockingCollection<Action>(10000);

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
