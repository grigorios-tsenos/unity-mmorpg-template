using System;
using System.Collections;
using System.Reflection;
using MmoTemplate.Rpg;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public class TrialIntegrationTests
{
    private static IEnumerator Until(Func<bool> predicate,string message,float timeout=15)
    {
        float end=Time.realtimeSinceStartup+timeout;
        while(!predicate()&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(predicate(),Is.True,message);
    }
    private static void Invoke(object target,string name,params object[] args)
        =>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
    [UnityTest] public IEnumerator HostTrialCastsLootAndRewardsAreAuthoritative()
    {
        yield return SceneManager.LoadSceneAsync("Bootstrap");
        var network=NetworkManager.Singleton;
        Assert.That(network.StartHost(),Is.True);
        network.SceneManager.LoadScene("World",LoadSceneMode.Single);
        yield return Until(()=>QuestManager.Instance!=null&&QuestManager.Instance.IsSpawned&&PlayerStats.Local!=null&&PlayerCombat.Local!=null,"World and local player spawn");
        yield return new WaitForSeconds(.5f);
        var player=PlayerStats.Local;var combat=PlayerCombat.Local;var quest=QuestManager.Instance;
        Assert.That(Camera.main.GetComponent<MmoTemplate.Player.CameraFollow>(),Is.Not.Null);
        // Arbitrary dialogue IDs must not grant rewards or potions.
        int originalPotions=player.Potions.Value;
        Invoke(combat,"ChooseRpc",4);
        Assert.That(player.Potions.Value,Is.EqualTo(originalPotions));
        quest.AcceptQuest(player);
        yield return Until(()=>Enemy.All.Count==1,"First guardian spawns");
        var enemy=Enemy.All[0];
        var controller=player.GetComponent<CharacterController>();
        controller.enabled=false;player.transform.position=enemy.transform.position+Vector3.back*4;controller.enabled=true;
        yield return new WaitForSeconds(.3f);
        Invoke(combat,"SetTargetRpc",enemy.NetworkObjectId);
        int mana=player.Resource.Value;
        combat.Use(1);
        yield return Until(()=>combat.CastingSlot.Value==1,"Ember Bolt starts casting");
        // Moving during a cast cancels it without charging mana.
        player.Moving=true;Invoke(combat,"ServerTick");
        Assert.That(combat.CastingSlot.Value,Is.EqualTo(-1));Assert.That(player.Resource.Value,Is.EqualTo(mana));
        player.Moving=false;
        yield return new WaitForSeconds(1.6f);
        int before=enemy.Health.Value;
        combat.Use(1);
        yield return Until(()=>enemy.Health.Value<before,"Completed cast damages selected target",5);
        Assert.That(player.Resource.Value,Is.EqualTo(mana-combat.Abilities[1].resourceCost));
        // Shared kill/collect progress is derived from actual authored loot.
        for(int i=0;i<quest.Definition.guardianKills;i++)
        {
            Enemy.All[0].TakeDamage(999,player);
            yield return null;
        }
        Assert.That(quest.Kills.Value,Is.EqualTo(quest.Definition.guardianKills));
        Assert.That(quest.Collected.Value,Is.EqualTo(quest.Definition.collectCount));
        Assert.That(quest.Stage.Value,Is.EqualTo(2));
        Assert.That(Enemy.All[0].IsBoss.Value,Is.True);
        Enemy.All[0].TakeDamage(999,player);yield return null;
        Assert.That(quest.Stage.Value,Is.EqualTo(3));
        quest.LightBrazier(player);Assert.That(quest.Stage.Value,Is.EqualTo(4));
        int gold=player.Gold.Value;
        quest.CompleteQuest(player);quest.CompleteQuest(player);
        Assert.That(player.Gold.Value,Is.EqualTo(gold+quest.Definition.rewardCopper));
        Assert.That(quest.Stage.Value,Is.EqualTo(5));
        Assert.That(player.ItemCount(quest.Definition.collectItem.itemId),Is.Zero);
        // Healing potions share a cooldown and cannot consume repeatedly.
        player.Health.Value=10;player.DrinkPotionRpc();int potions=player.Potions.Value;
        player.DrinkPotionRpc();Assert.That(player.Potions.Value,Is.EqualTo(potions));
        player.ResourceKind.Value=ResourceType.Energy;player.Resource.Value=0;
        yield return new WaitForSeconds(2.2f);Assert.That(player.Resource.Value,Is.GreaterThanOrEqualTo(20));
        network.Shutdown();yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(NetworkManager.Singleton!=null)
        {
            NetworkManager.Singleton.Shutdown();
            yield return Until(()=>NetworkManager.Singleton==null||!NetworkManager.Singleton.IsListening,"Network shuts down");
            if(NetworkManager.Singleton!=null)UnityEngine.Object.Destroy(NetworkManager.Singleton.gameObject);
        }
        yield return null;
    }
}
