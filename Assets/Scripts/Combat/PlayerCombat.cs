using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace MmoTemplate.Rpg
{
    public enum CombatNode { Alive, Mobile, Idle, Moving, Casting, Stunned }
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerCombat : NetworkBehaviour, IDialogueSender
    {
        public static PlayerCombat Local { get; private set; }
        public static event Action<PlayerCombat> LocalChanged;
        public event Action Changed;
        public NetworkVariable<ulong> TargetId = new(ulong.MaxValue);
        public NetworkVariable<int> CastingSlot = new(-1);
        public NetworkVariable<double> CastEnd = new(0), GlobalReady = new(0);
        public NetworkVariable<bool> AutoAttacking = new(false);
        public SpellData[] Abilities { get; private set; }
        public double Now => NetworkManager != null ? NetworkManager.ServerTime.Time : 0;
        public Enemy Target => Enemy.All.Find(e => e != null && e.IsSpawned && e.NetworkObjectId == TargetId.Value);
        private PlayerStats stats;
        private readonly HierarchicalStateMachine<CombatNode> machine = new();
        private readonly double[] ready = new double[6];
        private NetworkBehaviour nearest, dialogSource;
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
        public override void OnNetworkSpawn()
        {
            TargetId.OnValueChanged += OnTarget; CastingSlot.OnValueChanged += OnCast;
            AutoAttacking.OnValueChanged += OnAuto; stats.Died += OnDeath;
            if (IsOwner) { Local = this; GameEvents.DialogueChoice += Choose; LocalChanged?.Invoke(this); }
            StartCoroutine(Poll());
        }
        public override void OnNetworkDespawn()
        {
            StopAllCoroutines(); TargetId.OnValueChanged -= OnTarget; CastingSlot.OnValueChanged -= OnCast;
            AutoAttacking.OnValueChanged -= OnAuto; stats.Died -= OnDeath;
            if (IsOwner) { GameEvents.DialogueChoice -= Choose; Local = null; LocalChanged?.Invoke(null); }
        }
        private void OnTarget(ulong a, ulong b) => Changed?.Invoke();
        private void OnCast(int a, int b) => Changed?.Invoke();
        private void OnAuto(bool a, bool b) => Changed?.Invoke();
        private void OnDeath() { CancelCast(); AutoAttacking.Value = false; TargetId.Value = ulong.MaxValue; }
        private IEnumerator Poll()
        {
            var wait = new WaitForSeconds(.1f);
            while (true)
            {
                if (IsOwner)
                {
                    nearest = null; float best = 3.2f;
                    foreach (var entry in InteractionRegistry.All)
                    {
                        if (entry == null || !entry.IsSpawned) continue;
                        float d = Vector3.Distance(transform.position,entry.transform.position);
                        if (d < best) { best = d; nearest = entry; }
                    }
                    GameEvents.SetPrompt(nearest is IInteraction action ? "[E] " + action.Prompt : "");
                }
                if (IsServer) ServerTick();
                yield return wait;
            }
        }
        private void Update()
        {
            if (!IsOwner || GameEvents.InputBlocked || stats.IsDead) return;
            if (Input.GetKeyDown(KeyCode.Tab)) CycleTarget();
            if (Input.GetKeyDown(KeyCode.Escape)) { SetTargetRpc(ulong.MaxValue); StopAutoRpc(); InterruptRpc(); }
            if (Input.GetKeyDown(KeyCode.E) && nearest != null) InteractRpc(nearest.NetworkObjectId);
            if (Input.GetKeyDown(KeyCode.Alpha1)) Use(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Use(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Use(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) Use(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) Use(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) stats.DrinkPotionRpc();
        }
        private void CycleTarget()
        {
            var candidates = Enemy.All.FindAll(e => e != null && e.IsSpawned && e.Health.Value > 0 && Vector3.Distance(transform.position,e.transform.position) <= 25);
            candidates.Sort((a,b) => a.NetworkObjectId.CompareTo(b.NetworkObjectId));
            if (candidates.Count == 0) { SetTargetRpc(ulong.MaxValue); return; }
            int index = candidates.FindIndex(e => e.NetworkObjectId == TargetId.Value);
            SetTargetRpc(candidates[(index+1)%candidates.Count].NetworkObjectId);
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] private void SetTargetRpc(ulong id)
        {
            var enemy = Enemy.All.Find(e => e != null && e.IsSpawned && e.NetworkObjectId == id);
            if (id == ulong.MaxValue || (enemy != null && enemy.Health.Value > 0 && Vector3.Distance(transform.position,enemy.transform.position)<=25)) TargetId.Value = id;
        }
        public void Use(int slot) { if (IsOwner) CastRpc(slot); }
        public double Cooldown(int slot) => Math.Max(0,ready[slot]-Now);
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] private void CastRpc(int slot)
        {
            if (slot < 0 || slot >= Abilities.Length || stats.IsDead) return;
            var spell = Abilities[slot]; if (spell == null) return;
            if (spell.autoAttack) { AutoAttacking.Value = !AutoAttacking.Value; return; }
            if (pendingSlot >= 0 || Now < stunnedUntil || Now < GlobalReady.Value || Now < ready[slot]) { FeedbackRpc("Ability is not ready."); return; }
            var target = Target;
            if (!ValidTarget(spell,target)) { FeedbackRpc("Select a living target within range and line of sight."); return; }
            if (spell.castTime > 0 && stats.Moving) { FeedbackRpc("Stand still to cast."); return; }
            if (spell.resourceCost > 0 && (stats.ResourceKind.Value != spell.resourceType || stats.Resource.Value < spell.resourceCost)) { FeedbackRpc("Not enough " + spell.resourceType + "."); return; }
            GlobalReady.Value = Now + spell.globalCooldown;
            pendingSlot = slot; castTarget = target; castOrigin = transform.position;
            CastingSlot.Value = slot; CastEnd.Value = Now + spell.castTime;
            SetState(CombatNode.Casting,CharacterState.Casting);
            if (spell.castTime <= 0) FinishCast();
        }
        private bool ValidTarget(SpellData spell, Enemy enemy)
        {
            if (spell.healing) return true;
            if (enemy == null || !enemy.IsSpawned || enemy.Health.Value <= 0 || enemy.IsLeashing || Vector3.Distance(transform.position,enemy.transform.position) > spell.range) return false;
            return !Physics.Linecast(transform.position+Vector3.up,enemy.transform.position+Vector3.up,1,QueryTriggerInteraction.Ignore);
        }
        private void ServerTick()
        {
            if (stats.IsDead) return;
            if (Now < stunnedUntil) { CancelCast(); SetState(CombatNode.Stunned,CharacterState.Stunned); return; }
            if (TargetId.Value != ulong.MaxValue && Target == null) { TargetId.Value = ulong.MaxValue; AutoAttacking.Value = false; }
            if (pendingSlot >= 0)
            {
                if (stats.Moving || (transform.position-castOrigin).sqrMagnitude > .04f) { CancelCast(); FeedbackRpc("Cast interrupted by movement."); }
                else if (Now >= CastEnd.Value) FinishCast();
            }
            if (pendingSlot < 0) SetState(stats.Moving ? CombatNode.Moving : CombatNode.Idle, stats.Moving ? CharacterState.Moving : CharacterState.Idle);
            var swing = Abilities[0];
            if (AutoAttacking.Value && pendingSlot < 0 && swing != null && Now >= swingReady && ValidTarget(swing,Target))
            {
                swingReady = Now + swing.cooldown; stats.MarkCombat(); Target.TakeDamage(swing.power,stats);
                if (stats.ResourceKind.Value == ResourceType.Rage) stats.GainResource(10);
            }
        }
        private void FinishCast()
        {
            int slot = pendingSlot; var spell = Abilities[slot];
            if (!ValidTarget(spell,castTarget) || !stats.Spend(spell)) { CancelCast(); FeedbackRpc("Cast failed: target moved or resource unavailable."); return; }
            ready[slot] = Now + spell.cooldown; CooldownRpc(slot,ready[slot]); stats.MarkCombat();
            if (spell.healing) stats.Heal(spell.power); else castTarget.TakeDamage(spell.power,stats);
            pendingSlot = -1; CastingSlot.Value = -1;
        }
        private void CancelCast() { pendingSlot = -1; CastingSlot.Value = -1; }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] public void InterruptRpc() => CancelCast();
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] private void StopAutoRpc() => AutoAttacking.Value = false;
        public void Stun(float seconds) { if (IsServer) stunnedUntil = Math.Max(stunnedUntil,Now+seconds); }
        private void SetState(CombatNode node, CharacterState state) { machine.Change(node); stats.State.Value = state; }
        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)] private void CooldownRpc(int slot,double until) { if (IsOwner) { ready[slot]=until; Changed?.Invoke(); } }
        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)] private void FeedbackRpc(string text) { if (IsOwner) GameEvents.Notify(text); }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] private void InteractRpc(ulong id)
        {
            if (stats.IsDead || !NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id,out var obj)) return;
            var entry = InteractionRegistry.All.Find(n => n != null && n.NetworkObjectId == id);
            if (entry is not IInteraction action || Vector3.Distance(transform.position,obj.transform.position)>3.2f) return;
            CancelCast(); AutoAttacking.Value = false; dialogSource=entry; allowedActions.Clear(); action.Interact(stats);
        }
        public void SendDialog(string title,string body,string[] labels,int[] actions)
        {
            if (!IsServer) return;
            allowedActions.Clear(); foreach(int action in actions) allowedActions.Add(action);
            ShowDialogRpc(title,body,string.Join('\u001F',labels),string.Join(",",actions));
        }
        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)] private void ShowDialogRpc(string title,string body,string labels,string actions)
        {
            if (!IsOwner) return;
            var values = actions.Split(','); var ids = new int[values.Length];
            for(int i=0;i<ids.Length;i++) int.TryParse(values[i],out ids[i]);
            GameEvents.ShowDialog(title,body,labels.Split('\u001F'),ids);
        }
        private void Choose(int id) { if (IsOwner) ChooseRpc(id); }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] private void ChooseRpc(int id)
        {
            if (stats.IsDead || dialogSource == null || !dialogSource.IsSpawned || !allowedActions.Remove(id)) return;
            if (Vector3.Distance(transform.position,dialogSource.transform.position)>3.2f) { allowedActions.Clear(); FeedbackRpc("You are too far away."); return; }
            allowedActions.Clear(); (dialogSource as IInteraction)?.Choose(stats,id);
        }
    }
}
