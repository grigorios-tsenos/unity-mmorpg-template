using System;
using UnityEngine;
namespace MmoTemplate.Rpg
{
    // Presentation subscribes here; gameplay has no dependency on UI.Runtime.
    public static class GameEvents
    {
        public static event Action<string> Notification, Chat, Prompt;
        public static event Action<string, string, string[], int[]> Dialog;
        public static event Action<int> DialogueChoice;
        public static event Action<bool, PlayerStats> EnemyKilled;
        public static event Action<Vector3, int> Damage;
        public static bool InputBlocked { get; set; }
        /// <summary>True while the cursor is over the HUD, so gameplay leaves the mouse alone.</summary>
        public static bool PointerOverUi { get; set; }
        public static string LocalPlayerName { get; set; } = "Wayfarer";
        public static void Notify(string value) => Notification?.Invoke(value);
        public static void Say(string value) => Chat?.Invoke(value);
        public static void SetPrompt(string value) => Prompt?.Invoke(value);
        public static void ShowDialog(string title, string body, string[] labels, int[] ids) => Dialog?.Invoke(title, body, labels, ids);
        public static void Choose(int id) => DialogueChoice?.Invoke(id);
        public static void Kill(bool boss, PlayerStats player) => EnemyKilled?.Invoke(boss, player);
        public static void Hit(Vector3 position, int amount) => Damage?.Invoke(position, amount);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Notification = Chat = Prompt = null; Dialog = null; DialogueChoice = null;
            EnemyKilled = null; Damage = null; InputBlocked = false; PointerOverUi = false;
        }
    }
    public enum ResourceType { Mana, Rage, Energy }
    public enum CharacterState { Idle, Moving, Casting, Stunned }
    public interface IInteraction
    {
        string Prompt { get; }
        void Interact(PlayerStats player);
        bool Choose(PlayerStats player, int action);
    }
    public interface IDialogueSender
    {
        void SendDialog(string title, string body, string[] labels, int[] actions);
    }
}
