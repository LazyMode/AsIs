using System;
using System.Collections.Concurrent;
using System.Reflection;

static class Singleton
{
    internal static ConcurrentDictionary<Type, Lazy<object>> Singletons
        = new ConcurrentDictionary<Type, Lazy<object>>();

    internal static Type[] NoType = new Type[0];
}

public static class Singleton<T>
{
    static readonly Type ThisType = typeof(T);
#if NO_TYPEINFO
    static readonly bool IsValueType = ThisType.IsValueType;
#else
    static readonly bool IsValueType = ThisType.GetTypeInfo().IsValueType;
#endif

    static Singleton()
    {
        var proxy = CreateNewHelper<T>.Proxy;
        if (proxy == null)
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(ThisType.TypeHandle);
        else
            Register(proxy(), false);
    }

    static Lazy<object> LazyAccess
    {
        get { return Singleton.Singletons[ThisType]; }
        set { Singleton.Singletons[ThisType] = value; }
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

        if (Singleton.Singletons.TryAdd(ThisType, lazy))
            return true;

        if (throwIfExist.Value)
            throw new InvalidOperationException();

        return false;
    }

    public static bool Register(Func<T> factory, bool? throwIfExist = null)
     => Register(IsValueType ? () => factory() : (Func<object>)(MulticastDelegate)factory,
         throwIfExist);
    public static T Register(T value, bool? throwIfExist = null)
     => value.Coalesce(() => Register(() => (object)value,
         throwIfExist));
}
