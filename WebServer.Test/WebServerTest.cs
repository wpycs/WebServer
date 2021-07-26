using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace WebServer.Test
{
    public class WebServerTest
    {
        readonly HttpListener _listener;
        public WebServerTest()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://127.0.0.1:5000/");
            _listener.Start();
        }

        [Fact(DisplayName = "回调实现并发")]
        public void Test9()
        {
            var list = new LinkedList<KeyValuePair<DateTime, Action>>();
            var thread = new Thread(() =>
            {
                while (true)
                {
                    var item = list.First;
                    while (item != null)
                    {
                        if (item.Value.Key <= DateTime.Now)
                        {
                            list.Remove(item);
                            item.Value.Value.Invoke();
                        }
                        item = item.Next;
                    }
                    Thread.Sleep(0);
                }

            })
            { IsBackground = true };
            thread.Start();

            while (true)
            {
                var context = _listener.GetContext(); ;
                list.AddLast(new KeyValuePair<DateTime, Action>(DateTime.Now.AddMilliseconds(1000), () => EndResponse(context)));
                Thread.Sleep(0);
            }
        }

        [Fact(DisplayName = "单线程实现并发")]
        public void Test10()
        {
            var list = new LinkedList<KeyValuePair<DateTime, Action>>();
            var ioResQueue = new BlockingCollection<Action>();
            var thread = new Thread(() =>
            {
                while (true)
                {
                    var item = list.First;
                    while (item != null)
                    {
                        if (item.Value.Key <= DateTime.Now)
                        {
                            ioResQueue.Add(item.Value.Value);
                            list.Remove(item);
                        }
                        item = item.Next;
                    }
                    Thread.Sleep(0);
                }

            })
            { IsBackground = true };
            thread.Start();

        reGetContext:
            var contextTask = _listener.GetContextAsync();
            while (true)
            {
                if (contextTask.IsCompleted)
                {
                    var context = contextTask.Result;
                    list.AddLast(new KeyValuePair<DateTime, Action>(DateTime.Now.AddMilliseconds(1000), () => EndResponse(context)));
                    goto reGetContext;
                }
                if (ioResQueue.TryTake(out var action))
                {
                    action.Invoke();
                }
                Thread.Sleep(0);
            }
        }

        [Fact(DisplayName = "使用Task封装")]
        public void Test11()
        {
            var list = new LinkedList<KeyValuePair<DateTime, TaskCompletionSource<string>>>();
            var thread = new Thread(() =>
            {
                while (true)
                {
                    var item = list.First;
                    while (item != null)
                    {
                        if (item.Value.Key <= DateTime.Now)
                        {
                            list.Remove(item);
                            item.Value.Value.SetResult("1");
                            //或者传递异常
                            //item.Value.Value.SetException(new Exception("发生错误"));
                        }
                        item = item.Next;
                    }
                    Thread.Sleep(0);
                }

            })
            { IsBackground = true };
            thread.Start();
            while (true)
            {
                var context = _listener.GetContext(); ;
                var taskCompletionSource = new TaskCompletionSource<string>();
                list.AddLast(new KeyValuePair<DateTime, TaskCompletionSource<string>>(DateTime.Now.AddMilliseconds(1000), taskCompletionSource));
                var task = taskCompletionSource.Task;
                task.ContinueWith(r => EndResponse(context));
                Thread.Sleep(0);
            }
        }

        [Fact(DisplayName = "对操作进行简化，忽略IO部分的逻辑")]
        public void Test12()
        {
            while (true)
            {
                var context = _listener.GetContext();
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                //简化版
                Task.Delay(1000).ContinueWith(r =>
                {
                    Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                    EndResponse(context);
                });
                Thread.Sleep(0);
            }
        }

        [Fact(DisplayName = "实际的业务场景")]
        public void Test13()
        {
            while (true)
            {
                var context = _listener.GetContext();
                try
                {
                    Thread.Sleep(3);
                }
                catch
                {
                    EndResponse(context);
                    return;
                }
                Task.Delay(1000).ContinueWith(r =>
                {
                    if (r.IsFaulted)
                    {
                        EndResponse(context);
                        return;
                    }
                    try
                    {
                        Thread.Sleep(3);
                    }
                    catch
                    {
                        EndResponse(context);
                        return;
                    }
                    Task.Delay(1000).ContinueWith(t =>
                    {
                        if (r.IsFaulted)
                        {
                            EndResponse(context);
                            return;
                        }
                        try
                        {
                            Thread.Sleep(3);
                        }
                        catch
                        {
                            EndResponse(context);
                            return;
                        }
                        Task.Delay(1000).ContinueWith(r =>
                        {
                            if (r.IsFaulted)
                            {
                                EndResponse(context);
                                return;
                            }
                            try
                            {
                                Thread.Sleep(3);
                            }
                            catch
                            {
                                EndResponse(context);
                                return;
                            }
                            EndResponse(context);
                        });
                    });
                });
                Thread.Sleep(0);
            }
        }

        [Fact(DisplayName = "对操作进行简化，使用await处理嵌套,简化异常处理")]
        public void Test14()
        {
            while (true)
            {
                var context = _listener.GetContext();
                NewMethod(context);
                Thread.Sleep(0);
            }

            async void NewMethod(HttpListenerContext context)
            {
                try
                {
                    Thread.Sleep(3);
                    await Task.Delay(1000);
                    Thread.Sleep(3);
                    await Task.Delay(1000);
                    Thread.Sleep(3);
                    await Task.Delay(1000);
                    EndResponse(context);
                }
                catch
                {
                    EndResponse(context);
                }
            }
        }

        [Fact(DisplayName ="await Task是否会发生线程切换")]
        public  async Task Test15()
        {
            async Task InnerTest()
            {
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                await Task.CompletedTask;
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                var res = await Task.FromResult(10);
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                await Task.Delay(0);
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                await Task.Delay(1);
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                await Task.Yield();
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
            }
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
            await InnerTest();
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
        }

        [Fact(DisplayName = "await Task是否一定是异步IO")]
        public async Task Test16()
        {
            async Task InnerTest()
            {
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                await Task.Run(() => 1);
            }
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
            await InnerTest();
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
        }

        private void EndResponse(HttpListenerContext context)
        {
            var response = context.Response;
            var responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }
    }
}
