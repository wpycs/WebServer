using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWebServer.Web
{
    class Program
    {
        static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(0, 0);
            ThreadPool.SetMaxThreads(8, 8);
            var listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:5000/");
            listener.Start();
            //SyncListener(listener);
            SyncListenerWithThread(listener);
            //AsyncListener1(listener);
        }

        /// <summary>
        /// 单线程
        /// </summary>
        /// <param name="listener"></param>
        static void SyncListener(HttpListener listener)
        {
            while (true)
            {
                //接收请求
                var context = listener.GetContext();
                Thread.Sleep(1000);
                SetResponse(context);
                Thread.Sleep(0);
            }
        }

        /// <summary>
        /// 多线程
        /// </summary>
        /// <param name="listener"></param>
        static void SyncListenerWithThread(HttpListener listener)
        {
            while (true)
            {
                //接收请求
                var context = listener.GetContext();
                Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    SetResponse(context);
                });
                Thread.Sleep(0);
            }
        }

        /// <summary>
        /// 异步模拟
        /// </summary>
        /// <param name="listener"></param>
        static void AsyncListener1(HttpListener listener)
        {
            while (true)
            {
                var context = listener.GetContext();
                Task.Delay(1000).ContinueWith(task => SetResponse(context));
            }
        }

        /// <summary>
        /// 异步模拟
        /// </summary>
        /// <param name="listener"></param>
        static void AsyncListener(HttpListener listener)
        {
            while (true)
            {
                var context = listener.GetContext();
                //cpu密集操作消耗
                Thread.Sleep(1);
                //io操作
                Task.Delay(1000).ContinueWith((task =>
                {
                    //cpu密集操作消耗
                    Thread.Sleep(1);
                    //io操作
                    Task.Delay(100).ContinueWith(_ =>
                    {
                        //cpu密集操作
                        Thread.Sleep(1);
                        //响应
                        SetResponse(context);
                    });
                }));
                Thread.Sleep(0);
            }
        }



        private static void SetResponse(HttpListenerContext context)
        {
            var response = context.Response;
            var responseString = "Hello world!";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }
    }
}
