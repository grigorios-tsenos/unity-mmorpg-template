using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MmoTemplate.Rpg
{
    public enum CombatNode { Alive, Mobile, Idle, Moving, Casting, Stunned }
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerCombat : MonoBehaviour, IDialogueSender
    {
        public static PlayerCombat Local { get; private set; }
        public static event Action<PlayerCombat> LocalChanged;
        public event Action Changed;
        public event Action<SpellData> AbilityPerformed;
        public ObservableValue<int> TargetId = new(-1);
        public ObservableValue<int> CastingSlot = new(-1);
        public ObservableValue<double> CastEnd = new(0), GlobalReady = new(0);
        public ObservableValue<bool> AutoAttacking = new(false);
        public SpellData[] Abilities { get; private set; }
        public double Now => Time.timeAsDouble;
        public Enemy Target => Enemy.All.Find(e => e != null && e.isActiveAndEnabled && e.Id == TargetId.Value);
        private PlayerStats stats;
        private readonly HierarchicalStateMachine<CombatNode> machine = new();
        private readonly double[] ready = new double[6];
        private InteractionPoint nearest, dialogSource;
        private readonly HashSet<int> allowedActions = new();
        private double swingReady, stunnedUntil;
        private int pendingSlot = -1;
        private Enemy castTarget;
        private Vector3 castOrigin;
        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            string[] names = { "Strike", "Fireball", "Mend", "HeroicStrike", "SinisterStrike" };
            Abilities = new SpellData[names.Length];
            for (int i=0;i<names.Length;i++) Abilities[i] = Resources.Load<SpellData>("RPG/" + names[i]);
            machine.Add(CombatNode.Alive); machine.Add(CombatNode.Mobile,CombatNode.Alive);
            machine.Add(CombatNode.Idle,CombatNode.Mobile); machine.Add(CombatNode.Moving,CombatNode.Mobile);
            machine.Add(CombatNode.Casting,CombatNode.Alive); machine.Add(CombatNode.Stunned,CombatNode.Alive);
            machine.Change(CombatNode.Idle);
        }
        private void Start()
        {
            TargetId.OnValueChanged += OnTarget; CastingSlot.OnValueChanged += OnCast;
            AutoAttacking.OnValueChanged += OnAuto; stats.Died += OnDeath;
            Local = this; GameEvents.DialogueChoice += Choose; LocalChanged?.Invoke(this);
            StartCoroutine(Poll());
        }
        private void OnDestroy()
        {
            StopAllCoroutines(); TargetId.OnValueChanged -= OnTarget; CastingSlot.OnValueChanged -= OnCast;
            AutoAttacking.OnValueChanged -= OnAuto; stats.Died -= OnDeath;
            GameEvents.DialogueChoice -= Choose; if (Local == this) { Local = null; LocalChanged?.Invoke(null); }
        }
        private void OnTarget(int a, int b) => Changed?.Invoke();
        private void OnCast(int a, int b) => Changed?.Invoke();
        private void OnAuto(bool a, bool b) => Changed?.Invoke();
        private void OnDeath() { CancelCast(); AutoAttacking.Value = false; TargetId.Value = -1; }
        private IEnumerator Poll()
        {
            var wait = new WaitForSeconds(.1f);
            while (true)
            {
                
                {
                    nearest = null; float best = 3.2f;
                    foreach (var entry in InteractionRegistry.All)
                    {
                        if (entry == null || !entry.isActiveAndEnabled) continue;
                        float d = Vector3.Distance(transform.position,entry.transform.position);
                        if (d < best) { best = d; nearest = entry; }
                    }
                    GameEvents.SetPrompt(nearest is IInteraction action ? "[E] " + action.Prompt : "");
                }
                TickCombat();
                yield return wait;
            }
        }
        private void Update()
        {
            if (GameEvents.InputBlocked || stats.IsDead) return;
            if (Input.GetKeyDown(KeyCode.Tab)) CycleTarget();
            if (Input.GetKeyDown(KeyCode.Escape)) { SelectTarget(-1); StopAutoAttack(); Interrupt(); }
            if (Input.GetKeyDown(KeyCode.E) && nearest != null) Interact(nearest);
            if (Input.GetKeyDown(KeyCode.Alpha1)) Use(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Use(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Use(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) Use(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) Use(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) stats.DrinkPotion();
        }
        private void CycleTarget()
        {
            var candidates = Enemy.All.FindAll(e => e != null && e.isActiveAndEnabled && e.Health.Value > 0 && Vector3.Distance(transform.position,e.transform.position) <= 25);
            candidates.Sort((a,b) => a.Id.CompareTo(b.Id));
            if (candidates.Count == 0) { SelectTarget(-1); return; }
            int index = candidates.FindIndex(e => e.Id == TargetId.Value);
            SelectTarget(candidates[(index+1)%candidates.Count].Id);
        }
        public void SelectTarget(int id)
        {
            var enemy = Enemy.All.Find(e => e != null && e.isActiveAndEnabled && e.Id == id);
            if (id == -1 || (enemy != null && enemy.Health.Value > 0 && Vector3.Distance(transform.position,enemy.transform.position)<=25)) TargetId.Value = id;
        }
        public void Use(int slot) => TryCast(slot);
        public double Cooldown(int slot) => Math.Max(0,ready[slot]-Now);
        private void TryCast(int slot)
        {
            if (slot < 0 || slot >= Abilities.Length || stats.IsDead) return;
            var spell = Abilities[slot]; if (spell == null) return;
            if (spell.autoAttack) { AutoAttacking.Value = !AutoAttacking.Value; return; }
            if (pendingSlot >= 0 || Now < stunnedUntil || Now < GlobalReady.Value || Now < ready[slot]) { Feedback("Ability is not ready."); return; }
            var target = Target;
            if (!ValidTarget(spell,target)) { Feedback("Select a living target within range and line of sight."); return; }
            if (spell.castTime > 0 && stats.Moving) { Feedback("Stand still to cast."); return; }
            if (spell.resourceCost > 0 && (stats.ResourceKind.Value != spell.resourceType || stats.Resource.Value < spell.resourceCost)) { Feedback("Not enough " + spell.resourceType + "."); return; }
            GlobalReady.Value = Now + spell.globalCooldown;
            pendingSlot = slot; castTarget = target; castOrigin = transform.position;
            CastingSlot.Value = slot; CastEnd.Value = Now + spell.castTime;
            SetState(CombatNode.Casting,CharacterState.Casting);
            if (spell.castTime <= 0) FinishCast();
        }
        private bool ValidTarget(SpellData spell, Enemy enemy)
        {
            if (spell.healing) return true;
            if (enemy == null || !enemy.isActiveAndEnabled || enemy.Health.Value <= 0 || enemy.IsLeashing || Vector3.Distance(transform.position,enemy.transform.position) > spell.range) return false;
            return !Physics.Linecast(transform.position+Vector3.up,enemy.transform.position+Vector3.up,1,QueryTriggerInteraction.Ignore);
        }
        private void TickCombat()
        {
            if (stats.IsDead) return;
            if (Now < stunnedUntil) { CancelCast(); SetState(CombatNode.Stunned,CharacterState.Stunned); return; }
            if (TargetId.Value != -1 && Target == null) { TargetId.Value = -1; AutoAttacking.Value = false; }
            if (pendingSlot >= 0)
            {
                if (stats.Moving || (transform.position-castOrigin).sqrMagnitude > .04f) { CancelCast(); Feedback("Cast interrupted by movement."); }
                else if (Now >= CastEnd.Value) FinishCast();
            }
            if (pendingSlot < 0) SetState(stats.Moving ? CombatNode.Moving : CombatNode.Idle, stats.Moving ? CharacterState.Moving : CharacterState.Idle);
            var swing = Abilities[0];
            if (AutoAttacking.Value && pendingSlot < 0 && swing != null && Now >= swingReady && ValidTarget(swing,Target))
            {
                swingReady = Now + swing.cooldown; stats.MarkCombat(); Target.TakeDamage(swing.power,stats); AbilityPerformed?.Invoke(swing);
                if (stats.ResourceKind.Value == ResourceType.Rage) stats.GainResource(10);
            }
        }
        private void FinishCast()
        {
            int slot = pendingSlot; var spell = Abilities[slot];
            if (!ValidTarget(spell,castTarget) || !stats.Spend(spell)) { CancelCast(); Feedback("Cast failed: target moved or resource unavailable."); return; }
            ready[slot] = Now + spell.cooldown; SetCooldown(slot,ready[slot]); stats.MarkCombat();
            if (spell.healing) stats.Heal(spell.power); else castTarget.TakeDamage(spell.power,stats);
            AbilityPerformed?.Invoke(spell);
            pendingSlot = -1; CastingSlot.Value = -1;
        }
        private void CancelCast() { pendingSlot = -1; CastingSlot.Value = -1; }
        public void Interrupt() => CancelCast();
        private void StopAutoAttack() => AutoAttacking.Value = false;
        public void Stun(float seconds) { stunnedUntil = Math.Max(stunnedUntil,Now+seconds); }
        private void SetState(CombatNode node, CharacterState state) { machine.Change(node); stats.State.Value = state; }
        private void SetCooldown(int slot,double until) { ready[slot]=until; Changed?.Invoke(); }
        private void Feedback(string text) => GameEvents.Notify(text);
        public void Interact(InteractionPoint entry)
        {
            if(stats.IsDead||entry==null||!entry.isActiveAndEnabled||Vector3.Distance(transform.position,entry.transform.position)>3.2f)return;
            CancelCast(); AutoAttacking.Value=false;dialogSource=entry;allowedActions.Clear();entry.Interact(stats);
        }
        public void SendDialog(string title,string body,string[] labels,int[] actions)
        {
            allowedActions.Clear();foreach(int action in actions)allowedActions.Add(action);
            GameEvents.ShowDialog(title,body,labels,actions);
        }
        private void Choose(int id) => ChooseAction(id);
        public void ChooseAction(int id)
        {
            if (stats.IsDead || dialogSource == null || !dialogSource.isActiveAndEnabled || !allowedActions.Remove(id)) return;
            if (Vector3.Distance(transform.position,dialogSource.transform.position)>3.2f) { allowedActions.Clear(); Feedback("You are too far away."); return; }
            allowedActions.Clear(); (dialogSource as IInteraction)?.Choose(stats,id);
        }
    }
}
