using UnityEngine;
namespace MmoTemplate.Rpg
{
    [CreateAssetMenu(menuName = "RPG/Item Data")]
    public sealed class ItemData : ScriptableObject
    {
        public string itemId, displayName;
        [TextArea] public string description;
        [Min(0)] public int priceCopper, healing;
        public bool questItem;
    }
}
