using System;
using System.Linq;
using Xunit;

public class ShorthandsTests
{
    [Fact]
    public void CreationDelegate()
    {
        Assert.IsType<object>(AsIs.CreateNew<object>());
        Assert.Equal(Guid.Empty, AsIs.CreateNew<Guid>());
        Assert.Null(AsIs.GetCreationDelegateFor<string>());
    }
}
