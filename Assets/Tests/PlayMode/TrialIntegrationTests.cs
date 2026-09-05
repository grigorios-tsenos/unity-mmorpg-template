using System;
using System.Collections;
using System.Reflection;
using MmoTemplate.Rpg;
using MmoTemplate.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public class TrialIntegrationTests
{
    private static IEnumerator Until(Func<bool> condition,string message,float seconds=8)
    {
        float end=Time.realtimeSinceStartup+seconds;
        while(!condition()&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(condition(),Is.True,message);
    }
    private static void TickCombat(PlayerCombat combat)=>typeof(PlayerCombat).GetMethod("TickCombat",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(combat,null);
    private static void Place(PlayerStats player,Vector3 position)
    {
        var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=position;controller.enabled=true;
    }
    [UnityTest] public IEnumerator RoomMovementCombatAndQuestWorkWithoutNetworking()
    {
        yield return SceneManager.LoadSceneAsync("World");
        yield return Until(()=>PlayerStats.Local!=null&&PlayerCombat.Local!=null&&QuestManager.Instance!=null,"The room starts with a local player");
        yield return new WaitForSeconds(.6f);
        var player=PlayerStats.Local;var combat=PlayerCombat.Local;var quest=QuestManager.Instance;var movement=player.GetComponent<PlayerController>();
        movement.ReadInput=false;
        Assert.That(player.GetComponents<PlayerStats>().Length, Is.EqualTo(1), "One character sheet per player");
        Assert.That(movement.Grounded,Is.True,"Player stands on the room floor");
        Assert.That(GameObject.Find("Chamber"),Is.Not.Null);
        Assert.That(InteractionRegistry.All.Count,Is.EqualTo(3));
        Assert.That(player.GetComponentInChildren<Animation>(),Is.Not.Null,"Bundled character animations are wired");
        RoomPreview.Capture("/tmp/oathfire-room.png");
        // Jump keeps forward momentum even after changing input while airborne.
        float height=player.transform.position.y;
        movement.SetMovement(Vector2.up,180,true);yield return new WaitForSeconds(.15f);
        Assert.That(player.transform.position.y,Is.GreaterThan(height+.2f));
        float z=player.transform.position.z;movement.SetMovement(Vector2.down,180,false);
        yield return new WaitForSeconds(.15f);Assert.That(player.transform.position.z,Is.LessThan(z));
        movement.SetMovement(Vector2.zero,180,false);yield return new WaitForSeconds(.8f);
        Assert.That(movement.Grounded,Is.True);
        // A choice that was never offered cannot perform a purchase.
        int initialPotions=player.Potions.Value;combat.ChooseAction(4);Assert.That(player.Potions.Value,Is.EqualTo(initialPotions));
        quest.AcceptQuest(player);yield return Until(()=>Enemy.All.Count==1,"The first guardian enters the circle");
        var enemy=Enemy.All[0];Place(player,enemy.transform.position+Vector3.back*4);yield return new WaitForSeconds(.3f);
        combat.SelectTarget(enemy.Id);int mana=player.Resource.Value;combat.Use(1);
        Assert.That(combat.CastingSlot.Value,Is.EqualTo(1));
        player.Moving=true;TickCombat(combat);Assert.That(combat.CastingSlot.Value,Is.EqualTo(-1));
        Assert.That(player.Resource.Value,Is.EqualTo(mana));player.Moving=false;
        yield return new WaitForSeconds(1.6f);
        int health=enemy.Health.Value;combat.Use(1);
        yield return Until(()=>enemy.Health.Value<health,"A completed cast damages the selected target",5);
        Assert.That(player.Resource.Value,Is.EqualTo(mana-combat.Abilities[1].resourceCost));
        // Breaking pursuit restores the guardian at its home before continuing the trial.
        Place(player,new Vector3(-8,.2f,6));
        yield return Until(()=>enemy.Health.Value==enemy.MaxHealth,"Leashed guardian returns and heals",10);
        Place(player,new Vector3(4,.2f,3));yield return new WaitForSeconds(.3f);
        for(int i=0;i<quest.Definition.guardianKills;i++)
        {
            Enemy.All[0].TakeDamage(999,player);yield return null;
            yield return Until(()=>Enemy.All.Count==1,"Next trial opponent spawns");
        }
        Assert.That(quest.Kills.Value,Is.EqualTo(quest.Definition.guardianKills));
        Assert.That(quest.Collected.Value,Is.EqualTo(quest.Definition.collectCount));
        Assert.That(quest.Stage.Value,Is.EqualTo(2));Assert.That(Enemy.All[0].IsBoss.Value,Is.True);
        Enemy.All[0].TakeDamage(999,player);yield return null;Assert.That(quest.Stage.Value,Is.EqualTo(3));
        quest.LightBrazier(player);Assert.That(quest.Stage.Value,Is.EqualTo(4));
        int money=player.Gold.Value;quest.CompleteQuest(player);quest.CompleteQuest(player);
        Assert.That(player.Gold.Value,Is.EqualTo(money+quest.Definition.rewardCopper));Assert.That(quest.Stage.Value,Is.EqualTo(5));
        Assert.That(player.ItemCount(quest.Definition.collectItem.itemId),Is.Zero);
        player.Health.Value=10;player.DrinkPotion();int count=player.Potions.Value;player.DrinkPotion();Assert.That(player.Potions.Value,Is.EqualTo(count));
        player.ResourceKind.Value=ResourceType.Energy;player.Resource.Value=0;yield return new WaitForSeconds(2.2f);Assert.That(player.Resource.Value,Is.GreaterThanOrEqualTo(20));
        player.TakeDamage(999);Assert.That(player.IsDead,Is.True);yield return new WaitForSeconds(5.2f);Assert.That(player.IsDead,Is.False);
        Assert.That(player.transform.position.y,Is.GreaterThan(-.5f));
    }
    [UnityTearDown] public IEnumerator Cleanup(){yield return SceneManager.LoadSceneAsync("World");yield return null;}
}
