using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace MmoTemplate.Rpg
{
    public partial class RpgHud
    {
        private PlayerStats _player;
        private PlayerCombat _combat;
        private QuestManager _quest;
        private Enemy _target;
        private Image _resourceFill, _targetFill, _castFill;
        private Text _resourceText,_targetText,_castText,_journalText;
        private GameObject _targetPanel,_castPanel,_journal;
        private Text[] _abilityLabels=new Text[6];
        private Image[] _cooldownFills=new Image[6];
        private Button[] _abilityButtons=new Button[6];
        private RectTransform _root;
        private Camera _worldCamera;
        private Coroutine _typing;
        private readonly System.Collections.Generic.List<FloatingNumber> _numbers=new();
        private sealed class FloatingNumber { public Text Label; public Vector3 World; public float Age; }
        private void BuildClassicChrome(RectTransform root)
        {
            _root=root; _worldCamera=Camera.main;
            var playerPanel=_healthFill.transform.parent.parent as RectTransform;
            _resourceFill=Bar(playerPanel,new Vector2(12,-68),new Vector2(244,15),new Color(.15f,.3f,.7f));
            _resourceText=Label(playerPanel,"",11,Color.white,new Vector2(12,-67),new Vector2(244,16));
            _resourceText.alignment=TextAnchor.MiddleCenter;
            var target=Panel(root,new Vector2(280,86),new Vector2(314,-16),new Vector2(0,1),new Vector2(0,1));
            _targetPanel=target.gameObject;
            _targetText=Label(target,"",14,Parchment,new Vector2(12,-12),new Vector2(256,45));
            _targetFill=Bar(target,new Vector2(12,-61),new Vector2(256,13),new Color(.62f,.16f,.12f));
            _targetPanel.SetActive(false);
            var cast=Panel(root,new Vector2(330,48),new Vector2(0,168),new Vector2(.5f,0),new Vector2(.5f,0));
            _castPanel=cast.gameObject;
            _castFill=Bar(cast,new Vector2(9,-9),new Vector2(312,30),new Color(.65f,.45f,.16f));
            _castText=Label(cast,"",14,Parchment,new Vector2(9,-13),new Vector2(312,26));_castText.alignment=TextAnchor.MiddleCenter;
            _castPanel.SetActive(false);
            var actions=Panel(root,new Vector2(474,90),new Vector2(25,20),new Vector2(.5f,0),new Vector2(.5f,0));
            for(int i=0;i<6;i++)
            {
                int slot=i;
                var button=MakeButton(actions,""); _abilityButtons[i]=button;
                var rect=(RectTransform)button.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
                rect.anchoredPosition=new Vector2(10+i*77,-10);rect.sizeDelta=new Vector2(69,68);
                button.GetComponent<Image>().color=i switch {1=>new Color(.40f,.15f,.06f),2=>new Color(.19f,.32f,.20f),3=>new Color(.39f,.12f,.09f),4=>new Color(.32f,.29f,.11f),_=>new Color(.24f,.22f,.18f)};
                _abilityLabels[i]=button.GetComponentInChildren<Text>();_abilityLabels[i].fontSize=12;
                var veil=new GameObject("Cooldown",typeof(RectTransform),typeof(Image));veil.transform.SetParent(button.transform,false);
                var vr=(RectTransform)veil.transform;vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;vr.offsetMin=vr.offsetMax=Vector2.zero;
                var fill=veil.GetComponent<Image>();fill.color=new Color(0,0,0,.65f);fill.raycastTarget=false;_cooldownFills[i]=fill;
                _abilityLabels[i].transform.SetAsLastSibling();
                button.onClick.AddListener(()=>{if(slot==5)_player?.DrinkPotionRpc();else _combat?.Use(slot);});
            }
            var help=Label(root,"TAB target  ·  1 attack  ·  2–5 abilities  ·  6 potion  ·  E talk  ·  L quest log\nW/S move  ·  A/D turn (strafe with RMB)  ·  Q strafe left  ·  Space jump  ·  Mouse buttons orbit / steer",12,Parchment,new Vector2(-16,18),new Vector2(350,60),new Vector2(1,0),new Vector2(1,0));
            help.alignment=TextAnchor.LowerRight;
            var mode=Panel(root,new Vector2(280,42),new Vector2(16,-160),new Vector2(0,1),new Vector2(0,1));
            for(int i=0;i<3;i++)
            {
                var kind=(ResourceType)i;var button=MakeButton(mode,kind.ToString());var r=(RectTransform)button.transform;
                r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(8+i*89,-6);r.sizeDelta=new Vector2(85,30);
                button.onClick.AddListener(()=>_player?.ChangeResourceRpc(kind));
            }
            var journal=Panel(root,new Vector2(500,360),Vector2.zero,new Vector2(.5f,.5f),new Vector2(.5f,.5f));
            _journal=journal.gameObject;journal.GetComponent<Image>().color=new Color(.77f,.68f,.49f,.98f);
            Label(journal,"QUEST LOG   ·   THE OATHFIRE TRIAL",20,new Color(.23f,.13f,.06f),new Vector2(24,-24),new Vector2(450,32));
            _journalText=Label(journal,"",16,new Color(.23f,.13f,.06f),new Vector2(24,-72),new Vector2(450,250));
            _journal.SetActive(false);
            _dialogPanel.GetComponent<Image>().color=new Color(.16f,.12f,.075f,.99f);
            _dialogPanel.transform.SetAsLastSibling();
        }
        private void ConnectEvents()
        {
            GameEvents.Notification+=Notify;GameEvents.Chat+=AppendChat;GameEvents.Prompt+=SetPrompt;
            GameEvents.Dialog+=OpenDialog;GameEvents.Damage+=FloatDamage;
            PlayerStats.LocalChanged+=BindPlayer;PlayerCombat.LocalChanged+=BindCombat;QuestManager.Available+=BindQuest;
            BindPlayer(PlayerStats.Local);BindCombat(PlayerCombat.Local);BindQuest();
        }
        private void DisconnectEvents()
        {
            GameEvents.Notification-=Notify;GameEvents.Chat-=AppendChat;GameEvents.Prompt-=SetPrompt;
            GameEvents.Dialog-=OpenDialog;GameEvents.Damage-=FloatDamage;
            PlayerStats.LocalChanged-=BindPlayer;PlayerCombat.LocalChanged-=BindCombat;QuestManager.Available-=BindQuest;
            if(_player!=null)_player.Changed-=RefreshPlayer;
            if(_combat!=null)_combat.Changed-=RefreshCombat;
            if(_quest!=null)_quest.Changed-=RefreshQuest;
            UnbindTarget();
        }
        private void BindPlayer(PlayerStats player)
        {
            if(_player!=null)_player.Changed-=RefreshPlayer;_player=player;
            if(_player!=null)_player.Changed+=RefreshPlayer;RefreshPlayer();
        }
        private void BindCombat(PlayerCombat combat)
        {
            if(_combat!=null)_combat.Changed-=RefreshCombat;_combat=combat;
            if(_combat!=null)_combat.Changed+=RefreshCombat;RefreshCombat();
        }
        private void BindQuest()
        {
            if(_quest!=null)_quest.Changed-=RefreshQuest;_quest=QuestManager.Instance;
            if(_quest!=null)_quest.Changed+=RefreshQuest;RefreshQuest();
        }
        private void RefreshPlayer()
        {
            Refresh(_player,QuestManager.Title,_quest?.Objective() ?? "Speak to Mira.");
            if(_player==null)return;
            _resourceFill.rectTransform.anchorMax=new Vector2(_player.Resource.Value/100f,1);
            _resourceFill.color=_player.ResourceKind.Value switch {ResourceType.Rage=>new Color(.65f,.16f,.1f),ResourceType.Energy=>new Color(.76f,.65f,.13f),_=>new Color(.15f,.32f,.68f)};
            _resourceText.text=$"{_player.ResourceKind.Value}  {_player.Resource.Value} / 100";
        }
        private void RefreshQuest()
        {
            if(_quest==null)return;
            _questText.text=_quest.Objective();
            var definition=_quest.Definition;
            if(definition!=null)_journalText.text=definition.briefing+"\n\n"+_quest.Objective()+$"\n\nRewards: {definition.rewardXp} XP · {definition.rewardCopper} copper\n{definition.rewardPotions} potions · {definition.rewardItem.displayName}\n\n[L] Close quest log";
        }
        private void UnbindTarget()
        {
            if(_target==null)return;
            _target.Health.OnValueChanged-=TargetHealth;_target.State.OnValueChanged-=TargetState;
        }
        private void RefreshCombat()
        {
            var target=_combat?.Target;
            if(target!=_target)
            {
                UnbindTarget();_target=target;
                if(_target!=null){_target.Health.OnValueChanged+=TargetHealth;_target.State.OnValueChanged+=TargetState;}
            }
            RefreshTarget();
        }
        private void TargetHealth(int oldValue,int newValue)=>RefreshTarget();
        private void TargetState(EnemyState oldValue,EnemyState newValue)=>RefreshTarget();
        private void RefreshTarget()
        {
            _targetPanel.SetActive(_target!=null && _target.Health.Value>0 && !DialogOpen);
            if(_target==null)return;
            _targetText.text=$"{_target.Definition.displayName}\n{_target.Health.Value} / {_target.MaxHealth} HP · {_target.State.Value}";
            _targetFill.rectTransform.anchorMax=new Vector2(_target.Health.Value/(float)_target.MaxHealth,1);
        }
        private void OpenDialog(string title,string body,string[] labels,int[] ids)
        {
            _journal.SetActive(false);
            var options=new (string,Action)[labels.Length];
            for(int i=0;i<labels.Length;i++){int id=ids[i];options[i]=(labels[i],()=>{Close();GameEvents.Choose(id);});}
            ShowDialog(title,body,options);
        }
        private void BeginTyping(string body)
        {
            if(_typing!=null)StopCoroutine(_typing);
            _typing=StartCoroutine(Type(body));
        }
        private IEnumerator Type(string body)
        {
            _dialogBody.text="";
            for(int n=0;n<body.Length;n+=3){_dialogBody.text=body.Substring(0,Mathf.Min(n+3,body.Length));yield return new WaitForSecondsRealtime(.025f);}
            _typing=null;
        }
        private void UpdateClassicChrome()
        {
            _targetPanel.SetActive(_target!=null && _target.Health.Value>0 && !DialogOpen);
            _castPanel.SetActive(_combat!=null && _combat.CastingSlot.Value>=0 && !DialogOpen);
            if(_combat!=null)
            {
                int cast=_combat.CastingSlot.Value;
                if(cast>=0)
                {
                    var spell=_combat.Abilities[cast];double left=Math.Max(0,_combat.CastEnd.Value-_combat.Now);
                    _castFill.rectTransform.anchorMax=new Vector2(1-(float)(left/Math.Max(.01f,spell.castTime)),1);
                    _castText.text=$"{spell.spellName}  {left:0.0}s";
                }
                for(int i=0;i<6;i++)
                {
                    var spell=i<5?_combat.Abilities[i]:null;
                    double left=i<5?Math.Max(_combat.Cooldown(i),i==0?0:_combat.GlobalReady.Value-_combat.Now):(_player!=null?Math.Max(0,_player.PotionReady.Value-_combat.Now):0);
                    _cooldownFills[i].rectTransform.anchorMax=new Vector2(1,Mathf.Clamp01((float)(left/Math.Max(1.5f,spell!=null?spell.cooldown:60f))));
                    _abilityLabels[i].text=$"{i+1}\n"+(spell!=null?spell.spellName:"Potion")+(left>0?$"\n{left:0.0}":i==0 && _combat.AutoAttacking.Value?"\nON":"");
                    bool usable=_player!=null&&!_player.IsDead&&(spell==null || spell.resourceCost==0 || (_player.ResourceKind.Value==spell.resourceType && _player.Resource.Value>=spell.resourceCost));
                    _abilityButtons[i].interactable=usable&&!DialogOpen&&!ChatFocused;
                }
            }
            for(int i=_numbers.Count-1;i>=0;i--)
            {
                var number=_numbers[i];number.Age+=Time.deltaTime;
                if(number.Age>1.3f){Destroy(number.Label.gameObject);_numbers.RemoveAt(i);continue;}
                if(_worldCamera==null)continue;
                var screen=_worldCamera.WorldToScreenPoint(number.World+Vector3.up*number.Age);
                number.Label.gameObject.SetActive(screen.z>0);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_root,screen,null,out var point);
                number.Label.rectTransform.anchoredPosition=point;
                var color=number.Label.color;color.a=1-number.Age/1.3f;number.Label.color=color;
            }
        }
        private void FloatDamage(Vector3 position,int amount)
        {
            if(_numbers.Count>=40)return;
            var label=Label(_root,Math.Abs(amount).ToString(),28,amount<0?new Color(1,.25f,.2f):new Color(1,.86f,.3f),Vector2.zero,new Vector2(120,40),new Vector2(.5f,.5f),new Vector2(.5f,.5f));
            label.alignment=TextAnchor.MiddleCenter;label.fontStyle=FontStyle.Bold;
            var shadow=label.gameObject.AddComponent<Shadow>();shadow.effectDistance=new Vector2(2,-2);
            _numbers.Add(new FloatingNumber{Label=label,World=position});
        }
    }
}
