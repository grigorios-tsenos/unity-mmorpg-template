using UnityEngine;
namespace MmoTemplate.Rpg
{
    [CreateAssetMenu(menuName="RPG/Quest Data")]
    public sealed class QuestData : ScriptableObject
    {
        public string title;
        [TextArea(4,10)] public string briefing;
        [Min(1)] public int guardianKills=3, collectCount=3;
        public ItemData collectItem, rewardItem;
        [Min(0)] public int rewardCopper, rewardXp, rewardPotions;
    }
}
