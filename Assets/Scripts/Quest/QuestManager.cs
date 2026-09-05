using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace MmoTemplate.Rpg
{
    /// <summary>LAN party trial. Progress is shared; each participant can claim once.</summary>
    public class QuestManager : NetworkBehaviour
    {
        public static QuestManager Instance {get;private set;}
        public static event Action Available;
        public event Action Changed;
        public NetworkVariable<int> Stage=new(0), Kills=new(0), Collected=new(0);
        public NetworkList<ulong> Participants, Claimed;
        [SerializeField] private EnemySpawner spawner;
        public QuestData Definition {get;private set;}
        public static string Title => Instance != null && Instance.Definition != null ? Instance.Definition.title : "THE OATHFIRE TRIAL";
        private void Awake()
        {
            Instance=this; Definition=Resources.Load<QuestData>("RPG/OathfireTrial");
            Participants=new NetworkList<ulong>(); Claimed=new NetworkList<ulong>();
        }
        public override void OnNetworkSpawn()
        {
            Stage.OnValueChanged+=OnValue; Kills.OnValueChanged+=OnValue; Collected.OnValueChanged+=OnValue;
            Claimed.OnListChanged+=OnList; Participants.OnListChanged+=OnList;
            if(IsServer) { GameEvents.EnemyKilled+=RegisterKill; NetworkManager.OnClientDisconnectCallback+=Disconnected; }
            Available?.Invoke(); Changed?.Invoke();
        }
        public override void OnNetworkDespawn()
        {
            Stage.OnValueChanged-=OnValue; Kills.OnValueChanged-=OnValue; Collected.OnValueChanged-=OnValue;
            Claimed.OnListChanged-=OnList; Participants.OnListChanged-=OnList;
            GameEvents.EnemyKilled-=RegisterKill;
            if(NetworkManager!=null) NetworkManager.OnClientDisconnectCallback-=Disconnected;
        }
        public override void OnDestroy() { if(Instance==this) Instance=null; base.OnDestroy(); }
        private void OnValue(int oldValue,int newValue)=>Changed?.Invoke();
        private void OnList(NetworkListEvent<ulong> change)=>Changed?.Invoke();
        public bool HasClaimed(PlayerStats stats)=>stats!=null && Claimed.Contains(stats.OwnerClientId);
        public string Objective()
        {
            if(Definition==null) return "Quest data missing: rebuild playable scenes.";
            return Stage.Value switch
            {
                0=>"Speak to Mira by the hearth.",
                1=>$"Guardians defeated: {Kills.Value}/{Definition.guardianKills}\n{Definition.collectItem.displayName}: {Collected.Value}/{Definition.collectCount}",
                2=>"Defeat the Oathbound Warden.",
                3=>"Carry the embers to the Oathfire. [E]",
                4=>HasClaimed(PlayerStats.Local) ? "Reward claimed. Your companions may turn in." : "Return to Mira for your reward. [?]",
                _=>"Trial complete. Speak to Mira to repeat."
            };
        }
        public void AcceptQuest(PlayerStats player)
        {
            if(!IsServer || Definition==null || player==null || (Stage.Value!=0 && Stage.Value!=5)) return;
            Participants.Clear(); Claimed.Clear();
            foreach(var member in PlayerStats.All)
                if(member!=null && member.IsSpawned) { Participants.Add(member.OwnerClientId); member.ClearQuestItem(Definition.collectItem.itemId); }
            Kills.Value=0; Collected.Value=0; Stage.Value=1; spawner.SpawnNext(false);
            AnnounceRpc("The trial begins. Gather the guardians' embers in the eastern circle.");
        }
        private void RegisterKill(bool boss,PlayerStats killer)
        {
            if(!IsServer || killer==null || Definition==null) return;
            // Late arrivals join the active party by contributing to the trial.
            if(!Participants.Contains(killer.OwnerClientId)) Participants.Add(killer.OwnerClientId);
            if(Stage.Value==1 && !boss)
            {
                Kills.Value=Mathf.Min(Definition.guardianKills,Kills.Value+1);
                int total=0;
                foreach(var member in PlayerStats.All) if(member!=null && Participants.Contains(member.OwnerClientId)) total+=member.ItemCount(Definition.collectItem.itemId);
                Collected.Value=Mathf.Min(Definition.collectCount,total);
                if(Kills.Value>=Definition.guardianKills && Collected.Value>=Definition.collectCount) Stage.Value=2;
                spawner.SpawnNext(Stage.Value==2);
            }
            else if(Stage.Value==2 && boss) { Stage.Value=3; AnnounceRpc("The warden falls. Bring the embers to the Oathfire."); }
        }
        public void LightBrazier(PlayerStats player)
        {
            if(!IsServer || Stage.Value!=3 || !Participants.Contains(player.OwnerClientId)) return;
            foreach(var member in PlayerStats.All) if(member!=null) member.ClearQuestItem(Definition.collectItem.itemId);
            Stage.Value=4; AnnounceRpc("The Oathfire burns again. Each companion may claim a reward from Mira.");
        }
        public void CompleteQuest(PlayerStats player)
        {
            if(!IsServer || Stage.Value!=4 || player==null || !Participants.Contains(player.OwnerClientId) || HasClaimed(player)) return;
            Claimed.Add(player.OwnerClientId); // Commit before granting to prevent duplicate claims.
            player.AddGold(Definition.rewardCopper); player.AddXp(Definition.rewardXp); player.GrantPotions(Definition.rewardPotions);
            if(Definition.rewardItem!=null) player.AddItem(Definition.rewardItem,1);
            CheckComplete();
        }
        private void Disconnected(ulong id) { if(!IsServer) return; Participants.Remove(id); CheckComplete(); }
        private void CheckComplete()
        {
            if(Stage.Value!=4) return;
            foreach(var id in Participants) if(!Claimed.Contains(id)) return;
            Stage.Value=5;
        }
        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)] private void AnnounceRpc(string message) { GameEvents.Notify(message); GameEvents.Say(message); }
    }
}
