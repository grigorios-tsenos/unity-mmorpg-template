using UnityEngine;
namespace MmoTemplate.Rpg
{
    [CreateAssetMenu(menuName = "RPG/Spell Data")]
    public sealed class SpellData : ScriptableObject
    {
        public string spellName;
        [Min(0)] public float castTime, cooldown;
        [Min(0.1f)] public float range = 3;
        [Min(0)] public int resourceCost, power;
        [Min(0)] public float globalCooldown = 1.5f;
        public ResourceType resourceType;
        public bool healing, autoAttack;
    }
}
