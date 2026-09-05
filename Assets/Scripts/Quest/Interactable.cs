using UnityEngine;
namespace MmoTemplate.Rpg
{
    public enum InteractableKind { QuestGiver, Merchant, Brazier }
    public class Interactable : InteractionPoint
    {
        [SerializeField] private InteractableKind kind;
        [SerializeField] private string displayName="Mira", prompt="Speak to Mira";
        public InteractableKind Kind=>kind;
        public string DisplayName=>displayName;
        public override string Prompt=>prompt;
        private ItemData potion;
        private void Awake()=>potion=Resources.Load<ItemData>("RPG/HealingPotion");
        public override void Interact(PlayerStats player)
        {
            if(player==null) return;
            var quest=QuestManager.Instance; if(quest==null || quest.Definition==null) return;
            int stage=quest.Stage.Value;
            if(kind==InteractableKind.Merchant)
            {
                player.Dialogue.SendDialog(displayName+" · Supplies",potion.description+"\n\nA potion restores "+potion.healing+" health. Potions share a 60-second cooldown.",new[]{"Buy potion · "+potion.priceCopper+" copper","Farewell"},new[]{4,-1}); return;
            }
            if(kind==InteractableKind.Brazier)
            {
                if(stage==3) quest.LightBrazier(player);
                player.Dialogue.SendDialog("The Oathfire",quest.Stage.Value>=4 ? "The embers kindle a golden flame. Return to Mira." : "The brazier awaits the embers of the trial.",new[]{"Continue"},new[]{-1});return;
            }
            if(stage==0 || stage==5)
                player.Dialogue.SendDialog(displayName+" · Keeper of the Oathfire",quest.Definition.briefing,new[]{stage==5 ? "Repeat the trial" : "Accept quest","Rest at the hearth","Farewell"},new[]{0,3,-1});
            else if(stage==4 && !quest.HasClaimed(player))
                player.Dialogue.SendDialog(displayName+" · Quest complete",$"You have earned the hearth's blessing.\n\nRewards: {quest.Definition.rewardCopper} copper · {quest.Definition.rewardXp} XP\n{quest.Definition.rewardPotions} healing potions · {quest.Definition.rewardItem.displayName}",new[]{"Complete quest","Later"},new[]{1,-1});
            else player.Dialogue.SendDialog(displayName+" · The trial",quest.Objective()+"\n\nTab selects a foe. [1] toggles your weapon. [2–5] use abilities.\n[6] drinks a potion. Space jumps.",new[]{"Rest at the hearth","Continue"},new[]{3,-1});
        }
        public override bool Choose(PlayerStats player,int action)
        {
            if(player==null || player.IsDead) return false;
            var quest=QuestManager.Instance;
            if(kind==InteractableKind.QuestGiver)
            {
                if(action==0) { quest.AcceptQuest(player); return true; }
                if(action==1) { quest.CompleteQuest(player); return true; }
                if(action==3 && Time.time-player.LastCombatTime>5) { player.Heal(player.MaxHealth); return true; }
            }
            if(kind==InteractableKind.Merchant && action==4 && potion!=null && player.Gold.Value>=potion.priceCopper)
            { player.AddGold(-potion.priceCopper); player.GrantPotions(1); return true; }
            return false;
        }
    }
}
