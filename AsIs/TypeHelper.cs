using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

#if NO_TYPEINFO
using TypeInfo = System.Type;
#endif

static class TypeHelper
{
    internal static readonly Type TypeObject = typeof(object);
    internal static readonly Type TypeValueType = typeof(ValueType);
    internal static readonly Type TypeEnum = typeof(Enum);
    internal static readonly Type TypeDelegate = typeof(Delegate);
    internal static readonly Type TypeMulticastDelegate = typeof(MulticastDelegate);
    internal static readonly Type TypeVoid = typeof(void);

    internal static readonly Type TypeNullable = typeof(Nullable<>);
    internal static readonly Type TypeIntPtr = typeof(IntPtr);
    internal static readonly Type TypeUIntPtr = typeof(UIntPtr);
    
    internal static readonly Type TypeSingle = typeof(float);
    internal static readonly Type TypeDouble = typeof(double);
    
    internal static readonly Type TypeByte = typeof(byte);
    internal static readonly Type TypeSByte = typeof(sbyte);
    internal static readonly Type TypeInt16 = typeof(short);
    internal static readonly Type TypeUInt16 = typeof(ushort);
    internal static readonly Type TypeInt32 = typeof(int);
    internal static readonly Type TypeUInt32 = typeof(uint);
    internal static readonly Type TypeInt64 = typeof(long);
    internal static readonly Type TypeUInt64 = typeof(ulong);
    internal static readonly Type TypeChar = typeof(char);

#if NO_TYPEINFO
    internal static TypeInfo GetTypeInfo(this Type type) => type;
#endif
}

class TypeHelper<T>
{
    internal static readonly Type ThisType = typeof(T);
    internal static readonly TypeInfo ThisTypeInfo = ThisType.GetTypeInfo();
#if NO_TYPEINFO
    internal static readonly bool IsValueType = ThisType.IsValueType;
    internal static readonly ConstructorInfo DefaultConstructor
      = typeof(T).GetConstructor(Singleton.NoType);
#else
    internal static readonly bool IsValueType = ThisTypeInfo.IsValueType;
    internal static readonly ConstructorInfo DefaultConstructor
      = ThisTypeInfo.DeclaredConstructors.SingleOrDefault(ctor =>
        ctor.IsPublic && ctor.GetParameters().Length == 0);
#endif
    internal static readonly Func<T> Proxy
      = (DefaultConstructor != null ? Expression.Lambda<Func<T>>(Expression.New(DefaultConstructor))
        : IsValueType ? Expression.Lambda<Func<T>>(Expression.New(ThisType)) : null)?.Compile();
}
