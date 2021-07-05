using System;
using System.Net;
using System.Threading;

namespace SimpleWebServer.Web
{
    class Program
    {
        static void Main(string[] args)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:5000/");
            listener.Start();
            //SyncListener(listener);
            //SyncListenerWithThread(listener);
            AsyncListener(listener);
        }

        private static readonly Random Random = new Random();

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
                Thread.Sleep(Random.Next(1000, 2000));
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
                var thread = new Thread(() =>
                {
                    Thread.Sleep(Random.Next(1000, 2000));
                    SetResponse(context);
                });
                thread.Start();
                Thread.Sleep(0);
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
                //等待传入请求时，此方法会阻塞线程
                var context = listener.GetContext();
                CallbackThread.Instance.Register(() =>
                {
                    SetResponse(context);
                });
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
