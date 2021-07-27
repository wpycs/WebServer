using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TestProject.WebServer
{
    public class UnitTest1:IDisposable
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly HttpListener _listener;
        public UnitTest1(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            ThreadPool.SetMinThreads(0, 0);
            ThreadPool.SetMaxThreads(8, 8);
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://127.0.0.1:5000/");
            _listener.Start();
        }

        [Fact(DisplayName = "单线程")]
        public void Test1()
        {
            while (true)
            {
                //接收请求
                var context = _listener.GetContext();
                Thread.Sleep(1000);
                SetResponse(context);
                Thread.Sleep(0);
            }
        }

        [Fact(DisplayName = "多线程,使用Thread")]
        public void Test2()
        {
            while (true)
            {
                //接收请求
                var context = _listener.GetContext();
                var thread = new Thread(() =>
                {
                    Thread.Sleep(1000);
                    SetResponse(context);
                });
                thread.Start();
                Thread.Sleep(0);
            }
        }

        [Fact(DisplayName = "多线程,使用Task")]
        public void Test3()
        {
            while (true)
            {
                //接收请求
                var context = _listener.GetContext();
                Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    SetResponse(context);
                });
                Thread.Sleep(0);
            }
        }

        [Fact(DisplayName = "尝试自己实现和Thread.Sleep(1000)一样的效果")]
        public void Test4()
        {
            #region 使用独立线程扫描集合，找出过期的数据，执行回调方法
            var list = new LinkedList<KeyValuePair<DateTime, Action>>();
            var thread = new Thread(() =>
            {
                while (true)
                {
                    var item = list.First;
                    while (item != null)
                    {
                        var next = item.Next;
                        if (item.Value.Key <= DateTime.Now)
                        {
                            item.Value.Value.Invoke();
                            list.Remove(item);
                        }
                        item = next;
                    }
                    Thread.Sleep(0);
                }
            });
            thread.Start();
            #endregion
            while (true)
            {
                //接收请求
                var context = _listener.GetContext();
                list.AddLast(new KeyValuePair<DateTime, Action>(DateTime.Now.AddMilliseconds(1000),
                    (() => { SetResponse(context); })));
            }
        }

        [Fact(DisplayName = "使用Task.Delay")]
        public void Test5()
        {
            while (true)
            {
                //接收请求
                var context = _listener.GetContext();
                Task.Delay(1000).ContinueWith(task =>
                {
                    SetResponse(context);
                });
            }
        }

        /// <summary>
        /// 响应Hello world!
        /// </summary>
        /// <param name="context"></param>
        private void SetResponse(HttpListenerContext context)
        {
            var response = context.Response;
            var responseString = "Hello world!";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            ((IDisposable) _listener)?.Dispose();
        }
    }
}
