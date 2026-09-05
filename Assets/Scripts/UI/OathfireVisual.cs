using UnityEngine;
namespace MmoTemplate.Rpg
{
    public sealed class OathfireVisual : MonoBehaviour
    {
        private Transform flame;
        private Light glow;
        private QuestManager quest;
        private Vector3 size;
        private void Awake(){flame=transform.Find("Flame");glow=GetComponentInChildren<Light>();if(flame!=null)size=flame.localScale;}
        private void Start(){quest=QuestManager.Instance;if(quest!=null)quest.Changed+=Refresh;Refresh();}
        private void OnDestroy(){if(quest!=null)quest.Changed-=Refresh;}
        private void Refresh()
        {
            bool lit=quest!=null&&quest.Stage.Value>=4;
            if(flame!=null)flame.gameObject.SetActive(lit);
            if(glow!=null)glow.enabled=lit;
        }
        private void Update(){if(flame!=null&&flame.gameObject.activeSelf)flame.localScale=size*(1+Mathf.Sin(Time.time*7)*.08f);}
    }
}
