using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

public class SingletonTests
{
    [Fact]
    public void Test()
    {
        var guid = Guid.NewGuid();
        Singleton<Guid>.Register(guid);
        Assert.Equal(guid, Singleton<Guid>.Instance);

        var str = guid.ToString();
        Singleton<string>.Register(str);
        Assert.Equal(str, Singleton<string>.Instance);
    }
}

