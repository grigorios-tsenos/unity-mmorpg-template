using UnityEngine;
namespace MmoTemplate.Rpg
{
    [CreateAssetMenu(menuName = "RPG/Enemy Data")]
    public sealed class EnemyData : ScriptableObject
    {
        public string displayName;
        public int health = 60, damage = 7;
        public float speed = 2.6f, attackRange = 2.2f, aggroRange = 9, leashRange = 13, attackInterval = 1.6f;
        public LootTable loot;
    }
}
