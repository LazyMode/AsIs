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

    class ClassForNullable
    {
        public int Value;

        //public static explicit operator int? (ClassForNullable o)
        //    => o?.Value;
        //public static explicit operator int(ClassForNullable o)
        //    => o.Value;
        public static explicit operator ClassForNullable(int i)
            => new ClassForNullable { Value = i };
        public static explicit operator ClassForNullable(int? i)
            => i.HasValue ? (ClassForNullable)i.Value : null;
    }
    class ClassNonNullable
    {
        public int Value;

        //public static explicit operator int(ClassNonNullable o)
        //    => o.Value;
        public static explicit operator ClassNonNullable(int i)
            => new ClassNonNullable { Value = i };
    }

    [Fact]
    public void Nullable()
    {
        int? i = null;
        int? i1 = 1;
        Assert.Equal(1, 1.As<ClassForNullable>().Value);
        Assert.Equal(1, 1.To<ClassForNullable>().Value);
        Assert.Equal(null, i.As<ClassForNullable>());
        Assert.Equal(null, i.To<ClassForNullable>());
        Assert.Equal(1, i1.As<ClassForNullable>().Value);
        Assert.Equal(1, i1.To<ClassForNullable>().Value);

        Assert.Equal(1, 1.As<ClassNonNullable>().Value);
        Assert.Equal(1, 1.To<ClassNonNullable>().Value);
        Assert.Equal(null, i.As<ClassNonNullable>());
        Assert.Equal(null, i.To<ClassNonNullable>());
        Assert.Equal(1, i1.As<ClassNonNullable>().Value);
        Assert.Equal(1, i1.To<ClassNonNullable>().Value);
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
        Assert.Equal(-128, 128.To<sbyte>());

        Assert.Equal(null, ((object)null).To<object>());

        Assert.Equal(1d, 1.To<double>());
        Assert.ThrowsAny<Exception>(() => (double)(object)1);
        Assert.Equal(1d, 1.To<double?>());
        Assert.ThrowsAny<Exception>(() => (double?)(object)1);
        Assert.Equal(1, 1.To<object>());
        Assert.Equal(1, (object)1);
        Assert.ThrowsAny<Exception>(() => (string)(object)1);
        Assert.ThrowsAny<Exception>(() => 1.To<string>());
        Assert.ThrowsAny<Exception>(() => 1.As<double>());
        Assert.Equal(1d, 1.As<double?>());
        Assert.Equal(null, 1 as double?);
        Assert.Equal(1, 1.As<object>());
        Assert.Equal(null, (object)1 as string);
        Assert.Equal(null, 1.As<string>());

        Assert.Equal("", "".To<object>());
        Assert.Equal("", "".To<string>());
        Assert.ThrowsAny<Exception>(() => "".To<Guid>());
        Assert.ThrowsAny<Exception>(() => "".As<double>());
        Assert.Equal("", "".As<object>());
        Assert.Equal("", "".As<string>());

        var a = new A();
        var s = a.ToString();
        Assert.IsType<A>(1.As<A>());
        Assert.IsType<A>(1.To<A>());
        Assert.Equal(a, a.To<A>());
        Assert.Equal(a, a.To<object>());
        Assert.Equal(s, a.To<string>());
        Assert.Equal(a, a.As<A>());
        Assert.Equal(a, a.As<object>());
        Assert.Equal(s, a.As<string>());

        Assert.Equal(null, ((int?)null).As<B>());
        Assert.ThrowsAny<Exception>(() => a.To<B>());
        Assert.Equal(null, a.As<B>());
        Assert.IsType<B>(s.As<B>());
        Assert.IsType<B>(s.To<B>());
        Assert.IsType<B>(((int?)null).As<B>(typeof(string)));
        Assert.IsType<B>(((int?)null).To<B>(typeof(string)));
        Assert.IsType<B>(((string)null).ForCast().As<B>());
        Assert.IsType<B>(((string)null).ForCast().To<B>());
    }

    [Fact]
    public void Checked()
    {
        var sw = Stopwatch.StartNew();

        CheckedTestCore();
        Trace.WriteLine(sw.ElapsedMilliseconds);
        sw.Restart();

        CheckedTestCore();
        Trace.WriteLine(sw.ElapsedMilliseconds);
    }

    void CheckedTestCore()
    {
        Assert.ThrowsAny<Exception>(() => 128.ToChecked<sbyte>());
    }
}
