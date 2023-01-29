using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace TestProject.WebServer
{
    public class WebServerTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly HttpListener _listener;
        private readonly Random _random = new Random();

        public WebServerTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://127.0.0.1:5000/");
            _listener.Start();
        }
        [Fact(DisplayName = "单线程")]
        public void Start()
        {
            while (true)
            {
                //接收请求
                var context = _listener.GetContext();
                //模拟计算耗时
                Thread.Sleep(_random.Next(1, 50));
                SetResponse(context, DateTime.Now.ToString("O"));
            }
        }

        /// <summary>
        /// 响应
        /// </summary>
        private void SetResponse(HttpListenerContext context, string responseStr)
        {
            var response = context.Response;
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseStr);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        [Fact(DisplayName = "多线程")]
        public void Start2()
        {
            while (true)
            {
                //接收请求
                var context = _listener.GetContext();
                //模拟计算耗时
                var thread = new Thread(() =>
                {
                    //模拟计算耗时
                    Thread.Sleep(_random.Next(1, 50));
                    SetResponse(context, DateTime.Now.ToString("O"));
                });
                thread.Start();
            }
        }

        [Fact(DisplayName = "线程池")]
        public void Start2_1()
        {
            while (true)
            {
                //接收请求
                var context = _listener.GetContext();
                Task.Run(() =>
                {
                    Thread.Sleep(_random.Next(1, 50));
                    SetResponse(context, DateTime.Now.ToString("O"));
                });
            }
        }

        [Fact(DisplayName = "简单线程池")]
        public void Start3()
        {
            while (true)
            {
                //接收请求
                var context = _listener.GetContext();
                SimpleThreadPool.Push(() =>
                {
                    Thread.Sleep(_random.Next(1, 50));
                    SetResponse(context, DateTime.Now.ToString("O"));
                });
            }
        }

        [Fact(DisplayName = "发生IO")]
        public void Start4()
        {
            while (true)
            {
                //接收请求
                var context = _listener.GetContext();
                SimpleThreadPool.Push(() =>
                {
                    var req = WebRequest.Create("http://www.baidu.com");
                    req.Method = "GET";
                    using var wr = req.GetResponse();
                    SetResponse(context, wr.ContentType);
                });
            }
        }

        [Fact(DisplayName = "使用异步IO")]
        public void Start5()
        {
            while (true)
            {
                //接收请求
                var context = _listener.GetContext();
                SimpleThreadPool.Push(() =>
                {
                    var req = WebRequest.Create("http://www.baidu.com");
                    req.Method = "GET";
                    req.BeginGetResponse(asyncResult =>
                    {
                        SimpleThreadPool.Push(() =>
                        {
                            var reqObj = asyncResult.AsyncState as WebRequest;
                            using var res = reqObj.EndGetResponse(asyncResult);
                            SetResponse(context, res.ContentType);
                        });
                    }, req);
                });
            }
        }

        [Fact(DisplayName = "使用Task进行异步IO")]
        public void Start6()
        {
            while (true)
            {
                //接收请求
                var context = _listener.GetContext();
                var req = WebRequest.Create("http://www.baidu.com");
                req.Method = "GET";
                req.GetResponseAsync().ContinueWith(r =>
                {
                    SetResponse(context, r.Result.ContentType);
                });
            }
        }

        [Fact(DisplayName = "使用await Task进行异步IO")]
        public void Start7()
        {
            async void GetContentType(HttpListenerContext context)
            {
                var req = WebRequest.Create("http://www.baidu.com");
                req.Method = "GET";
                var res = await req.GetResponseAsync();
                SetResponse(context, res.ContentType);
            }
            while (true)
            {
                //接收请求
                var context = _listener.GetContext();
                GetContentType(context);
            }
        }

        [Fact(DisplayName = ".Net 1.x APM模式的异步")]
        public void Test1()
        {
            //逻辑是分离的，需要花费大量的精力，为一小部分代码维护一大段上下文，在回调函数中再通过危险的Cast操作恢复数据
            var req = WebRequest.Create("http://www.baidu.com");
            req.Method = "GET";
            req.BeginGetResponse(EndGetResponse, new SendResponseState { Context = null, WebRequest = req });
            _testOutputHelper.WriteLine("发生IO立即返回");
            Thread.Sleep(500);
        }

        private void EndGetResponse(IAsyncResult asyncResult)
        {
            _testOutputHelper.WriteLine("IO完成");
            var reqObj = asyncResult.AsyncState as SendResponseState;
            var context = reqObj.Context;
            using var res = reqObj.WebRequest.EndGetResponse(asyncResult);
            _testOutputHelper.WriteLine(res.ContentType);
        }

        class SendResponseState
        {
            public WebRequest WebRequest { get; set; }
            public HttpListenerContext Context { get; set; }
        }

        [Fact(DisplayName = ".Net 2.x APM模式的异步")]
        public void Test2()
        {
            //最重要的进化，可以闭包捕获上下文，即使逻辑比较复杂，和主方法分离，也可以利用捕获的强类型变量，让分离的逻辑更可读
            var req = WebRequest.Create("http://www.baidu.com");
            req.Method = "GET";
            req.BeginGetResponse(delegate (IAsyncResult ar)
            {
                _testOutputHelper.WriteLine("IO完成");
                using var res = req.EndGetResponse(ar);
                _testOutputHelper.WriteLine(res.ContentType);
            }, req);
            _testOutputHelper.WriteLine("发生IO立即返回");
            Thread.Sleep(500);
        }

        [Fact(DisplayName = ".Net 3.x APM模式的异步")]
        public void Test3()
        {
            //小提升，简化了匿名委托的方法，不需要再指定委托参数的类型
            var req = WebRequest.Create("http://www.baidu.com");
            req.Method = "GET";
            req.BeginGetResponse(ar =>
            {
                _testOutputHelper.WriteLine("IO完成");
                using var res = req.EndGetResponse(ar);
                _testOutputHelper.WriteLine(res.ContentType);
            }, req);
            _testOutputHelper.WriteLine("发生IO立即返回");
            Thread.Sleep(500);
        }

        [Fact(DisplayName = ".Net 4.x TAP模式的异步")]
        public void Test4()
        {
            //进一步简化，从BeginXX和EndXX两个方法简化为一个方法，提供了统一的中断任务的方式
            var req = WebRequest.Create("http://www.baidu.com");
            req.Method = "GET";
            var task = req.GetResponseAsync().ContinueWith(r =>
              {
                  _testOutputHelper.WriteLine("IO完成");
                  _testOutputHelper.WriteLine(r.Result.ContentType);
              });
            _testOutputHelper.WriteLine("发生IO立即返回");
            task.Wait();
        }

        [Fact(DisplayName = ".Net 4.x TAP模式的异步")]
        public async void Test5()
        {
            //解决了回调嵌套问题、异常捕获等问题，使用同步的思维，来解决异步的问题。
            var req = WebRequest.Create("http://www.baidu.com");
            req.Method = "GET";
            var task = req.GetResponseAsync();
            _testOutputHelper.WriteLine("发生IO立即返回");
            var r = await task;
            _testOutputHelper.WriteLine("IO完成");
            _testOutputHelper.WriteLine(r.ContentType);
        }


        [Fact(DisplayName = "异步IO的边界")]
        public void Test6()
        {
            var req = WebRequest.Create("http://www.baidu.com");
            req.Method = "GET";
            var task = SelfGetResponseAsync(req).ContinueWith(r =>
            {
                _testOutputHelper.WriteLine("IO完成");
                _testOutputHelper.WriteLine(r.Result.ContentType);
            });
            _testOutputHelper.WriteLine("发生IO立即返回");
            task.Wait();
        }

        [Fact(DisplayName = "如何动态代理异步方法")]
        public async Task Test7()
        {
            var testClass = new ProxyGenerator().CreateClassProxy<AsyncTestClass>(new Interceptor());
            var res = await testClass.Get();
            Assert.Equal(2, res);
        }

        private Task<WebResponse> SelfGetResponseAsync(WebRequest webRequest)
        {
            var taskSource = new TaskCompletionSource<WebResponse>();
            webRequest.BeginGetResponse(ar =>
            {
                taskSource.SetResult(webRequest.EndGetResponse(ar));
            }, webRequest);
            return taskSource.Task;
        }
    }

    public class AsyncTestClass
    {
        public virtual async Task<int> Get()
        {
            Debug.WriteLine("实例方法执行前");
            await Task.Delay(1000);
            Debug.WriteLine("实例方法执行后");
            return 1;
        }
    }

    public class Interceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            Debug.WriteLine("代理方法执行前");
            invocation.Proceed();
            var task = (Task<int>)invocation.ReturnValue;
            invocation.ReturnValue = Test(task);
            Debug.WriteLine("代理方法执行后");
        }

        async Task<int> DoAfter(Task<int> task)
        {
            var res = await task;
            Debug.WriteLine("异步方法执行后");
            return res + 1;
        }

        private Task<int> Test(Task<int> oldTask)
        {
            var task = new TaskCompletionSource<int>();
            oldTask.ContinueWith(r =>
            {
                var res = r.Result;
                task.SetResult(res + 1);
            });
            return task.Task;
        }
    }
}