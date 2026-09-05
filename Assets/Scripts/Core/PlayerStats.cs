using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MmoTemplate.Rpg
{
    [DisallowMultipleComponent]
    public class PlayerStats : MonoBehaviour
    {
        public static readonly List<PlayerStats> All = new();
        public static PlayerStats Local { get; private set; }
        public static event Action<PlayerStats> LocalChanged;
        public event Action Changed, Died, Recovered;
        public event Action<ItemData, int> ItemReceived;
        public ObservableValue<int> Health = new(100), Level = new(1), Xp = new(0), Gold = new(0), Potions = new(3);
        public ObservableValue<int> Resource = new(100);
        public ObservableValue<ResourceType> ResourceKind = new(ResourceType.Mana);
        public ObservableValue<CharacterState> State = new(CharacterState.Idle);
        public IDialogueSender Dialogue { get; private set; }
        public int MaxHealth => 100 + (Level.Value - 1) * 5;
        public float XpForNextLevel => Level.Value * 60f;
        public bool IsDead => Health.Value <= 0;
        public bool Moving { get; set; }
        public float LastCombatTime { get; private set; } = -100;
        private CharacterController controller;
        private ItemData potion;
        public ObservableValue<double> PotionReady = new(0);
        private readonly Dictionary<string,int> inventory = new();
        private void Awake() { controller = GetComponent<CharacterController>(); Dialogue = GetComponent<IDialogueSender>(); potion = Resources.Load<ItemData>("RPG/HealingPotion"); }
        private void Start()
        {
            All.Add(this);
            Health.OnValueChanged += OnValue; Level.OnValueChanged += OnValue; Xp.OnValueChanged += OnValue;
            Gold.OnValueChanged += OnValue; Potions.OnValueChanged += OnValue; Resource.OnValueChanged += OnValue;
            ResourceKind.OnValueChanged += OnKind;
            { Health.Value = MaxHealth; StartCoroutine(Ticks()); }
            { Local = this; LocalChanged?.Invoke(this); }
        }
        private void OnDestroy()
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
        public void MarkCombat() { LastCombatTime = Time.time; }
        public void GainResource(int amount) { Resource.Value = Mathf.Clamp(Resource.Value + amount, 0, 100); }
        public bool Spend(SpellData spell)
        {
            if ((spell.resourceCost > 0 && (ResourceKind.Value != spell.resourceType || Resource.Value < spell.resourceCost))) return false;
            Resource.Value -= spell.resourceCost; return true;
        }
        public void ChangeResource(ResourceType kind)
        {
            if (!Enum.IsDefined(typeof(ResourceType), kind) || Time.time - LastCombatTime < 5 || State.Value == CharacterState.Casting || IsDead) return;
            ResourceKind.Value = kind; Resource.Value = kind == ResourceType.Rage ? 0 : 100;
        }
        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            MarkCombat(); Health.Value = Mathf.Max(0, Health.Value - amount);
            if (ResourceKind.Value == ResourceType.Rage) GainResource(8);
            ShowDamage(transform.position + Vector3.up * 2, amount);
            if (IsDead) { Died?.Invoke(); StartCoroutine(Recover()); }
        }
        private IEnumerator Recover()
        {
            Notify("You fall. Returning to the hearth in five seconds…");
            yield return new WaitForSeconds(5);
            controller.enabled = false; transform.SetPositionAndRotation(new Vector3(-2, .2f, 4), Quaternion.Euler(0,180,0)); controller.enabled = true;
            Recovered?.Invoke();
            Health.Value = MaxHealth; State.Value = CharacterState.Idle;
        }
        public void Heal(int amount) { if (!IsDead) Health.Value = Mathf.Min(MaxHealth, Health.Value + Mathf.Max(0, amount)); }
        public void AddXp(int amount)
        {
            if (amount < 0) return;
            Xp.Value += amount;
            while (Xp.Value >= XpForNextLevel) { Xp.Value -= (int)XpForNextLevel; Level.Value++; Heal(MaxHealth); Notify($"You reach level {Level.Value}!"); }
        }
        public void AddGold(int amount) { Gold.Value = Mathf.Max(0, Gold.Value + amount); }
        public void GrantPotions(int count) { Potions.Value += Mathf.Max(0,count); }
        public int ItemCount(string id) => inventory.TryGetValue(id, out int value) ? value : 0;
        public void AddItem(ItemData item, int count)
        {
            if (item == null || count <= 0) return;
            inventory[item.itemId] = ItemCount(item.itemId) + count;
            ItemReceived?.Invoke(item, count); GameEvents.Say($"Loot: {item.displayName} ×{count}"); Notify($"Received: {item.displayName} ×{count}");
        }
        public void ClearQuestItem(string id) { inventory.Remove(id); }
        public void DrinkPotion()
        {
            if (IsDead || potion == null || Time.timeAsDouble < PotionReady.Value || Potions.Value <= 0 || Health.Value >= MaxHealth) return;
            PotionReady.Value = Time.timeAsDouble + 60; Potions.Value--; Heal(potion.healing);
        }
        private void Notify(string message) { GameEvents.Notify(message); }
        private void ShowDamage(Vector3 position, int amount) => GameEvents.Hit(position, -amount);
    }
}
