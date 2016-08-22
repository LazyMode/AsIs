using System;

public static partial class AsIs
{
    public static Func<T> GetCreationDelegateFor<T>() => TypeHelper<T>.Proxy;
    public static T CreateNew<T>() where T : new() => GetCreationDelegateFor<T>()();

    public static T Coalesce<T>(this T self, Action action)
    {
        action();
        return self;
    }
    public static T Coalesce<T>(this T self, Action<T> action)
    {
        action(self);
        return self;
    }
    public static T Run<T>(Func<T> func) => func();
    public static void Run(Action logic) => logic();
}
