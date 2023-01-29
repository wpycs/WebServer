using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Autofac.Extras.Moq;
using Moq;
using Shouldly;
using Xunit;

namespace WebServer.Test
{
    public class UnitTest2
    {
        [Fact(DisplayName = "最简单的单元测试")]
        public void Test1()
        {
            var service = new WaitTestClass1();
            var res = service.DoSomething(1, 2);
            Assert.Equal(3, res);
        }

        [Fact(DisplayName = "存在依赖,调用不通过")]
        public void Test2()
        {
            var service = new WaitTestClass2();
            var res = service.DoSomething(1, 2);
            Assert.Equal(3, res);
        }

        [Fact(DisplayName = "依赖调用方法时传入")]
        public void Test3()
        {
            var service = new WaitTestClass2();
            var res = service.DoSomething2(1, 2, new DependencyTest());
            Assert.Equal(3, res);
        }

        [Fact(DisplayName = "依赖在构造函数传入")]
        public void Test4()
        {
            var service = new WaitTestClass2(new DependencyTest());
            var res = service.DoSomething3(1, 2);
            Assert.Equal(3, res);
        }

        [Fact(DisplayName = "提供设置方法，对象初始化完成后传入")]
        public void Test5()
        {
            var service = new WaitTestClass3
            {
                Dependency = new DependencyTest()
            };
            var res = service.DoSomething(1, 2);
            Assert.Equal(3, res);
        }

        [Fact(DisplayName = "委托传入")]
        public void Test6()
        {
            var service = new WaitTestClass4
            {
                DependencyMethod = (a) => a
            };
            var res = service.DoSomething(1, 2);
            Assert.Equal(3, res);
        }

        [Fact(DisplayName = "自动生成假对象")]
        public void Test7()
        {
            var mock = new Moq.Mock<IDependency>();
            mock.Setup(c => c.Method(It.IsAny<int>())).Returns((int a) => a + 1);
            var service = new WaitTestClass2(mock.Object);
            var res = service.DoSomething3(1, 2);
            Assert.Equal(4, res);
        }

        [Fact(DisplayName = "自动生成假对象2")]
        public void Test8()
        {
            var mock = AutoMock.GetLoose();
            mock.Mock<IDependency>().Setup(c => c.Method(It.IsAny<int>())).Returns((int a) => a + 1);
            var service = mock.Create<WaitTestClass5>();
            var res = service.DoSomething(1, 2);
            Assert.Equal(4, res);
        }

        [Fact(DisplayName = "断言-验证结果")]
        public void Test9()
        {
            var mock = AutoMock.GetLoose();
            mock.Mock<IDependency>().Setup(c => c.Method(It.IsAny<int>())).Returns((int a) => a + 1);
            var service = mock.Create<WaitTestClass5>();
            var res = service.DoSomething(1, 2);
            res.ShouldBe(4);
        }

        [Fact(DisplayName = "断言-验证依赖正常调用")]
        public void Test10()
        {
            var mock = AutoMock.GetLoose();
            mock.Mock<IDependency>().Setup(c => c.Method(It.IsAny<int>())).Returns((int a) => a + 1);
            var service = mock.Create<WaitTestClass5>();
            var res = service.DoSomething(1, 2);
            mock.Mock<IDependency2>().Verify(a => a.Method(2), Times.Once);
            mock.Mock<IDependency2>().Verify(a => a.Method2(It.IsAny<int>()), Times.Never);
        }

    }

    /// <summary>
    /// 手动继承生成假对象
    /// </summary>
    public class DependencyTest : IDependency
    {
        public int Method(int a)
        {
            return a;
        }
    }
}