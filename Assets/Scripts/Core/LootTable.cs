using System;
using UnityEngine;
namespace MmoTemplate.Rpg
{
    [CreateAssetMenu(menuName = "RPG/Loot Table")]
    public sealed class LootTable : ScriptableObject
    {
        [Serializable] public struct Entry { public ItemData item; [Range(0,1)] public float chance; [Min(1)] public int count; }
        public Entry[] entries = Array.Empty<Entry>();
        [Min(0)] public int copper, experience;
    }
}
