using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.Console.ExtendedConsole.In
{
    public class KeyWatcher : IKeyResolver
    {
        public List<Keys> WatchingKeys { get; private set; }

        public virtual bool Resolve(Keys key, bool up)
        {
            if (WatchingKeys.Contains(key))
            {
                if (up) { if (KeyUp != null) return KeyUp(key); }
                else { if (KeyDown != null) return KeyDown(key); }
            }
            return false;
        }

        public event KeyResolveHandle KeyDown;
        public event KeyResolveHandle KeyUp;

        public KeyManager Owner { get; private set; }

        public void Unbind()
        {
            Owner.UnBind(this);
        }

        public KeyWatcher(KeyManager manager, params Keys[] watching)
        {
            WatchingKeys = new List<Keys>(watching);
            Owner = manager;
            manager.Bind(this);
        }
    }

    public interface IKeyResolver
    {
        bool Resolve(Keys key, bool up);
    }

    public delegate bool KeyResolveHandle(Keys key);

    public sealed class KeyManager
    {
        List<IKeyResolver> list = new List<IKeyResolver>();

        public void Bind(IKeyResolver kr)
        {
            if (kr == null) throw new ArgumentNullException("kr");
            list.Add(kr);
        }
        public void UnBind(IKeyResolver kr)
        {
            if (kr == null) throw new ArgumentNullException("kr");
            list.Remove(kr);
        }

        internal void Push(Keys key, bool up)
        {
            foreach (var kr in list) if (kr.Resolve(key, up)) return;
        }
    }
}
