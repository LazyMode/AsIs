using System;
using System.Diagnostics;
using Xunit;

public class AsTests
{
    class A
    {
        public static explicit operator string(A self)
            => self?.ToString();
    }

    class B
    {
        public static implicit operator B(string s)
            => new B();
        public static implicit operator B(ValueType e)
            => new B();
    }

    [Fact]
    public void Cast()
    {
        var sw = Stopwatch.StartNew();

        CastTestCore();
        Trace.WriteLine(sw.ElapsedMilliseconds);
        sw.Restart();

        CastTestCore();
        Trace.WriteLine(sw.ElapsedMilliseconds);
    }

    void CastTestCore()
    {
        Assert.Equal(null, ((object)null).Cast<object>());

        Assert.Equal(1d, 1.Cast<double>());
        Assert.ThrowsAny<Exception>(() => (double)(object)1);
        Assert.Equal(1d, 1.Cast<double?>());
        Assert.ThrowsAny<Exception>(() => (double?)(object)1);
        Assert.Equal(1, 1.Cast<object>());
        Assert.Equal(1, (object)1);
        Assert.ThrowsAny<Exception>(() => (string)(object)1);
        Assert.ThrowsAny<Exception>(() => 1.Cast<string>());
        Assert.ThrowsAny<Exception>(() => 1.As<double>());
        Assert.Equal(1d, 1.As<double?>());
        Assert.Equal(null, 1 as double?);
        Assert.Equal(1, 1.As<object>());
        Assert.Equal(null, (object)1 as string);
        Assert.Equal(null, 1.As<string>());

        Assert.Equal("", "".Cast<object>());
        Assert.Equal("", "".Cast<string>());
        Assert.ThrowsAny<Exception>(() => "".Cast<Guid>());
        Assert.ThrowsAny<Exception>(() => "".As<double>());
        Assert.Equal("", "".As<object>());
        Assert.Equal("", "".As<string>());

        var a = new A();
        var s = a.ToString();
        Assert.Equal(a, a.Cast<A>());
        Assert.Equal(a, a.Cast<object>());
        Assert.Equal(s, a.Cast<string>());
        Assert.Equal(a, a.As<A>());
        Assert.Equal(a, a.As<object>());
        Assert.Equal(s, a.As<string>());

        Assert.Equal(null, ((int?)null).As<B>());
        Assert.ThrowsAny<Exception>(() => a.Cast<B>());
        Assert.Equal(null, a.As<B>());
        Assert.IsType<B>(s.As<B>());
        Assert.IsType<B>(s.Cast<B>());
        Assert.IsType<B>(1.TypeAs<B>(typeof(ValueType)));
        Assert.IsType<B>(1.TypeCast<B>(typeof(ValueType)));
        Assert.IsType<B>(((int?)null).TypeAs<B>(typeof(string)));
        Assert.IsType<B>(((int?)null).TypeCast<B>(typeof(string)));
    }
}
