using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
internal static class RoomPreview
{
    public static void Capture(string path)
    {
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return;
        var camera=Camera.main;var canvas=Object.FindAnyObjectByType<Canvas>();
        var target=new RenderTexture(1280,720,24);target.Create();
        var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldDistance=canvas.planeDistance;
        camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;
        Canvas.ForceUpdateCanvases();
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
        RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
        File.WriteAllBytes(path,image.EncodeToPNG());
        camera.targetTexture=oldTarget;RenderTexture.active=oldActive;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;
        Object.Destroy(image);target.Release();Object.Destroy(target);
    }
}
