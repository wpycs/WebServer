using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Linq;

namespace WebServer.Test
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var tasks = new List<Func<int>>();
            for (int i = 0; i <= 500; i++)
            {
                tasks.Add(() => i);
            }
            var res = tasks[0]();
        }
    }
}
