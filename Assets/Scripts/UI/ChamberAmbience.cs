using UnityEngine;
namespace MmoTemplate.Rpg
{
    /// <summary>A quiet procedural hearth bed, local to the room; no downloaded audio.</summary>
    public sealed class ChamberAmbience : MonoBehaviour
    {
        private AudioClip clip;
        private void Start()
        {
            const int rate=22050;var samples=new float[rate*6];var random=new System.Random(2004);float filtered=0;
            for(int i=0;i<samples.Length;i++)
            {
                float noise=(float)(random.NextDouble()*2-1);filtered=Mathf.Lerp(filtered,noise,.06f);
                samples[i]=filtered*.28f+noise*.015f;
            }
            clip=AudioClip.Create("Hearth embers",samples.Length,1,rate,false);clip.SetData(samples,0);
            var source=gameObject.AddComponent<AudioSource>();source.clip=clip;source.loop=true;source.volume=.14f;source.Play();
        }
        private void OnDestroy(){if(clip!=null)Destroy(clip);}
    }
}
