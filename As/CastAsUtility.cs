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

    static class CastImpl<T>
    {
        public static readonly Type TypeT = typeof(T);
        public static readonly bool? IsPrimitiveIntegral = IsPrimitiveIntegral(TypeT);
        public static readonly bool? IsNullable;

        public class Item
        {
            public Lazy<Func<object, T>> UAs;
            public Lazy<Expression<Func<object, T>>> UAsExpr;
            public Lazy<Func<object, T>> UCast;
            public Lazy<Expression<Func<object, T>>> UCastExpr;

            public Lazy<Func<object, T>> CAs;
            public Lazy<Expression<Func<object, T>>> CAsExpr;
            public Lazy<Func<object, T>> CCast;
            public Lazy<Expression<Func<object, T>>> CCastExpr;
        }

        static readonly ConcurrentDictionary<Type, Lazy<Item>> Dict = new ConcurrentDictionary<Type, Lazy<Item>>();

        static readonly Expression<Func<object, T>> NullExpr;
        static readonly Func<object, T> NullFunc;

        static CastImpl()
        {
            if (TypeT.IsGenericTypeDefinition)
                throw new NotSupportedException();

            if (!TypeT.IsValueType)
                IsNullable = null;
            else if (!TypeT.IsGenericType)
                IsNullable = false;
            else
            {
                IsNullable = (TypeT.GetGenericTypeDefinition() == TypeNullable);
            }

            if (IsNullable.GetValueOrDefault(true))
            {
                NullExpr = Lambda<Func<object, T>>(Constant(null, TypeT), Param0);
                NullFunc = NullExpr.Compile();
            }
        }

        public static Item For(Type type)
        => Dict.GetOrAdd(type, new Lazy<Item>(() =>
        {
            var item = new Item();

            item.UCastExpr = new Lazy<Expression<Func<object, T>>>(() =>
            {
                try
                {
                    var expr = Convert(Param0, type);
                    if (IsNullable.GetValueOrDefault())
                        expr = Convert(expr, TypeT.GetGenericArguments().Single());
                    return Lambda<Func<object, T>>(Convert(expr, TypeT), Param0);
                }
                catch
                {
                    type = type.BaseType;
                    if (type.IsAssignableFrom(TypeT))
                        throw;
                    return For(type).UCastExpr.Value;
                }
            });
            item.UCast = new Lazy<Func<object, T>>(() => item.UCastExpr.Value.Compile());

            if (IsNullable.GetValueOrDefault(true))
            {
                item.UAsExpr = new Lazy<Expression<Func<object, T>>>(() =>
                {
                    try
                    {
                        return item.UCastExpr.Value;
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
                        return item.UCast.Value;
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
                item.CCastExpr = item.UCastExpr;
                item.CCast = item.UCast;
                item.CAsExpr = item.UAsExpr;
                item.CAs = item.UAs;

                return item;
            }

            item.CCastExpr = new Lazy<Expression<Func<object, T>>>(() =>
            {
                try
                {
                    var expr = Convert(Param0, type);
                    expr = (!IsNullable.GetValueOrDefault()) ? ConvertChecked(expr, TypeT)
                         : Convert(ConvertChecked(expr, TypeT.GetGenericArguments().Single()), TypeT);
                    return Lambda<Func<object, T>>(expr, Param0);
                }
                catch
                {
                    type = type.BaseType;
                    if (type.IsAssignableFrom(TypeT))
                        throw;
                    return For(type).CCastExpr.Value;
                }
            });
            item.CCast = new Lazy<Func<object, T>>(() => item.CCastExpr.Value.Compile());

            if (IsNullable.GetValueOrDefault(true))
            {
                item.CAsExpr = new Lazy<Expression<Func<object, T>>>(() =>
              {
                  try
                  {
                      return item.CCastExpr.Value;
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
                        return item.CCast.Value;
                    }
                    catch
                    {
                        return NullFunc;
                    }
                });
            }

            return item;
        })).Value;

        public static T TypeCast(object o, Type type)
            => For(type).UCast.Value(o);
        public static T TypeCastChecked(object o, Type type)
            => For(type).CCast.Value(o);
    }

    static class AsProxy<T>
    {
        static AsProxy()
        {
            if (!CastImpl<T>.IsNullable.GetValueOrDefault(true))
                throw new NotSupportedException();
        }

        public static T TypeAs(object o, Type type)
            => CastImpl<T>.For(type).UAs.Value(o);
        public static T TypeAsChecked(object o, Type type)
            => CastImpl<T>.For(type).CAs.Value(o);
        public static Expression<Func<object, T>> GetAsExprFor(Type type)
            => CastImpl<T>.For(type).UAsExpr.Value;
        public static Expression<Func<object, T>> GetAsExprForChecked(Type type)
            => CastImpl<T>.For(type).CAsExpr.Value;
    }

    public static T TypeAs<T>(this object o, Type type)
        => AsProxy<T>.TypeAs(o, type);
    public static T TypeAsChecked<T>(this object o, Type type)
        => AsProxy<T>.TypeAsChecked(o, type);

    public static T TypeCast<T>(this object o, Type type)
        => CastImpl<T>.TypeCast(o, type);
    public static T TypeCastChecked<T>(this object o, Type type)
        => CastImpl<T>.TypeCastChecked(o, type);

    public static T As<T>(this object o)
        => (o == null) ? default(T) : AsProxy<T>.TypeAs(o, o.GetType());
    public static T AsChecked<T>(this object o)
        => (o == null) ? default(T) : AsProxy<T>.TypeAsChecked(o, o.GetType());

    public static T Cast<T>(this object o)
        => (o == null) ? default(T) : CastImpl<T>.TypeCast(o, o.GetType());
    public static T CastChecked<T>(this object o)
        => (o == null) ? default(T) : CastImpl<T>.TypeCastChecked(o, o.GetType());

    public static Expression<Func<object, T>> GetAsExprFor<T>(this Type type)
        => AsProxy<T>.GetAsExprFor(type);
    public static Expression<Func<object, T>> GetAsExprForChecked<T>(this Type type)
        => AsProxy<T>.GetAsExprForChecked(type);

    public static Expression<Func<object, T>> GetCastExprFor<T>(this Type type)
        => CastImpl<T>.For(type).UCastExpr.Value;
    public static Expression<Func<object, T>> GetCastExprForChecked<T>(this Type type)
        => CastImpl<T>.For(type).CCastExpr.Value;
}
