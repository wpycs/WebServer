using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace ConsoleApp.Web
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new HttpServer();
            server.Listener();
            Console.ReadKey();
        }
    }

    public class HttpServer
    {
        public void Listener()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:5000/");
            listener.Start();
            Console.WriteLine("Listening...");
            UseBeginEnd(listener);
        }

        /// <summary>
        /// 串行处理请求，一个请求处理结束后接收下一个请求
        /// </summary>
        /// <param name="listener"></param>
        private void Simple(HttpListener listener)
        {
            while (true)
            {
                //等待传入请求时，此方法会阻止
                var context = listener.GetContext();
                var request = context.Request;
                var response = context.Response;
                //模拟处理任务时长
                Thread.Sleep(1000);
                var responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                var output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
        }

        /// <summary>
        /// 多线程处理，一个请求开启一个新线程处理
        /// </summary>
        /// <param name="listener"></param>
        private void UseThread(HttpListener listener)
        {
            while (true)
            {
                //等待传入请求时，此方法会阻止
                var context = listener.GetContext();
                var thread = new Thread(() =>
                {
                    var request = context.Request;
                    var response = context.Response;
                    //模拟处理任务时长
                    Thread.Sleep(1000);
                    var responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
                    var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    var output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                });
                thread.Start();
            }
        }

        /// <summary>
        /// 使用.NET早期的异步方法处理
        /// </summary>
        /// <param name="listener"></param>
        private void UseBeginEnd(HttpListener listener)
        {
            listener.BeginGetContext(EndResponse, listener);
        }

        private void EndResponse(IAsyncResult ar)
        {
            var listener = ar.AsyncState as HttpListener;
            var context = listener.EndGetContext(ar);
            listener.BeginGetContext(EndResponse, listener);
            var request = context.Request;
            var response = context.Response;
            //模拟处理任务时长
            Thread.Sleep(1000);
            var responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

    }
}
