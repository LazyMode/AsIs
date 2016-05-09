using System;
using System.Diagnostics;
using Xunit;

public class AsTests
{
    class A
    {
        public static explicit operator string(A self)
            => self?.ToString();
        public static implicit operator A(ValueType e)
            => new A();
    }

    class B
    {
        public static implicit operator B(string s)
            => new B();
    }

    [Fact]
    public void Unchecked()
    {
        var sw = Stopwatch.StartNew();

        UncheckedTestCore();
        Trace.WriteLine(sw.ElapsedMilliseconds);
        sw.Restart();

        UncheckedTestCore();
        Trace.WriteLine(sw.ElapsedMilliseconds);
    }

    void UncheckedTestCore()
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
        Assert.IsType<A>(1.As<A>());
        Assert.IsType<A>(1.Cast<A>());
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
        Assert.IsType<B>(((int?)null).TypeAs<B>(typeof(string)));
        Assert.IsType<B>(((int?)null).TypeCast<B>(typeof(string)));
        Assert.IsType<B>(((string)null).ForCast().As<B>());
        Assert.IsType<B>(((string)null).ForCast().To<B>());
    }
}
