using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

class TypeHelper<T>
{
    internal static readonly Type ThisType = typeof(T);
#if NO_TYPEINFO
    internal static readonly bool IsValueType = ThisType.IsValueType;
    internal static readonly ConstructorInfo DefaultConstructor
      = typeof(T).GetConstructor(Singleton.NoType);
#else
    internal static readonly bool IsValueType = ThisType.GetTypeInfo().IsValueType;
    internal static readonly ConstructorInfo DefaultConstructor
      = typeof(T).GetTypeInfo().DeclaredConstructors.SingleOrDefault(ctor =>
        ctor.IsPublic && ctor.GetParameters().Length == 0);
#endif
    internal static readonly Func<T> Proxy
      = (DefaultConstructor != null ? Expression.Lambda<Func<T>>(Expression.New(DefaultConstructor))
        : IsValueType ? Expression.Lambda<Func<T>>(Expression.New(ThisType)) : null)?.Compile();
}
