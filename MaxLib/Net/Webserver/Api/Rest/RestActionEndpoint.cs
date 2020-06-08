using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver.Api.Rest
{
    public class RestActionEndpoint : RestEndpoint
    {
        public Func<Dictionary<string, object>, Task<HttpDataSource>> HandleRequest { get; set; }

        public RestActionEndpoint(Func<Dictionary<string, object>, Task<HttpDataSource>> handleRequest)
            => HandleRequest = handleRequest;

        public override Task<HttpDataSource> GetSource(Dictionary<string, object> args)
        {
            _ = args ?? throw new ArgumentNullException(nameof(args));
            return HandleRequest?.Invoke(args);
        }

        public static RestActionEndpoint Create(Func<Dictionary<string, object>, Task<HttpDataSource>> handler)
            => new RestActionEndpoint(handler);

        public static RestActionEndpoint Create(Func<Dictionary<string, object>, Task<Stream>> handler)
            => new RestActionEndpoint(async args =>
            {
                var result = await handler(args);
                if (result == null)
                    return null;
                return new HttpStreamDataSource(result);
            });

        public static RestActionEndpoint Create(Func<Dictionary<string, object>, Task<string>> handler)
            => new RestActionEndpoint(async args =>
            {
                var result = await handler(args);
                if (result == null)
                    return null;
                return new HttpStringDataSource(result);
            });

        public static RestActionEndpoint Create(Func<Task<HttpDataSource>> handler)
            => new RestActionEndpoint(async args =>
            {
                var result = await handler();
                if (result == null)
                    return null;
                return result;
            });

        public static RestActionEndpoint Create(Func<Task<Stream>> handler)
            => new RestActionEndpoint(async args =>
            {
                var result = await handler();
                if (result == null)
                    return null;
                return new HttpStreamDataSource(result);
            });

        public static RestActionEndpoint Create(Func<Task<string>> handler)
            => new RestActionEndpoint(async args =>
            {
                var result = await handler();
                if (result == null)
                    return null;
                return new HttpStringDataSource(result);
            });

        public static RestActionEndpoint Create<T>(Func<T, Task<HttpDataSource>> handler, string argName)
            => new RestActionEndpoint(async args =>
            {
                T arg = default;
                if (!args.TryGetValue(argName, out object rawArg))
                    if (rawArg is T)
                        arg = (T)rawArg;
                var result = await handler(arg);
                if (result == null)
                    return null;
                return result;
            });

        public static RestActionEndpoint Create<T>(Func<T, Task<Stream>> handler, string argName)
            => new RestActionEndpoint(async args =>
            {
                T arg = default;
                if (!args.TryGetValue(argName, out object rawArg))
                    if (rawArg is T)
                        arg = (T)rawArg;
                var result = await handler(arg);
                if (result == null)
                    return null;
                return new HttpStreamDataSource(result);
            });

        public static RestActionEndpoint Create<T>(Func<T, Task<string>> handler, string argName)
            => new RestActionEndpoint(async args =>
            {
                T arg = default;
                if (!args.TryGetValue(argName, out object rawArg))
                    if (rawArg is T)
                        arg = (T)rawArg;
                var result = await handler(arg);
                if (result == null)
                    return null;
                return new HttpStringDataSource(result);
            });

        public static RestActionEndpoint Create<T1, T2>(Func<T1, T2, Task<HttpDataSource>> handler, string argName1, string argName2)
            => new RestActionEndpoint(async args =>
            {
                T1 arg1 = default;
                if (!args.TryGetValue(argName1, out object rawArg))
                    if (rawArg is T1)
                        arg1 = (T1)rawArg;
                T2 arg2 = default;
                if (!args.TryGetValue(argName2, out object rawArg2))
                    if (rawArg is T2)
                        arg2 = (T2)rawArg2;
                var result = await handler(arg1, arg2);
                if (result == null)
                    return null;
                return result;
            });

        public static RestActionEndpoint Create<T1, T2>(Func<T1, T2, Task<Stream>> handler, string argName1, string argName2)
            => new RestActionEndpoint(async args =>
            {
                T1 arg1 = default;
                if (!args.TryGetValue(argName1, out object rawArg))
                    if (rawArg is T1)
                        arg1 = (T1)rawArg;
                T2 arg2 = default;
                if (!args.TryGetValue(argName2, out object rawArg2))
                    if (rawArg is T2)
                        arg2 = (T2)rawArg2;
                var result = await handler(arg1, arg2);
                if (result == null)
                    return null;
                return new HttpStreamDataSource(result);
            });

        public static RestActionEndpoint Create<T1, T2>(Func<T1, T2, Task<string>> handler, string argName1, string argName2)
            => new RestActionEndpoint(async args =>
            {
                T1 arg1 = default;
                if (!args.TryGetValue(argName1, out object rawArg))
                    if (rawArg is T1)
                        arg1 = (T1)rawArg;
                T2 arg2 = default;
                if (!args.TryGetValue(argName2, out object rawArg2))
                    if (rawArg is T2)
                        arg2 = (T2)rawArg2;
                var result = await handler(arg1, arg2);
                if (result == null)
                    return null;
                return new HttpStringDataSource(result);
            });

        public static RestActionEndpoint Create<T1, T2, T3>(Func<T1, T2, T3, Task<HttpDataSource>> handler, string argName1, string argName2, string argName3)
            => new RestActionEndpoint(async args =>
            {
                T1 arg1 = default;
                if (!args.TryGetValue(argName1, out object rawArg))
                    if (rawArg is T1)
                        arg1 = (T1)rawArg;
                T2 arg2 = default;
                if (!args.TryGetValue(argName2, out object rawArg2))
                    if (rawArg is T2)
                        arg2 = (T2)rawArg2;
                T3 arg3 = default;
                if (!args.TryGetValue(argName3, out object rawArg3))
                    if (rawArg is T3)
                        arg3 = (T3)rawArg3;
                var result = await handler(arg1, arg2, arg3);
                if (result == null)
                    return null;
                return result;
            });

        public static RestActionEndpoint Create<T1, T2, T3>(Func<T1, T2, T3, Task<Stream>> handler, string argName1, string argName2, string argName3)
            => new RestActionEndpoint(async args =>
            {
                T1 arg1 = default;
                if (!args.TryGetValue(argName1, out object rawArg))
                    if (rawArg is T1)
                        arg1 = (T1)rawArg;
                T2 arg2 = default;
                if (!args.TryGetValue(argName2, out object rawArg2))
                    if (rawArg is T2)
                        arg2 = (T2)rawArg2;
                T3 arg3 = default;
                if (!args.TryGetValue(argName3, out object rawArg3))
                    if (rawArg is T3)
                        arg3 = (T3)rawArg3;
                var result = await handler(arg1, arg2, arg3);
                if (result == null)
                    return null;
                return new HttpStreamDataSource(result);
            });

        public static RestActionEndpoint Create<T1, T2, T3>(Func<T1, T2, T3, Task<string>> handler, string argName1, string argName2, string argName3)
            => new RestActionEndpoint(async args =>
            {
                T1 arg1 = default;
                if (!args.TryGetValue(argName1, out object rawArg))
                    if (rawArg is T1)
                        arg1 = (T1)rawArg;
                T2 arg2 = default;
                if (!args.TryGetValue(argName2, out object rawArg2))
                    if (rawArg is T2)
                        arg2 = (T2)rawArg2;
                T3 arg3 = default;
                if (!args.TryGetValue(argName3, out object rawArg3))
                    if (rawArg is T3)
                        arg3 = (T3)rawArg3;
                var result = await handler(arg1, arg2, arg3);
                if (result == null)
                    return null;
                return new HttpStringDataSource(result);
            });

        public static RestActionEndpoint Create(Delegate handler, string[] argsOrder)
            => new RestActionEndpoint(async args =>
            {
                var use = new object[argsOrder?.Length ?? 0];
                for (int i = 0; i < use.Length; ++i)
                    if (args.TryGetValue(argsOrder[i], out object value))
                        use[i] = value;
                var result = handler.DynamicInvoke(use);
                if (result is Task task)
                {
                    await task;
                    if (!GetTaskValue(task, out result))
                        result = null;
                }
                if (result is HttpDataSource dataSource)
                    return dataSource;
                if (result is Stream stream)
                    return new HttpStreamDataSource(stream);
                if (result is string text)
                    return new HttpStringDataSource(text);
                var resText = result?.ToString() ?? "";
                if (result is IDisposable disposable)
                    disposable.Dispose();
                return new HttpStringDataSource(resText);
            });

        private static bool GetTaskValue(Task task, out object value)
        {
            // thx to: https://stackoverflow.com/a/52500763/12469007
            value = default;
            var voidTaskType = typeof(Task<>).MakeGenericType(Type.GetType("System.Threading.Tasks.VoidTaskResult"));
            if (voidTaskType.IsAssignableFrom(task.GetType()))
                return false;
            var property = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return false;
            value = property.GetValue(task);
            return true;
        }
    }
}
