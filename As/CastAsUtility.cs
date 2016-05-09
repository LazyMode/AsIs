using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

using static System.Linq.Expressions.Expression;

public static class CastAsUtility
{
    static readonly Type TypeObject = typeof(object);
    static readonly Type TypeNullable = typeof(Nullable<>);

    static readonly ParameterExpression Param0 = Parameter(TypeObject);

    static class CastImpl<T>
    {
        public class Item
        {
            public Lazy<Func<object, T>> UAs;
            public Lazy<Expression<Func<object, T>>> UAsExpr;
            public Lazy<Func<object, T>> UCast;
            public Lazy<Expression<Func<object, T>>> UCastExpr;
        }

        static readonly ConcurrentDictionary<Type, Lazy<Item>> Dict = new ConcurrentDictionary<Type, Lazy<Item>>();

        public static readonly Type TypeT = typeof(T);
        public static readonly bool? IsNullable;

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

            // T can not be null
            if (!IsNullable.GetValueOrDefault(true))
                return item;

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

            return item;
        })).Value;

        public static T TypeCast(object o, Type type)
            => For(type).UCast.Value(o);
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
        public static Expression<Func<object, T>> GetAsExprFor(Type type)
            => CastImpl<T>.For(type).UAsExpr.Value;
    }

    public static T TypeAs<T>(this object o, Type type)
        => AsProxy<T>.TypeAs(o, type);

    public static T TypeCast<T>(this object o, Type type)
        => CastImpl<T>.TypeCast(o, type);

    public static T As<T>(this object o)
        => (o == null) ? default(T) : AsProxy<T>.TypeAs(o, o.GetType());

    public static T Cast<T>(this object o)
        => (o == null) ? default(T) : CastImpl<T>.TypeCast(o, o.GetType());

    public static Expression<Func<object, T>> GetAsExprFor<T>(this Type type)
        => AsProxy<T>.GetAsExprFor(type);
    public static Expression<Func<object, T>> GetCastExprFor<T>(this Type type)
        => CastImpl<T>.For(type).UCastExpr.Value;
}
