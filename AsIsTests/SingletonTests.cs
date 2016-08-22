using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

public class SingletonTests
{
    class A
    {
        static A()
        {
            Singleton<A>.Instance = null;
        }

        public A(object arg) { }
    }

    class B
    {
        static B()
        {
            Singleton<B>.Instance = null;
        }
    }

    class C { }

    [Fact]
    public void Test()
    {
        Assert.Null(Singleton<A>.Instance);
        Assert.Null(Singleton<B>.Instance);
        Assert.IsType<C>(Singleton<C>.Instance);

        var guid = Guid.NewGuid();
        Singleton<Guid>.Register(guid);
        Assert.Equal(guid, Singleton<Guid>.Instance);

        var str = guid.ToString();
        Singleton<string>.Register(str);
        Assert.Equal(str, Singleton<string>.Instance);
    }
}
