using Unity.Netcode;
using UnityEngine;

namespace MmoTemplate.Rpg
{
    /// <summary>
    /// World chat. Client -> server -> everyone, using Netcode 2.x's modern
    /// [Rpc(SendTo...)] attributes. Replaces the older ChatManager, which relied
    /// on serialized UI references and a deprecated ServerRpc signature.
    /// </summary>
    public class ChatRelay : NetworkBehaviour
    {
        public static ChatRelay Instance { get; private set; }

        private void Awake() => Instance = this;

        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy(); // NetworkBehaviour does its own cleanup here
        }

        /// <summary>Called by the HUD when the local player submits a chat line.</summary>
        public void Send(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
            if (text.Length > 200) text = text[..200];
            SubmitRpc(text);
        }

        // ChatRelay lives on the GameManager, which no client owns — so anyone
        // is allowed to invoke this, not just the owner.
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitRpc(string text, RpcParams rpcParams = default)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            text = text.Replace("<", "").Replace(">", "");
            if (text.Length > 200) text = text[..200];
            ulong sender = rpcParams.Receive.SenderClientId;
            string who = $"Player {sender}";

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(sender, out var client)
                && client.PlayerObject != null
                && client.PlayerObject.TryGetComponent<Player.PlayerController>(out var pc))
            {
                string named = pc.PlayerName.Value.ToString();
                if (!string.IsNullOrEmpty(named)) who = named;
            }

            BroadcastRpc(who, text);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
        private void BroadcastRpc(string sender, string text)
        {
            GameEvents.Say($"{sender}: {text}");
        }
    }
}
