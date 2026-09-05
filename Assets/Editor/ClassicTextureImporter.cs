#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public sealed class ClassicTextureImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/ClassicTextures/")) return;
        var importer=(TextureImporter)assetImporter;
        importer.maxTextureSize=1024;
        importer.mipmapEnabled=true;
        importer.sRGBTexture=true;
        importer.textureType=TextureImporterType.Default;
        importer.textureCompression=TextureImporterCompression.Compressed;
    }
    [System.Serializable] public sealed class Mapping { public Entry[] entries; }
    [System.Serializable] public sealed class Entry { public string sourceAsset,materialName,texturePath; }
    private static Mapping map;
    public static Texture Diffuse(Material source)
    {
        if(map==null)map=JsonUtility.FromJson<Mapping>(System.IO.File.ReadAllText("Assets/Editor/ClassicTextureMap.json"));
        string path=AssetDatabase.GetAssetPath(source);
        foreach(var entry in map.entries)
            if(entry.sourceAsset==path && entry.materialName==source.name)
                return AssetDatabase.LoadAssetAtPath<Texture2D>(entry.texturePath);
        return null;
    }
}
#endif
