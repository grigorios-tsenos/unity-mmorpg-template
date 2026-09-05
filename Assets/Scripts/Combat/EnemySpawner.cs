using UnityEngine;
namespace MmoTemplate.Rpg
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Vector3 circleCenter=new(4.8f,.2f,-1.5f);
        public void SpawnNext(bool boss)
        {
            if(enemyPrefab==null)return;
            var go=Instantiate(enemyPrefab,circleCenter,Quaternion.Euler(0,270,0));
            go.GetComponent<Enemy>().IsBoss.Value=boss;
        }
    }
}
