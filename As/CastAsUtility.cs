using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

using static System.Linq.Expressions.Expression;

public static class CastAsUtility
{
    static readonly Type TypeObject = typeof(object);
    static readonly Type TypeNullable = typeof(Nullable<>);
    static readonly Type TypeIntPtr = typeof(IntPtr);
    static readonly Type TypeUIntPtr = typeof(UIntPtr);

    static readonly ParameterExpression Param0 = Parameter(TypeObject);

    static Type GetNonNullable(Type type)
    {
        if (!type.IsGenericType)
            return null;
        if (type.IsGenericTypeDefinition)
            throw new NotSupportedException();
        if (type.GetGenericTypeDefinition() != TypeNullable)
            return null;
        return type.GetGenericArguments().Single();
    }

    static bool? IsPrimitiveIntegral(Type type)
    {
        type = GetNonNullable(type) ?? type;

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
            if (ThisType.IsGenericTypeDefinition)
                throw new NotSupportedException();

            if (!ThisType.IsValueType)
                IsNullable = null;
            else if (!ThisType.IsGenericType)
                IsNullable = false;
            else
            {
                TypeNonNullable = GetNonNullable(ThisType);
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
            var nonNullableType = GetNonNullable(type);

            if (IsNullable.HasValue)
            {
                if (IsNullable.Value)
                {
                    if (nonNullableType != null)
                        throw new NotSupportedException();

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
                else
                {
                    if (nonNullableType == null)
                        expr = ConvertFactory(expr, ThisType, @checked);
                    else
                    {
                        try
                        {
                            expr = ConvertFactory(expr, ThisType, @checked);
                        }
                        catch
                        {
                            expr = ConvertFactory(expr, nonNullableType, @checked);
                            expr = Convert(expr, ThisType);
                        }
                    }
                }

                return Lambda<Func<object, T>>(expr, Param0);
            }

            try
            {
                if (nonNullableType == null)
                    expr = Convert(expr, ThisType);
                else
                {
                    try
                    {
                        expr = Convert(expr, ThisType);
                    }
                    catch
                    {
                        expr = Convert(expr, nonNullableType);
                        expr = Convert(expr, ThisType);
                    }
                }

                return Lambda<Func<object, T>>(expr, Param0);
            }
            catch
            {
                type = type.BaseType;
                if (type.IsAssignableFrom(ThisType))
                    throw;
                return For(type).CToLambda.Value;
            }
        }
        public static Item For(Type type)
        => Dict.GetOrAdd(type, new Lazy<Item>(() =>
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

    public static T As<T>(this object o, Type type)
        => AsProxy<T>.As(o, type);
    public static T AsChecked<T>(this object o, Type type)
        => AsProxy<T>.AsChecked(o, type);

    public static T To<T>(this object o, Type type)
        => CastImpl<T>.To(o, type);
    public static T ToChecked<T>(this object o, Type type)
        => CastImpl<T>.ToChecked(o, type);

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
