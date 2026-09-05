using System.Collections.Generic;
using UnityEngine;
namespace MmoTemplate.Rpg
{
    public abstract class InteractionPoint : MonoBehaviour, IInteraction
    {
        public abstract string Prompt { get; }
        public abstract void Interact(PlayerStats player);
        public abstract bool Choose(PlayerStats player,int action);
        protected virtual void OnEnable()=>InteractionRegistry.All.Add(this);
        protected virtual void OnDisable()=>InteractionRegistry.All.Remove(this);
    }
    public static class InteractionRegistry { public static readonly List<InteractionPoint> All=new(); }
}
