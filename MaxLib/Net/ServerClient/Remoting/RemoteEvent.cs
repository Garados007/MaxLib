using System;
using System.Collections.Generic;

namespace MaxLib.Net.ServerClient.Remoting
{
    [Serializable]
    public class RemoteEvent<T> : MarshalByRefObject where T : class
    {
        readonly List<T> EventHandler = new List<T>();

        public void AddHandler(T Handler)
        {
            EventHandler.Add(Handler);
        }
        public void RemoveHandler(T Handler)
        {
            EventHandler.Remove(Handler);
        }

        public virtual object Invoke(params object[] arguments)
        {
            object result = null;
            foreach (var eh in EventHandler) result = (eh as Delegate).DynamicInvoke(arguments);
            return result;
        }

        public RemoteEvent() { }
        public RemoteEvent(Delegate ev)
        {
            EventHandler.Add(ev as T);
        }
        public RemoteEvent(params Delegate[] evs)
        {
            foreach (var ev in evs) EventHandler.Add(ev as T);
        }
        static RemoteEvent()
        {
            if (!typeof(T).IsSubclassOf(typeof(Delegate)))
            {
                throw new InvalidOperationException(typeof(T).Name + " is not a delegate type");
            }
        }
    }

    public class RemoteEvent: RemoteEvent<Delegate> { }

    public class RemoteEventArgs : RemoteEvent<EventHandler> { }
    public class RemoteEventArgs<T> : RemoteEvent<EventHandler<T>> where T : EventArgs { }

    public class RemoteAction<T> : RemoteEvent<Action<T>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T> act) : base(act) { }
        public RemoteAction(params Action<T>[] act) : base(act) { }
        public void Invoke(T arg)
        {
            base.Invoke(arg);
        }
    }
    public class RemoteAction<T1, T2> : RemoteEvent<Action<T1, T2>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2> act) : base(act) { }
        public RemoteAction(params Action<T1, T2>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2)
        {
            base.Invoke(arg1, arg2);
        }
    }
    public class RemoteAction<T1, T2, T3> : RemoteEvent<Action<T1, T2, T3>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3)
        {
            base.Invoke(arg1, arg2, arg3);
        }
    }
    public class RemoteAction<T1, T2, T3, T4> : RemoteEvent<Action<T1, T2, T3, T4>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3, T4> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3, T4>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            base.Invoke(arg1, arg2, arg3, arg4);
        }
    }
    public class RemoteAction<T1, T2, T3, T4, T5> : RemoteEvent<Action<T1, T2, T3, T4, T5>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3, T4, T5> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3, T4, T5>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5);
        }
    }
    public class RemoteAction<T1, T2, T3, T4, T5, T6> : RemoteEvent<Action<T1, T2, T3, T4, T5, T6>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3, T4, T5, T6> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3, T4, T5, T6>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);
        }
    }
    public class RemoteAction<T1, T2, T3, T4, T5, T6, T7> : RemoteEvent<Action<T1, T2, T3, T4, T5, T6, T7>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3, T4, T5, T6, T7> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3, T4, T5, T6, T7>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
    }
    public class RemoteAction<T1, T2, T3, T4, T5, T6, T7, T8> : RemoteEvent<Action<T1, T2, T3, T4, T5, T6, T7, T8>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3, T4, T5, T6, T7, T8> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3, T4, T5, T6, T7, T8>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
    }
    public class RemoteAction<T1, T2, T3, T4, T5, T6, T7, T8, T9> : RemoteEvent<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
    }
    public class RemoteAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : RemoteEvent<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }
    }
    public class RemoteAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : RemoteEvent<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }
    }
    public class RemoteAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : RemoteEvent<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }
    }
    public class RemoteAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : RemoteEvent<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }
    }
    public class RemoteAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : RemoteEvent<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }
    }
    public class RemoteAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : RemoteEvent<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }
    }
    public class RemoteAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : RemoteEvent<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>>
    {
        public RemoteAction() { }
        public RemoteAction(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> act) : base(act) { }
        public RemoteAction(params Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }
    }
    public class RemoteFunc<T> : RemoteEvent<Func<T>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T> act) : base(act) { }
        public RemoteFunc(params Func<T>[] act) : base(act) { }
        public void Invoke(T arg)
        {
            base.Invoke(arg);
        }
    }
    public class RemoteFunc<T1, T2> : RemoteEvent<Func<T1, T2>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2)
        {
            base.Invoke(arg1, arg2);
        }
    }
    public class RemoteFunc<T1, T2, T3> : RemoteEvent<Func<T1, T2, T3>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3)
        {
            base.Invoke(arg1, arg2, arg3);
        }
    }
    public class RemoteFunc<T1, T2, T3, T4> : RemoteEvent<Func<T1, T2, T3, T4>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3, T4> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3, T4>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            base.Invoke(arg1, arg2, arg3, arg4);
        }
    }
    public class RemoteFunc<T1, T2, T3, T4, T5> : RemoteEvent<Func<T1, T2, T3, T4, T5>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3, T4, T5> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3, T4, T5>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5);
        }
    }
    public class RemoteFunc<T1, T2, T3, T4, T5, T6> : RemoteEvent<Func<T1, T2, T3, T4, T5, T6>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3, T4, T5, T6> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3, T4, T5, T6>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);
        }
    }
    public class RemoteFunc<T1, T2, T3, T4, T5, T6, T7> : RemoteEvent<Func<T1, T2, T3, T4, T5, T6, T7>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3, T4, T5, T6, T7> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3, T4, T5, T6, T7>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
    }
    public class RemoteFunc<T1, T2, T3, T4, T5, T6, T7, T8> : RemoteEvent<Func<T1, T2, T3, T4, T5, T6, T7, T8>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3, T4, T5, T6, T7, T8> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3, T4, T5, T6, T7, T8>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
    }
    public class RemoteFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9> : RemoteEvent<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3, T4, T5, T6, T7, T8, T9>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
    }
    public class RemoteFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : RemoteEvent<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }
    }
    public class RemoteFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : RemoteEvent<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }
    }
    public class RemoteFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : RemoteEvent<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }
    }
    public class RemoteFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : RemoteEvent<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }
    }
    public class RemoteFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : RemoteEvent<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }
    }
    public class RemoteFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : RemoteEvent<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }
    }
    public class RemoteFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : RemoteEvent<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>>
    {
        public RemoteFunc() { }
        public RemoteFunc(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> act) : base(act) { }
        public RemoteFunc(params Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>[] act) : base(act) { }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
        {
            base.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }
    }

}
