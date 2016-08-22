using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

class CreateNewHelper<T>
{
#if NO_TYPEINFO
    internal static readonly ConstructorInfo DefaultConstructor
      = typeof(T).GetConstructor(Singleton.NoType);
#else
    internal static readonly ConstructorInfo DefaultConstructor
      = typeof(T).GetTypeInfo().DeclaredConstructors.SingleOrDefault(ctor =>
        ctor.IsPublic && ctor.GetParameters().Length == 0);
#endif
    internal static readonly Func<T> Proxy
      = (DefaultConstructor == null) ? null : Expression.Lambda<Func<T>>(Expression.New(DefaultConstructor)).Compile();
}
