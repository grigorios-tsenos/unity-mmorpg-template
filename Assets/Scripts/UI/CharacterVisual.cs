using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MmoTemplate.Rpg
{
    /// <summary>Presentation reads character state and subscribes to actions; gameplay never calls animation code.</summary>
    public sealed class CharacterVisual : MonoBehaviour
    {
        private Animation animationRig;
        private PlayerStats player;
        private PlayerCombat combat;
        private Enemy enemy;
        private readonly Dictionary<string,string> clips=new();
        private string current;
        private float actionUntil;
        private void Awake()
        {
            animationRig=GetComponentInChildren<Animation>();player=GetComponent<PlayerStats>();combat=GetComponent<PlayerCombat>();enemy=GetComponent<Enemy>();
            if(animationRig==null)return;
            foreach(AnimationState state in animationRig)
            {
                clips[state.name]=state.name;
                state.wrapMode=state.name.Contains("Loop")||state.name.Contains("Idle")?WrapMode.Loop:WrapMode.ClampForever;
            }
        }
        private void OnEnable(){if(combat!=null)combat.AbilityPerformed+=Perform;if(enemy!=null)enemy.Attacked+=Attack;StartCoroutine(Animate());}
        private void OnDisable(){if(combat!=null)combat.AbilityPerformed-=Perform;if(enemy!=null)enemy.Attacked-=Attack;StopAllCoroutines();}
        private IEnumerator Animate()
        {
            var wait=new WaitForSeconds(.06f);
            while(true)
            {
                bool dead=player!=null?player.IsDead:enemy!=null&&enemy.Health.Value<=0;
                if(dead)Play("Death01");
                else if(Time.time>=actionUntil)
                {
                    bool moving=player!=null?player.Moving:enemy!=null&&(enemy.State.Value==EnemyState.Chase||enemy.State.Value==EnemyState.Leash);
                    bool casting=player!=null&&player.State.Value==CharacterState.Casting;
                    Play(casting?"Spell_Simple_Idle_Loop":moving?"Jog_Fwd_Loop":enemy!=null&&enemy.State.Value==EnemyState.Combat?"Sword_Idle":"Idle_Loop");
                }
                yield return wait;
            }
        }
        private void Perform(SpellData spell){Play(spell.castTime>0?"Spell_Simple_Shoot":"Sword_Attack",true);actionUntil=Time.time+.7f;}
        private void Attack(){Play("Sword_Attack",true);actionUntil=Time.time+.7f;}
        private void Play(string name,bool restart=false)
        {
            if(animationRig==null||(!restart&&current==name))return;
            string found=null;
            foreach(var pair in clips)if(pair.Key==name||pair.Key.EndsWith(name)){found=pair.Value;break;}
            if(found==null)return;
            current=name;if(restart)animationRig[found].time=0;
            animationRig.CrossFade(found,.12f);
        }
    }
}
