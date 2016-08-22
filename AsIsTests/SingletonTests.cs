using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

public class SingletonTests
{
    class Nested {
        static Nested()
        {
            Singleton<Nested>.Instance = null;
        }
    }

    [Fact]
    public void Test()
    {
        Assert.Null(Singleton<Nested>.Instance);

        var guid = Guid.NewGuid();
        Singleton<Guid>.Register(guid);
        Assert.Equal(guid, Singleton<Guid>.Instance);

        var str = guid.ToString();
        Singleton<string>.Register(str);
        Assert.Equal(str, Singleton<string>.Instance);
    }
}
