using UnityEngine;
namespace MmoTemplate.Rpg
{
    public sealed class TorchFlicker : MonoBehaviour
    {
        private Light flame;
        private float baseline,seed;
        private void Awake(){flame=GetComponent<Light>();seed=transform.position.sqrMagnitude;}
        private void Start(){baseline=flame.intensity;}
        private void Update(){flame.intensity=baseline*(.9f+Mathf.PerlinNoise(seed,Time.time*3)*.2f);}
    }
}
