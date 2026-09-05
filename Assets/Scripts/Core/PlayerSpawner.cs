using Unity.Netcode;
using UnityEngine;

namespace MmoTemplate.Network
{
    /// <summary>
    /// Server-side spawn logic. Netcode for GameObjects already auto-spawns the
    /// NetworkManager's configured Player Prefab for each connecting client, but
    /// the default spawn point is the origin for everyone — this repositions each
    /// avatar to a random point so players don't stack, exactly like the
    /// _random_spawn_point() helper in the Godot template's world.gd.
    ///
    /// Attach to a "GameManager" object in World.unity (server + host only need
    /// this to do anything; it's a no-op on pure clients).
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private float spawnRadius = 4f;

        private void Start()
        {
            if (!NetworkManager.Singleton.IsServer) return;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // Host is also client 0 / connects to itself before this script's
            // Start() runs in some load orders — cover that case too.
            foreach (var kv in NetworkManager.Singleton.ConnectedClients)
                PlacePlayer(kv.Key);
        }

        private void OnClientConnected(ulong clientId) => PlacePlayer(clientId);

        private void PlacePlayer(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
            if (client.PlayerObject == null) return;

            Vector3 spawn = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                1f,
                Random.Range(-spawnRadius, spawnRadius));
            client.PlayerObject.transform.position = spawn;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}
