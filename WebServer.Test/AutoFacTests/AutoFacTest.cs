using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Xunit;

namespace WebServer.Test.AutoFacTests
{
    public class AutoFacTest
    {
        [Fact]
        public void Test()
        {
            var builder = new Autofac.ContainerBuilder();
            builder.RegisterType<TestContext>().InstancePerLifetimeScope();
            builder.RegisterType<TestContextContainer>().InstancePerLifetimeScope();
            builder.RegisterType<TestA>().InstancePerLifetimeScope();
            builder.RegisterType<TestB>().InstancePerLifetimeScope();
            builder.RegisterType<TestC>().InstancePerLifetimeScope();
            builder.RegisterType<TestD>().InstancePerLifetimeScope();
            builder.RegisterType<TestF>().InstancePerLifetimeScope();
            builder.Register(c =>
            {
                var context = c.Resolve<TestContext>();
                return context.Resolve<ITest1>();
            }).As<ITest1>().InstancePerLifetimeScope();
            builder.Register(c =>
            {
                var context = c.Resolve<TestContext>();
                return context.Resolve<ITest2>();
            }).As<ITest2>().InstancePerLifetimeScope();
            builder.RegisterType<TestClass>().AsSelf().InstancePerLifetimeScope();

            var container = builder.Build();
            using (var scope = container.BeginLifetimeScope())
            {
                var arr = new[] { "a", "b", "c" };
                var set = new HashSet<TestClass>();
                foreach (var type in arr)
                {
                    var innerScope = scope.Resolve<TestContextContainer>().GetScope(type, null);
                    var data = innerScope.Resolve<TestClass>();
                    set.Add(data);
                }
                foreach (var type in arr.Reverse())
                {
                    var innerScope = scope.Resolve<TestContextContainer>().GetScope(type, null);
                    var data = innerScope.Resolve<TestClass>();
                    set.Add(data);
                }
            }
        }
    }

    public class TestContextContainer
    {
        private readonly ILifetimeScope _scope;
        private readonly Dictionary<(string, string), ILifetimeScope> _scopeDictionary;
        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public TestContextContainer(ILifetimeScope scope)
        {
            _scope = scope;
            _scopeDictionary = new Dictionary<(string, string), ILifetimeScope>();

        }

        public ILifetimeScope GetScope(string lIdType, string trueLcId)
        {
            var key = (lIdType, trueLcId);
            if (_scopeDictionary.TryGetValue(key, out var scope))
            {
                return scope;
            }
            var newScope = _scope.BeginLifetimeScope();
            _scope.CurrentScopeEnding += (a, b) =>
            {
                newScope.Dispose();
            };
            newScope.Resolve<TestContext>(new NamedParameter("lIdType", lIdType),
                new NamedParameter("trueLcId", trueLcId));
            _scopeDictionary[key] = newScope;
            return newScope;
        }
    }
    public class TestContext
    {
        private readonly ILifetimeScope _lifetimeScope;
        private readonly string _lIdType;
        private readonly string _trueLcId;
        private readonly (string, string) _key;

        private static readonly Dictionary<(string, string), Type[]> TypeDictionary = new Dictionary<(string, string), Type[]>()
        {
            { ("a", null), new[] { typeof(TestA) } },
            { ("b", null), new[] { typeof(TestB),typeof(TestF) } },
        };

        private static readonly Type[] OtherTypes = new Type[]
        {
            typeof(TestC),
            typeof(TestD)
        };
        

        public TestContext(ILifetimeScope lifetimeScope, string lIdType, string trueLcId)
        {
            _lifetimeScope = lifetimeScope;
           
            _lIdType = lIdType;
            _trueLcId = trueLcId;
            _key = (lIdType, trueLcId);
        }

        public object Resolve<T>()
        {
            var typeT = typeof(T);
            if (TypeDictionary.TryGetValue(_key, out var types))
            {
                var type = types.FirstOrDefault(c => typeT.IsAssignableFrom(c));
                if (type != null)
                {
                    return _lifetimeScope.Resolve(type);
                }
            }
            return _lifetimeScope.Resolve(OtherTypes.First(c => typeT.IsAssignableFrom(c)));
        }
    }

    public class TestClass
    {
        private readonly ITest1 _test1;
        private readonly ITest2 _test2;

        public TestClass(ITest1 test1, ITest2 test2)
        {
            _test1 = test1;
            _test2 = test2;
        }
    }
    public interface ITest1
    {

    }

    public interface ITest2
    {

    }

    [Test(LIdType = "a")]
    public class TestA : ITest1
    {

    }

    [Test(LIdType = "b")]
    public class TestB : ITest1
    {
        private readonly ITest2 _test2;

        public TestB(ITest2 test2)
        {
            _test2 = test2;
        }
    }

    public class TestC : ITest1
    {
        private readonly ITest2 _test2;

        public TestC(ITest2 test2)
        {
            _test2 = test2;
        }
    }

    public class TestD : ITest2
    {

    }

    [Test(LIdType = "b")]
    public class TestF : ITest2
    {

    }

    public class TestAttribute : Attribute
    {
        public string LIdType { get; set; }

        public string TrueLcId { get; set; }
    }
}