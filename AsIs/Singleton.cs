using System;
using System.Collections.Concurrent;

static class Singleton
{
    internal static ConcurrentDictionary<Type, Lazy<object>> Singletons
        = new ConcurrentDictionary<Type, Lazy<object>>();

    internal static Type[] NoType = new Type[0];
}

public static class Singleton<T>
{
    static Singleton()
    {
        var proxy = TypeHelper<T>.Proxy;
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(TypeHelper<T>.ThisType.TypeHandle);

        if (proxy != null)
            Register(proxy, false);
    }

    static Lazy<object> LazyAccess
    {
        get { return Singleton.Singletons[TypeHelper<T>.ThisType]; }
        set { Singleton.Singletons[TypeHelper<T>.ThisType] = value; }
    }

    public static T Instance
    {
        get { return (T)LazyAccess.Value; }
        set { LazyAccess = new Lazy<object>(() => value); }
    }

    static bool Register(Func<object> factory, bool? throwIfExist = null)
    {
        var lazy = new Lazy<object>(factory);

        if (!throwIfExist.HasValue)
        {
            LazyAccess = lazy;
            return true;
        }

        if (Singleton.Singletons.TryAdd(TypeHelper<T>.ThisType, lazy))
            return true;

        if (throwIfExist.Value)
            throw new InvalidOperationException();

        return false;
    }

    public static bool Register(Func<T> factory, bool? throwIfExist = null)
     => Register(TypeHelper<T>.IsValueType ? () => factory() : (Func<object>)(MulticastDelegate)factory,
         throwIfExist);
    public static T Register(T value, bool? throwIfExist = null)
     => value.Coalesce(() => Register(() => (object)value,
         throwIfExist));
}
