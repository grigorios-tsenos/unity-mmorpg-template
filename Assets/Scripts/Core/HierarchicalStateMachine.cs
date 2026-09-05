using System;
using System.Collections.Generic;
namespace MmoTemplate.Rpg
{
    /// <summary>Transitions exit to the common ancestor, then enter down to the leaf.</summary>
    public sealed class HierarchicalStateMachine<T> where T : struct, Enum
    {
        private sealed class Node { public T? Parent; public Action Enter, Exit; }
        private readonly Dictionary<T, Node> nodes = new();
        private readonly List<T> exits = new(), entries = new();
        public T Current { get; private set; }
        public bool Started { get; private set; }
        public event Action<T> Changed;
        public void Add(T id, T? parent = null, Action enter = null, Action exit = null)
            => nodes.Add(id, new Node { Parent = parent, Enter = enter, Exit = exit });
        public bool IsIn(T id)
        {
            if (!Started) return false;
            T? cursor = Current;
            while (cursor.HasValue) { if (cursor.Value.Equals(id)) return true; cursor = nodes[cursor.Value].Parent; }
            return false;
        }
        public void Change(T next)
        {
            if (Started && Current.Equals(next)) return;
            if (!nodes.ContainsKey(next)) throw new ArgumentException("Unknown state");
            exits.Clear(); entries.Clear();
            T? cursor = Started ? Current : (T?)null;
            while (cursor.HasValue) { exits.Add(cursor.Value); cursor = nodes[cursor.Value].Parent; }
            cursor = next;
            while (cursor.HasValue) { entries.Add(cursor.Value); cursor = nodes[cursor.Value].Parent; }
            while (exits.Count > 0 && entries.Count > 0 && exits[exits.Count - 1].Equals(entries[entries.Count - 1]))
            { exits.RemoveAt(exits.Count - 1); entries.RemoveAt(entries.Count - 1); }
            foreach (var id in exits) nodes[id].Exit?.Invoke();
            for (int i = entries.Count - 1; i >= 0; i--) nodes[entries[i]].Enter?.Invoke();
            Current = next; Started = true; Changed?.Invoke(next);
        }
    }
}
