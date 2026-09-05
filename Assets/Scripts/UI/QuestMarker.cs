using System.Collections;
using UnityEngine;
namespace MmoTemplate.Rpg
{
    public sealed class QuestMarker : MonoBehaviour
    {
        private Interactable npc;
        private TextMesh label;
        private Camera cameraRig;
        private void Awake()
        {
            npc=GetComponent<Interactable>();
            var go=new GameObject("Quest indicator");go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up*2.8f;
            label=go.AddComponent<TextMesh>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=64;label.characterSize=.075f;
            label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(1,.83f,.1f);
            label.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
        }
        private IEnumerator Start()
        {
            // Builder adds Interactable after this marker; serialized scenes have it before Awake.
            if(npc==null)npc=GetComponent<Interactable>();
            cameraRig=Camera.main;var wait=new WaitForSeconds(.2f);
            while(true)
            {
                var quest=QuestManager.Instance;string symbol="";
                if(npc!=null&&quest!=null)
                {
                    int stage=quest.Stage.Value;
                    if(npc.Kind==InteractableKind.QuestGiver)symbol=stage==0||stage==5?"!":stage==4&&!quest.HasClaimed(PlayerStats.Local)?"?":"";
                    if(npc.Kind==InteractableKind.Brazier&&stage==3)symbol="?";
                }
                label.text=symbol;yield return wait;
            }
        }
        private void LateUpdate(){if(cameraRig!=null)label.transform.rotation=cameraRig.transform.rotation;}
    }
}
