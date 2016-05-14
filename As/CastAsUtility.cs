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
            public Lazy<Expression<Func<object, T>>> UAsExpr;
            public Lazy<Func<object, T>> UTo;
            public Lazy<Expression<Func<object, T>>> UToExpr;

            public Lazy<Func<object, T>> CAs;
            public Lazy<Expression<Func<object, T>>> CAsExpr;
            public Lazy<Func<object, T>> CTo;
            public Lazy<Expression<Func<object, T>>> CToExpr;
        }

        static readonly ConcurrentDictionary<Type, Lazy<Item>> Dict = new ConcurrentDictionary<Type, Lazy<Item>>();

        static readonly Expression<Func<object, T>> NullExpr;
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
                NullExpr = Lambda<Func<object, T>>(Constant(null, ThisType), Param0);
                NullFunc = NullExpr.Compile();
            }
        }

        public static Item For(Type type)
        => Dict.GetOrAdd(type, new Lazy<Item>(() =>
        {
            var expr0 = Convert(Param0, type);
            var item = new Item();

            item.UToExpr = new Lazy<Expression<Func<object, T>>>(() =>
            {
                try
                {
                    var expr = !IsNullable.GetValueOrDefault() ? Convert(expr0, ThisType)
                         : Convert(Convert(expr0, TypeNonNullable), ThisType);
                    return Lambda<Func<object, T>>(expr, Param0);
                }
                catch
                {
                    type = type.BaseType;
                    if (type.IsAssignableFrom(ThisType))
                        throw;
                    return For(type).UToExpr.Value;
                }
            });
            item.UTo = new Lazy<Func<object, T>>(() => item.UToExpr.Value.Compile());

            if (IsNullable.GetValueOrDefault(true))
            {
                item.UAsExpr = new Lazy<Expression<Func<object, T>>>(() =>
                {
                    try
                    {
                        return item.UToExpr.Value;
                    }
                    catch
                    {
                        return NullExpr;
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
                item.CToExpr = item.UToExpr;
                item.CTo = item.UTo;
                item.CAsExpr = item.UAsExpr;
                item.CAs = item.UAs;

                return item;
            }

            item.CToExpr = new Lazy<Expression<Func<object, T>>>(() =>
            {
                try
                {
                    var expr = !IsNullable.GetValueOrDefault() ? ConvertChecked(expr0, ThisType)
                         : Convert(ConvertChecked(expr0, TypeNonNullable), ThisType);
                    return Lambda<Func<object, T>>(expr, Param0);
                }
                catch
                {
                    type = type.BaseType;
                    if (type.IsAssignableFrom(ThisType))
                        throw;
                    return For(type).CToExpr.Value;
                }
            });
            item.CTo = new Lazy<Func<object, T>>(() => item.CToExpr.Value.Compile());

            if (IsNullable.GetValueOrDefault(true))
            {
                item.CAsExpr = new Lazy<Expression<Func<object, T>>>(() =>
                {
                    try
                    {
                        return item.CToExpr.Value;
                    }
                    catch
                    {
                        return NullExpr;
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
        static AsProxy()
        {
            if (!CastImpl<T>.IsNullable.GetValueOrDefault(true))
                throw new NotSupportedException();
        }

        public static T As(object o, Type type)
            => CastImpl<T>.For(type).UAs.Value(o);
        public static T AsChecked(object o, Type type)
            => CastImpl<T>.For(type).CAs.Value(o);
        public static Expression<Func<object, T>> GetAsExprFor(Type type)
            => CastImpl<T>.For(type).UAsExpr.Value;
        public static Expression<Func<object, T>> GetAsExprForChecked(Type type)
            => CastImpl<T>.For(type).CAsExpr.Value;
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
        => (o == null) ? default(T) : AsProxy<T>.As(o, o.GetType());
    public static T AsChecked<T>(this object o)
        => (o == null) ? default(T) : AsProxy<T>.AsChecked(o, o.GetType());

    public static T To<T>(this object o)
        => (o == null) ? default(T) : CastImpl<T>.To(o, o.GetType());
    public static T ToChecked<T>(this object o)
        => (o == null) ? default(T) : CastImpl<T>.ToChecked(o, o.GetType());

    public static Expression<Func<object, T>> GetAsExprFor<T>(this Type type)
        => AsProxy<T>.GetAsExprFor(type);
    public static Expression<Func<object, T>> GetAsExprForChecked<T>(this Type type)
        => AsProxy<T>.GetAsExprForChecked(type);

    public static Expression<Func<object, T>> GetToExprFor<T>(this Type type)
        => CastImpl<T>.For(type).UToExpr.Value;
    public static Expression<Func<object, T>> GetToExprForChecked<T>(this Type type)
        => CastImpl<T>.For(type).CToExpr.Value;
}
