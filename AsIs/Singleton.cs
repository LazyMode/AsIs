using System;
using System.Collections.Concurrent;
using System.Reflection;

using static TypeHelper;

public enum SingletonOverwriting
{
    ThrowIfExist,
    ReturnWhenFail,
    SkipOverConflict,
    OverwriteAllTheWay,
}

static class Singleton
{
    internal static ConcurrentDictionary<Type, Lazy<object>> Singletons
        = new ConcurrentDictionary<Type, Lazy<object>>();

    internal static Type[] NoType = new Type[0];

    internal static bool IsAmbiguous(this Type type)
    {
        if (type == TypeObject)
            return true;
        if (type == TypeValueType)
            return true;
        if (type == TypeEnum)
            return true;
        if (type == TypeMulticastDelegate)
            return true;
        if (type == TypeDelegate)
            return true;
        return false;
    }
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

    static bool? Register(Func<object> factory, SingletonOverwriting behavior, Type ancestor = null)
    {
        var type = TypeHelper<T>.ThisType;
        if (type.IsAmbiguous())
            throw new InvalidOperationException();

        var ancestorInfo = ancestor?.GetTypeInfo();
        var typeInfo = TypeHelper<T>.ThisTypeInfo;
        if (ancestorInfo != null)
        {
            if (!ancestorInfo.IsAssignableFrom(typeInfo))
                throw new ArgumentException();
            if (ancestor.IsAmbiguous() || ancestor == TypeVoid)
                throw new ArgumentException();
        }

        var hasSucc = false;
        var hasFail = false;
        var lazy = new Lazy<object>(factory);

        for (;;)
        {
            if (behavior == SingletonOverwriting.OverwriteAllTheWay)
                Singleton.Singletons[type] = lazy;
            else if (Singleton.Singletons.TryAdd(type, lazy))
                hasSucc = true;
            else
            {
                if (behavior == SingletonOverwriting.ThrowIfExist)
                    throw new InvalidOperationException();

                hasFail = true;

                if (behavior == SingletonOverwriting.ReturnWhenFail)
                    break;
            }

            if (type == ancestor) break;
            type = typeInfo.BaseType;
            if (type == null || type.IsAmbiguous()) break;
            typeInfo = type.GetTypeInfo();
        }

        if (!hasFail) return true;
        if (!hasSucc) return false;
        return null;
    }
    public static bool? Register(Func<T> factory, SingletonOverwriting behavior, Type ancestor = null)
     => Register(TypeHelper<T>.IsValueType ? () => factory() : (Func<object>)(MulticastDelegate)factory,
         behavior, ancestor);
    public static bool? Register(T value, SingletonOverwriting behavior, Type ancestor = null)
     => Register(() => (object)value, behavior, ancestor);
}
