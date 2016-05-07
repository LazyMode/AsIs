using System;
using System.Collections.Generic;
using System.Text;

public struct ForCast<TSource>
{
    private static Type TypeSource = typeof(TSource);

    public TSource Source;
    public ForCast(TSource source)
    {
        Source = source;
    }

    public TTarget As<TTarget>()
        => Source.TypeAs<TTarget>(TypeSource);
    public TTarget To<TTarget>()
        => Source.TypeCast<TTarget>(TypeSource);
}

public static class ForCastEx
{
    public static ForCast<T> ForCast<T>(this T source)
        => new ForCast<T>(source);
}
