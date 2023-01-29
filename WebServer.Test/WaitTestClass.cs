using System;

namespace WebServer.Test
{
    public class WaitTestClass1
    {
        public int DoSomething(int a, int b)
        {
            return a + b;
        }
    }

    public class WaitTestClass2
    {
        private readonly IDependency _dependency;

        public IDependency Dependency { get; set; }

        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public WaitTestClass2(IDependency dependency)
        {
            _dependency = dependency;
        }

        public WaitTestClass2() : this(new Dependency())
        {

        }

        public int DoSomething(int a, int b)
        {
            var dependency = new Dependency();
            a = dependency.Method(a);
            return a + b;
        }

        public int DoSomething2(int a, int b, IDependency dependency)
        {
            a = dependency.Method(a);
            return a + b;
        }

        public int DoSomething3(int a, int b)
        {
            a = _dependency.Method(a);
            return a + b;
        }
    }

    public class WaitTestClass3
    {
        public IDependency Dependency { get; set; }

        public WaitTestClass3()
        {
            Dependency = new Dependency();
        }

        public int DoSomething(int a, int b)
        {
            a = Dependency.Method(a);
            return a + b;
        }
    }

    public class WaitTestClass4
    {
        public Func<int, int> DependencyMethod { get; set; }

        public WaitTestClass4()
        {
            DependencyMethod = new Dependency().Method;
        }

        public int DoSomething(int a, int b)
        {
            a = DependencyMethod(a);
            return a + b;
        }
    }

    public class WaitTestClass5
    {
        private readonly IDependency _dependency;
        private readonly IDependency2 _dependency2;

        public WaitTestClass5(IDependency dependency, IDependency2 dependency2)
        {
            _dependency = dependency;
            _dependency2 = dependency2;
        }

        public int DoSomething(int a, int b)
        {
            a = _dependency.Method(a);
            _dependency2.Method(b);
            return a + b;
        }
    }

    public class Dependency : IDependency
    {
        public int Method(int a)
        {
            throw new System.NotImplementedException();
        }
    }

    public interface IDependency
    {
        int Method(int a);
    }

    public interface IDependency2
    {
        void Method(int a);

        void Method2(int a);
    }
}