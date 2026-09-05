#if UNITY_EDITOR
using MmoTemplate.Rpg;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class ClassicContentBuilder
{
    private const string Root="Assets/Resources/RPG/";
    public static void Build()
    {
        System.IO.Directory.CreateDirectory(Root);
        // Existing definitions are preserved so designers can tune values safely.
        var potion=Asset<ItemData>("HealingPotion",a=>{a.itemId="healing_potion";a.displayName="Minor Healing Potion";a.healing=60;a.priceCopper=12;a.description="A small crimson draught, stoppered with wax. Keep it close when steel is drawn.";});
        var ember=Asset<ItemData>("OathEmber",a=>{a.itemId="oath_ember";a.displayName="Oathfire Ember";a.questItem=true;a.description="A warm fragment of a guardian's oath.";});
        var seal=Asset<ItemData>("HearthSeal",a=>{a.itemId="hearth_seal";a.displayName="Seal of the Hearth";a.description="A keepsake awarded to those who rekindle the Oathfire.";});
        var loot=Asset<LootTable>("GuardianLoot",a=>{a.copper=6;a.experience=25;a.entries=new[]{new LootTable.Entry{item=ember,chance=1,count=1}};});
        var bossLoot=Asset<LootTable>("WardenLoot",a=>{a.copper=25;a.experience=60;});
        Asset<EnemyData>("Guardian",a=>{a.displayName="Oathbound Guardian";a.health=65;a.damage=6;a.loot=loot;});
        Asset<EnemyData>("Warden",a=>{a.displayName="Oathbound Warden";a.health=220;a.damage=12;a.speed=2.3f;a.attackInterval=2;a.loot=bossLoot;});
        Spell("Strike","Attack",0,2,3,0,15,ResourceType.Mana,false,true);
        Spell("Fireball","Ember Bolt",2.2f,0,18,18,40,ResourceType.Mana);
        Spell("Mend","Hearthlight",2,6,0,25,50,ResourceType.Mana,true);
        Spell("HeroicStrike","Heroic Strike",0,3,3,15,35,ResourceType.Rage);
        Spell("SinisterStrike","Quick Slash",0,1,3,40,30,ResourceType.Energy);
        Asset<QuestData>("OathfireTrial",a=>{a.title="THE OATHFIRE TRIAL";a.briefing="Welcome, traveler. Our hearth grows cold, and the old vows must be renewed.\n\nDefeat three guardians in the eastern proving circle and gather their Oathfire Embers. Face the warden, rekindle the brazier, then return to me.\n\nYour companions share this trial. Each earns a reward.";a.guardianKills=3;a.collectCount=3;a.collectItem=ember;a.rewardCopper=80;a.rewardXp=100;a.rewardPotions=2;a.rewardItem=seal;});
        ConfigurePipeline(); AssetDatabase.SaveAssets();
    }
    private static T Asset<T>(string name,System.Action<T> initialize) where T:ScriptableObject
    {
        var existing=AssetDatabase.LoadAssetAtPath<T>(Root+name+".asset");if(existing!=null)return existing;
        var value=ScriptableObject.CreateInstance<T>();initialize(value);AssetDatabase.CreateAsset(value,Root+name+".asset");return value;
    }
    private static void Spell(string id,string name,float cast,float cooldown,float range,int cost,int power,ResourceType type,bool healing=false,bool auto=false)
        =>Asset<SpellData>(id,a=>{a.spellName=name;a.castTime=cast;a.cooldown=cooldown;a.range=range;a.resourceCost=cost;a.power=power;a.resourceType=type;a.healing=healing;a.autoAttack=auto;a.globalCooldown=auto?0:1.5f;});
    private static void ConfigurePipeline()
    {
        const string path="Assets/Settings/ClassicURP.asset";
        System.IO.Directory.CreateDirectory("Assets/Settings");
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if(pipeline==null)
        {
            var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer,"Assets/Settings/ClassicRenderer.asset");
            pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,path);
        }
        GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
    }
}
#endif
