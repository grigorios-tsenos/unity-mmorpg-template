using Unity.Netcode;
using UnityEngine;
namespace MmoTemplate.Rpg
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Vector3 circleCenter=new(4.8f,.2f,-1.5f);
        public void SpawnNext(bool boss)
        {
            if(NetworkManager.Singleton==null || !NetworkManager.Singleton.IsServer || enemyPrefab==null) return;
            var go=Instantiate(enemyPrefab,circleCenter,Quaternion.identity);
            var enemy=go.GetComponent<Enemy>();
            enemy.IsBoss.Value=boss; // Included in the initial spawn payload on every peer.
            go.GetComponent<NetworkObject>().Spawn(true);
        }
    }
}
