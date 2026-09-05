using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace MmoTemplate.Rpg
{
    public class PlayerStats : NetworkBehaviour
    {
        public static readonly List<PlayerStats> All = new();
        public static PlayerStats Local { get; private set; }
        public static event Action<PlayerStats> LocalChanged;
        public event Action Changed, Died;
        public event Action<ItemData, int> ItemReceived;
        public NetworkVariable<int> Health = new(100), Level = new(1), Xp = new(0), Gold = new(0), Potions = new(3);
        public NetworkVariable<int> Resource = new(100);
        public NetworkVariable<ResourceType> ResourceKind = new(ResourceType.Mana);
        public NetworkVariable<CharacterState> State = new(CharacterState.Idle);
        public IDialogueSender Dialogue { get; private set; }
        public int MaxHealth => 100 + (Level.Value - 1) * 5;
        public float XpForNextLevel => Level.Value * 60f;
        public bool IsDead => Health.Value <= 0;
        public bool Moving { get; set; }
        public float LastCombatTime { get; private set; } = -100;
        private CharacterController controller;
        private ItemData potion;
        public NetworkVariable<double> PotionReady = new(0);
        private readonly Dictionary<string,int> inventory = new();
        private void Awake() { controller = GetComponent<CharacterController>(); Dialogue = GetComponent<IDialogueSender>(); potion = Resources.Load<ItemData>("RPG/HealingPotion"); }
        public override void OnNetworkSpawn()
        {
            All.Add(this);
            Health.OnValueChanged += OnValue; Level.OnValueChanged += OnValue; Xp.OnValueChanged += OnValue;
            Gold.OnValueChanged += OnValue; Potions.OnValueChanged += OnValue; Resource.OnValueChanged += OnValue;
            ResourceKind.OnValueChanged += OnKind;
            if (IsServer) { Health.Value = MaxHealth; StartCoroutine(Ticks()); }
            if (IsOwner) { Local = this; LocalChanged?.Invoke(this); }
        }
        public override void OnNetworkDespawn()
        {
            StopAllCoroutines(); All.Remove(this);
            Health.OnValueChanged -= OnValue; Level.OnValueChanged -= OnValue; Xp.OnValueChanged -= OnValue;
            Gold.OnValueChanged -= OnValue; Potions.OnValueChanged -= OnValue; Resource.OnValueChanged -= OnValue;
            ResourceKind.OnValueChanged -= OnKind;
            if (Local == this) { Local = null; LocalChanged?.Invoke(null); }
        }
        private void OnValue(int before, int after) => Changed?.Invoke();
        private void OnKind(ResourceType before, ResourceType after) => Changed?.Invoke();
        private IEnumerator Ticks()
        {
            var wait = new WaitForSeconds(2);
            while (true)
            {
                yield return wait;
                if (IsDead) continue;
                bool resting = Time.time - LastCombatTime > 5;
                if (resting) Heal(4);
                if (ResourceKind.Value == ResourceType.Energy) GainResource(20);
                if (ResourceKind.Value == ResourceType.Mana && resting && State.Value != CharacterState.Casting) GainResource(12);
                if (ResourceKind.Value == ResourceType.Rage && resting) Resource.Value = Mathf.Max(0, Resource.Value - 5);
            }
        }
        public void MarkCombat() { if (IsServer) LastCombatTime = Time.time; }
        public void GainResource(int amount) { if (IsServer) Resource.Value = Mathf.Clamp(Resource.Value + amount, 0, 100); }
        public bool Spend(SpellData spell)
        {
            if (!IsServer || (spell.resourceCost > 0 && (ResourceKind.Value != spell.resourceType || Resource.Value < spell.resourceCost))) return false;
            Resource.Value -= spell.resourceCost; return true;
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] public void ChangeResourceRpc(ResourceType kind)
        {
            if (!Enum.IsDefined(typeof(ResourceType), kind) || Time.time - LastCombatTime < 5 || State.Value == CharacterState.Casting || IsDead) return;
            ResourceKind.Value = kind; Resource.Value = kind == ResourceType.Rage ? 0 : 100;
        }
        public void TakeDamage(int amount)
        {
            if (!IsServer || IsDead || amount <= 0) return;
            MarkCombat(); Health.Value = Mathf.Max(0, Health.Value - amount);
            if (ResourceKind.Value == ResourceType.Rage) GainResource(8);
            DamageRpc(transform.position + Vector3.up * 2, amount);
            if (IsDead) { Died?.Invoke(); StartCoroutine(Recover()); }
        }
        private IEnumerator Recover()
        {
            NotifyRpc("You fall. Returning to the hearth in five seconds…");
            yield return new WaitForSeconds(5);
            controller.enabled = false; transform.position = new Vector3(-2, .2f, 3.5f); controller.enabled = true;
            Health.Value = MaxHealth; State.Value = CharacterState.Idle;
        }
        public void Heal(int amount) { if (IsServer && !IsDead) Health.Value = Mathf.Min(MaxHealth, Health.Value + Mathf.Max(0, amount)); }
        public void AddXp(int amount)
        {
            if (!IsServer || amount < 0) return;
            Xp.Value += amount;
            while (Xp.Value >= XpForNextLevel) { Xp.Value -= (int)XpForNextLevel; Level.Value++; Heal(MaxHealth); NotifyRpc($"You reach level {Level.Value}!"); }
        }
        public void AddGold(int amount) { if (IsServer) Gold.Value = Mathf.Max(0, Gold.Value + amount); }
        public void GrantPotions(int count) { if (IsServer) Potions.Value += Mathf.Max(0,count); }
        public int ItemCount(string id) => inventory.TryGetValue(id, out int value) ? value : 0;
        public void AddItem(ItemData item, int count)
        {
            if (!IsServer || item == null || count <= 0) return;
            inventory[item.itemId] = ItemCount(item.itemId) + count;
            ItemReceived?.Invoke(item, count); NotifyRpc($"Received: {item.displayName} ×{count}");
        }
        public void ClearQuestItem(string id) { if (IsServer) inventory.Remove(id); }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] public void DrinkPotionRpc()
        {
            if (IsDead || potion == null || NetworkManager.ServerTime.Time < PotionReady.Value || Potions.Value <= 0 || Health.Value >= MaxHealth) return;
            PotionReady.Value = NetworkManager.ServerTime.Time + 60; Potions.Value--; Heal(potion.healing);
        }
        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)] private void NotifyRpc(string message) { if (IsOwner) GameEvents.Notify(message); }
        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)] private void DamageRpc(Vector3 position, int amount) => GameEvents.Hit(position, -amount);
    }
}
