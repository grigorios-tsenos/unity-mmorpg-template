using System.Collections.Generic;
using MmoTemplate.Rpg;
using NUnit.Framework;
using UnityEngine;
public class ClassicRulesTests
{
    private enum State { Root, Mobile, Idle, Moving, Casting }
    [Test] public void HierarchicalTransitionsPreserveCommonAncestors()
    {
        var trace=new List<string>();var machine=new HierarchicalStateMachine<State>();
        machine.Add(State.Root,enter:()=>trace.Add("enter root"),exit:()=>trace.Add("exit root"));
        machine.Add(State.Mobile,State.Root,()=>trace.Add("enter mobile"),()=>trace.Add("exit mobile"));
        machine.Add(State.Idle,State.Mobile,()=>trace.Add("enter idle"),()=>trace.Add("exit idle"));
        machine.Add(State.Moving,State.Mobile,()=>trace.Add("enter moving"),()=>trace.Add("exit moving"));
        machine.Add(State.Casting,State.Root,()=>trace.Add("enter casting"));
        machine.Change(State.Idle);trace.Clear();machine.Change(State.Moving);
        CollectionAssert.AreEqual(new[]{"exit idle","enter moving"},trace);
        Assert.That(machine.IsIn(State.Mobile),Is.True);
        trace.Clear();machine.Change(State.Casting);
        CollectionAssert.AreEqual(new[]{"exit moving","exit mobile","enter casting"},trace);
        Assert.That(machine.IsIn(State.Mobile),Is.False);Assert.That(machine.IsIn(State.Root),Is.True);
    }
    [Test] public void QuestLootCanSatisfyAuthoredCollectionRequirement()
    {
        var quest=Resources.Load<QuestData>("RPG/OathfireTrial");var guardian=Resources.Load<EnemyData>("RPG/Guardian");
        Assert.That(quest,Is.Not.Null);Assert.That(guardian,Is.Not.Null);
        int guaranteed=0;
        foreach(var entry in guardian.loot.entries) if(entry.item==quest.collectItem&&entry.chance==1) guaranteed+=entry.count;
        Assert.That(guaranteed*quest.guardianKills,Is.GreaterThanOrEqualTo(quest.collectCount));
        Assert.That(quest.rewardItem,Is.Not.Null);
    }
    [Test] public void AbilityLoadoutCoversAllResourcesAndHasRealCosts()
    {
        foreach(string id in new[]{"Fireball","HeroicStrike","SinisterStrike"})
        {
            var spell=Resources.Load<SpellData>("RPG/"+id);
            Assert.That(spell,Is.Not.Null);Assert.That(spell.resourceCost,Is.InRange(1,100));
            Assert.That(spell.globalCooldown,Is.EqualTo(1.5f));Assert.That(spell.power,Is.GreaterThan(0));
        }
        Assert.That(Resources.Load<SpellData>("RPG/Fireball").resourceType,Is.EqualTo(ResourceType.Mana));
        Assert.That(Resources.Load<SpellData>("RPG/HeroicStrike").resourceType,Is.EqualTo(ResourceType.Rage));
        Assert.That(Resources.Load<SpellData>("RPG/SinisterStrike").resourceType,Is.EqualTo(ResourceType.Energy));
    }
}
