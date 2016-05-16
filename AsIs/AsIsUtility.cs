using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

#if NO_TYPEINFO
using TypeInfo = System.Type;
#endif

public static class AsIsUtility
{
    static readonly Type TypeObject = typeof(object);
    static readonly Type TypeNullable = typeof(Nullable<>);
    static readonly Type TypeIntPtr = typeof(IntPtr);
    static readonly Type TypeUIntPtr = typeof(UIntPtr);

    static readonly ParameterExpression Param0 = Parameter(TypeObject);

    static Type GetNonNullable(TypeInfo type)
    {
        if (!type.IsGenericType)
            return null;
        if (type.IsGenericTypeDefinition)
            throw new NotSupportedException();
        if (type.GetGenericTypeDefinition() != TypeNullable)
            return null;

#if NO_TYPEINFO
        return type.GetGenericArguments().Single();
#else
        return type.GenericTypeArguments.Single();
#endif
    }

#if NO_TYPECODE
    static readonly Type TypeSingle = typeof(float);
    static readonly Type TypeDouble = typeof(double);

    static readonly Type TypeByte = typeof(byte);
    static readonly Type TypeSByte = typeof(sbyte);
    static readonly Type TypeInt16 = typeof(short);
    static readonly Type TypeUInt16 = typeof(ushort);
    static readonly Type TypeInt32 = typeof(int);
    static readonly Type TypeUInt32 = typeof(uint);
    static readonly Type TypeInt64 = typeof(long);
    static readonly Type TypeUInt64 = typeof(ulong);
    static readonly Type TypeChar = typeof(char);

    static bool? IsPrimitiveIntegral(Type type)
    {
        if (type == TypeSingle || type == TypeDouble)
            return false;

        if (type == TypeIntPtr || type == TypeUIntPtr)
            return true;

        if (type == TypeByte)
            return true;
        if (type == TypeSByte)
            return true;
        if (type == TypeInt16)
            return true;
        if (type == TypeUInt16)
            return true;
        if (type == TypeInt32)
            return true;
        if (type == TypeUInt32)
            return true;
        if (type == TypeInt64)
            return true;
        if (type == TypeUInt64)
            return true;
        if (type == TypeChar)
            return true;

        return null;
    }
#else
    static bool? IsPrimitiveIntegral(Type type)
    {
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Char:
                return true;

            case TypeCode.Single:
            case TypeCode.Double:
                return false;
        }

        if (type == TypeIntPtr || type == TypeUIntPtr)
            return true;

        return null;
    }
#endif

    static class CastImpl<T>
    {
        public static readonly Type ThisType = typeof(T);
        public static readonly Type TypeNonNullable;
        public static readonly bool? IsPrimitiveIntegral = IsPrimitiveIntegral(ThisType);
        public static readonly bool? IsNullable;

        public class Item
        {
            public Lazy<Func<object, T>> UAs;
            public Lazy<Expression<Func<object, T>>> UAsLambda;
            public Lazy<Func<object, T>> UTo;
            public Lazy<Expression<Func<object, T>>> UToLambda;

            public Lazy<Func<object, T>> CAs;
            public Lazy<Expression<Func<object, T>>> CAsLambda;
            public Lazy<Func<object, T>> CTo;
            public Lazy<Expression<Func<object, T>>> CToLambda;
        }

        static readonly ConcurrentDictionary<Type, Lazy<Item>> Dict = new ConcurrentDictionary<Type, Lazy<Item>>();

        static readonly Expression<Func<object, T>> NullLambda;
        static readonly Func<object, T> NullFunc;

        static CastImpl()
        {
#if NO_TYPEINFO
            var ti = ThisType;
#else
            var ti = ThisType.GetTypeInfo();
#endif
            if (ti.IsGenericTypeDefinition)
                throw new NotSupportedException();

            if (!ti.IsValueType)
                IsNullable = null;
            else if (!ti.IsGenericType)
                IsNullable = false;
            else
            {
                TypeNonNullable = GetNonNullable(ti);
                IsNullable = TypeNonNullable != null;
            }

            if (IsNullable.GetValueOrDefault(true))
            {
                NullLambda = Lambda<Func<object, T>>(Constant(null, ThisType), Param0);
                NullFunc = NullLambda.Compile();
            }
        }

        static UnaryExpression ConvertFactory(Expression expr, Type type, bool @checked)
            => @checked ? ConvertChecked(expr, type) : Convert(expr, type);
        static Expression<Func<object, T>> LambdaFactory(Expression expr, Type type, bool @checked)
        {
            if (IsNullable.HasValue)
            {
                if (!IsNullable.Value)
                    expr = ConvertFactory(expr, ThisType, @checked);
                else
                {
                    try
                    {
                        expr = ConvertFactory(expr, ThisType, @checked);
                    }
                    catch
                    {
                        expr = ConvertFactory(expr, TypeNonNullable, @checked);
                        expr = Convert(expr, ThisType);
                    }
                }

                return Lambda<Func<object, T>>(expr, Param0);
            }

            try
            {
                expr = Convert(expr, ThisType);

                return Lambda<Func<object, T>>(expr, Param0);
            }
            catch
            {
#if NO_TYPEINFO
                type = type.BaseType;
                if (type.IsAssignableFrom(ThisType))
                    throw;
#else
                type = type.GetTypeInfo().BaseType;
                if (type == ThisType || ThisType.GetTypeInfo().IsSubclassOf(type))
                    throw;
#endif
                return For(type).CToLambda.Value;
            }
        }
        static Item TupleFactory(Type type)
        {
            var expr0 = Convert(Param0, type);
            var item = new Item();

            item.UToLambda = new Lazy<Expression<Func<object, T>>>(() => LambdaFactory(expr0, type, false));
            item.UTo = new Lazy<Func<object, T>>(() => item.UToLambda.Value.Compile());

            if (IsNullable.GetValueOrDefault(true))
            {
                item.UAsLambda = new Lazy<Expression<Func<object, T>>>(() =>
                {
                    try
                    {
                        return item.UToLambda.Value;
                    }
                    catch
                    {
                        return NullLambda;
                    }
                });
                item.UAs = new Lazy<Func<object, T>>(() =>
                {
                    try
                    {
                        return item.UTo.Value;
                    }
                    catch
                    {
                        return NullFunc;
                    }
                });
            }

            var useChecked = IsPrimitiveIntegral.GetValueOrDefault() && IsPrimitiveIntegral(type).HasValue;
            if (!useChecked)
            {
                item.CToLambda = item.UToLambda;
                item.CTo = item.UTo;
                item.CAsLambda = item.UAsLambda;
                item.CAs = item.UAs;

                return item;
            }

            item.CToLambda = new Lazy<Expression<Func<object, T>>>(() => LambdaFactory(expr0, type, true));
            item.CTo = new Lazy<Func<object, T>>(() => item.CToLambda.Value.Compile());

            if (IsNullable.GetValueOrDefault(true))
            {
                item.CAsLambda = new Lazy<Expression<Func<object, T>>>(() =>
                {
                    try
                    {
                        return item.CToLambda.Value;
                    }
                    catch
                    {
                        return NullLambda;
                    }
                });
                item.CAs = new Lazy<Func<object, T>>(() =>
                {
                    try
                    {
                        return item.CTo.Value;
                    }
                    catch
                    {
                        return NullFunc;
                    }
                });
            }

            return item;
        }
        public static Item For(Type type)
        => Dict.GetOrAdd(type, new Lazy<Item>(() =>
        {
#if NO_TYPEINFO
            var underlying = GetNonNullable(type);
#else
            var underlying = GetNonNullable(type.GetTypeInfo());
#endif
            if (underlying != null)
                return For(underlying);
            return TupleFactory(type);
        })).Value;

        public static T To(object o, Type type)
            => For(type).UTo.Value(o);
        public static T ToChecked(object o, Type type)
            => For(type).CTo.Value(o);
    }

    static class AsProxy<T>
    {
        public static readonly T Default;

        static AsProxy()
        {
            if (!CastImpl<T>.IsNullable.GetValueOrDefault(true))
                throw new NotSupportedException();
        }

        public static T As(object o, Type type)
            => CastImpl<T>.For(type).UAs.Value(o);
        public static T AsChecked(object o, Type type)
            => CastImpl<T>.For(type).CAs.Value(o);
        public static Expression<Func<object, T>> GetAsLambdaFor(Type type)
            => CastImpl<T>.For(type).UAsLambda.Value;
        public static Expression<Func<object, T>> GetAsLambdaForChecked(Type type)
            => CastImpl<T>.For(type).CAsLambda.Value;
    }

    public static T As<T>(this object o)
        => (o == null) ? AsProxy<T>.Default : AsProxy<T>.As(o, o.GetType());
    public static T AsChecked<T>(this object o)
        => (o == null) ? AsProxy<T>.Default : AsProxy<T>.AsChecked(o, o.GetType());

    public static T To<T>(this object o)
        => (o == null) ? AsProxy<T>.Default : CastImpl<T>.To(o, o.GetType());
    public static T ToChecked<T>(this object o)
        => (o == null) ? AsProxy<T>.Default : CastImpl<T>.ToChecked(o, o.GetType());

    public static Expression<Func<object, T>> GetAsLambdaFor<T>(this Type type)
        => AsProxy<T>.GetAsLambdaFor(type);
    public static Expression<Func<object, T>> GetAsLambdaForChecked<T>(this Type type)
        => AsProxy<T>.GetAsLambdaForChecked(type);

    public static Expression<Func<object, T>> GetToLambdaFor<T>(this Type type)
        => CastImpl<T>.For(type).UToLambda.Value;
    public static Expression<Func<object, T>> GetToLambdaForChecked<T>(this Type type)
        => CastImpl<T>.For(type).CToLambda.Value;
}
