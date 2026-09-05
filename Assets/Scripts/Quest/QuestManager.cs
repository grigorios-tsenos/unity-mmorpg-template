using System;
using UnityEngine;
namespace MmoTemplate.Rpg
{
    /// <summary>One local quest, with explicit stages and a single committed reward.</summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance {get;private set;}
        public static event Action Available;
        public event Action Changed;
        public ObservableValue<int> Stage=new(0), Kills=new(0), Collected=new(0);
        [SerializeField] private EnemySpawner spawner;
        public QuestData Definition {get;private set;}
        public static string Title=>Instance!=null&&Instance.Definition!=null?Instance.Definition.title:"THE OATHFIRE TRIAL";
        private void Awake(){Instance=this;Definition=Resources.Load<QuestData>("RPG/OathfireTrial");}
        private void Start()
        {
            Stage.OnValueChanged+=OnValue;Kills.OnValueChanged+=OnValue;Collected.OnValueChanged+=OnValue;
            GameEvents.EnemyKilled+=RegisterKill;Available?.Invoke();Changed?.Invoke();
        }
        private void OnDestroy()
        {
            Stage.OnValueChanged-=OnValue;Kills.OnValueChanged-=OnValue;Collected.OnValueChanged-=OnValue;
            GameEvents.EnemyKilled-=RegisterKill;if(Instance==this)Instance=null;
        }
        private void OnValue(int before,int after)=>Changed?.Invoke();
        public bool HasClaimed(PlayerStats player)=>Stage.Value==5;
        public string Objective()
        {
            if(Definition==null)return "Quest content missing.";
            return Stage.Value switch
            {
                0=>"Speak to Mira beside the hearth. [E]",
                1=>$"Guardians defeated: {Kills.Value}/{Definition.guardianKills}\n{Definition.collectItem.displayName}: {Collected.Value}/{Definition.collectCount}",
                2=>"Defeat the Oathbound Warden.",
                3=>"Carry the embers to the Oathfire. [E]",
                4=>"Return to Mira for your reward. [?]",
                _=>"Trial complete. Rest, explore, or repeat."
            };
        }
        public void AcceptQuest(PlayerStats player)
        {
            if(Definition==null||player==null||(Stage.Value!=0&&Stage.Value!=5))return;
            player.ClearQuestItem(Definition.collectItem.itemId);
            Kills.Value=0;Collected.Value=0;Stage.Value=1;spawner.SpawnNext(false);
            Announce("The trial begins. Enter the eastern proving circle.");
        }
        private void RegisterKill(bool boss,PlayerStats player)
        {
            if(player==null||Definition==null)return;
            if(Stage.Value==1&&!boss)
            {
                Kills.Value=Mathf.Min(Definition.guardianKills,Kills.Value+1);
                Collected.Value=Mathf.Min(Definition.collectCount,player.ItemCount(Definition.collectItem.itemId));
                if(Kills.Value>=Definition.guardianKills&&Collected.Value>=Definition.collectCount)Stage.Value=2;
                spawner.SpawnNext(Stage.Value==2);
            }
            else if(Stage.Value==2&&boss){Stage.Value=3;Announce("The warden falls. Bring the embers to the Oathfire.");}
        }
        public void LightBrazier(PlayerStats player)
        {
            if(Stage.Value!=3||player==null)return;
            player.ClearQuestItem(Definition.collectItem.itemId);Stage.Value=4;
            Announce("The Oathfire burns again. Return to Mira.");
        }
        public void CompleteQuest(PlayerStats player)
        {
            if(Stage.Value!=4||player==null)return;
            Stage.Value=5;
            player.AddGold(Definition.rewardCopper);player.AddXp(Definition.rewardXp);player.GrantPotions(Definition.rewardPotions);
            if(Definition.rewardItem!=null)player.AddItem(Definition.rewardItem,1);
            Announce("The Oathfire Trial — complete.");
        }
        private void Announce(string message){GameEvents.Notify(message);GameEvents.Say(message);}
    }
}
