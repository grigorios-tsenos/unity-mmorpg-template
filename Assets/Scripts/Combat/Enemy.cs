using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MmoTemplate.Rpg
{
    public enum EnemyState { Alive, Passive, Wander, Engaged, Aggro, Chase, Combat, Leash }
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> All = new();
        public ObservableValue<int> Health = new(60);
        public ObservableValue<bool> IsBoss = new(false);
        public ObservableValue<EnemyState> State = new(EnemyState.Wander);
        public EnemyData Definition => IsBoss.Value ? boss : normal;
        public int MaxHealth => Definition != null ? Definition.health : 1;
        public bool IsLeashing => State.Value == EnemyState.Leash;
        private static int nextId;
        public int Id {get;private set;}
        public event System.Action Attacked;
        private EnemyData normal,boss;
        private Vector3 home, wanderPoint;
        private PlayerStats target;
        private float nextAttack,nextWander,aggroEnd;
        private bool dead;
        private CharacterController controller;
        private readonly HierarchicalStateMachine<EnemyState> machine = new();
        private void Awake()
        {
            Id=++nextId;
            normal=Resources.Load<EnemyData>("RPG/Guardian"); boss=Resources.Load<EnemyData>("RPG/Warden");
            controller=GetComponent<CharacterController>();
            machine.Add(EnemyState.Alive); machine.Add(EnemyState.Passive,EnemyState.Alive);
            machine.Add(EnemyState.Wander,EnemyState.Passive); machine.Add(EnemyState.Engaged,EnemyState.Alive);
            machine.Add(EnemyState.Aggro,EnemyState.Engaged); machine.Add(EnemyState.Chase,EnemyState.Engaged);
            machine.Add(EnemyState.Combat,EnemyState.Engaged); machine.Add(EnemyState.Leash,EnemyState.Alive);
            machine.Changed += state => State.Value=state;
        }
        private void Start()
        {
            All.Add(this); home=transform.position; wanderPoint=home;
            { Health.Value=MaxHealth; machine.Change(EnemyState.Wander); StartCoroutine(Think()); }
            if (IsBoss.Value) transform.localScale=Vector3.one*1.3f;
        }
        private void OnDestroy() { All.Remove(this); StopAllCoroutines(); }
        private IEnumerator Think()
        {
            var wait=new WaitForSeconds(.2f);
            while (!dead)
            {
                if (Definition == null) yield break;
                if (!IsLeashing && target != null && (target.IsDead || !target.isActiveAndEnabled || Vector3.Distance(home,transform.position)>Definition.leashRange || Vector3.Distance(home,target.transform.position)>Definition.leashRange))
                { target=null; machine.Change(EnemyState.Leash); }
                if (IsLeashing)
                {
                    if (Vector3.Distance(transform.position,home)<.5f) { Health.Value=MaxHealth; machine.Change(EnemyState.Wander); }
                }
                else if (target == null)
                {
                    float nearest=Definition.aggroRange;
                    foreach(var player in PlayerStats.All)
                    {
                        if (player == null || player.IsDead || !player.isActiveAndEnabled) continue;
                        float d=Vector3.Distance(transform.position,player.transform.position);
                        if (d<nearest && !Physics.Linecast(transform.position+Vector3.up,player.transform.position+Vector3.up,1)) { target=player; nearest=d; }
                    }
                    if (target != null) { machine.Change(EnemyState.Aggro); aggroEnd=Time.time+.5f; }
                    else if(Time.time>=nextWander) { nextWander=Time.time+4; Vector2 point=Random.insideUnitCircle*1.5f; wanderPoint=home+new Vector3(point.x,0,point.y); }
                }
                else if (Time.time>=aggroEnd)
                {
                    float distance=Vector3.Distance(transform.position,target.transform.position);
                    bool inReach=distance<=Definition.attackRange && !Physics.Linecast(transform.position+Vector3.up,target.transform.position+Vector3.up,1);
                    machine.Change(inReach ? EnemyState.Combat : EnemyState.Chase);
                    if (inReach && Time.time>=nextAttack) { nextAttack=Time.time+Definition.attackInterval; target.TakeDamage(Definition.damage); Attacked?.Invoke(); }
                }
                yield return wait;
            }
        }
        private void FixedUpdate()
        {
            if (dead || Definition == null || controller == null) return;
            Vector3 destination=transform.position;
            if (State.Value==EnemyState.Wander) destination=wanderPoint;
            if (State.Value==EnemyState.Leash) destination=home;
            if (State.Value==EnemyState.Chase && target != null) destination=target.transform.position;
            Vector3 delta=destination-transform.position; delta.y=0;
            float speed=State.Value==EnemyState.Wander ? Definition.speed*.2f : Definition.speed;
            Vector3 motion=delta.magnitude>.3f ? delta.normalized*speed : Vector3.zero;
            if(motion.sqrMagnitude>.01f) transform.rotation=Quaternion.LookRotation(motion);
            motion.y=-2; controller.Move(motion*Time.fixedDeltaTime);
        }
        public void TakeDamage(int amount,PlayerStats attacker)
        {
            if(dead || IsLeashing || amount<=0 || attacker==null) return;
            target=attacker; attacker.MarkCombat(); Health.Value=Mathf.Max(0,Health.Value-amount); ShowDamage(transform.position+Vector3.up*2,amount);
            if(Health.Value>0) return;
            dead=true;
            var loot=Definition.loot;
            if(loot!=null)
            {
                attacker.AddXp(loot.experience); attacker.AddGold(loot.copper);
                foreach(var entry in loot.entries) if(Random.value<=entry.chance) attacker.AddItem(entry.item,entry.count);
            }
            All.Remove(this); controller.enabled=false; GameEvents.Kill(IsBoss.Value,attacker); StartCoroutine(RemoveCorpse());
        }
        private IEnumerator RemoveCorpse(){yield return new WaitForSeconds(1.3f);Destroy(gameObject);}
        private void ShowDamage(Vector3 position,int amount)=>GameEvents.Hit(position,amount);
    }
}
