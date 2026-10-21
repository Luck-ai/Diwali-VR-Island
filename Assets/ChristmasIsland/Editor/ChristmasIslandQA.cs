#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class ChristmasIslandQA
{
    public static string Folder => Path.GetFullPath("../Scene_Previews/Christmas-Integration-20261019");
    public static string Render(string name, Vector3 position, Vector3 look, float fieldOfView=55)
    {
        Directory.CreateDirectory(Folder);
        var go=new GameObject("Temporary Christmas QA Camera",typeof(Camera));
        var cam=go.GetComponent<Camera>();cam.enabled=false;cam.fieldOfView=fieldOfView;cam.nearClipPlane=.1f;cam.farClipPlane=600;
        cam.transform.position=position;cam.transform.LookAt(look);
        cam.GetUniversalAdditionalCameraData().allowXRRendering=false;
        var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
        var previous=RenderTexture.active;
        try
        {
            rt.antiAliasing=4;cam.targetTexture=rt;
            RenderPipeline.SubmitRenderRequest(cam,new RenderPipeline.StandardRequest{destination=rt});
            RenderTexture.active=rt;
            var texture=new Texture2D(1600,1000,TextureFormat.RGBAFloat,false,true);
            texture.ReadPixels(new Rect(0,0,1600,1000),0,0);texture.Apply();
            var pixels=texture.GetPixels(); for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;
            var png=new Texture2D(1600,1000,TextureFormat.RGB24,false);png.SetPixels(pixels);png.Apply();
            string path=Path.Combine(Folder,name+".png");File.WriteAllBytes(path,png.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(png);return path;
        }
        finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
    public static string DispatchAll()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Run in Play mode.");
        var root=GameObject.Find("Christmas Island Integration");
        var items=root.GetComponentsInChildren<InteractableItem>();
        foreach(var item in items)
        {
            var action=item.GetComponent<ChristmasItemAction>();
            if(item.eventNumber==8 && action.target)item.transform.position=action.target.GetComponent<Renderer>().bounds.center;
            if(item.GetComponent<XRGrabInteractable>())item.GetComponent<XRBaseInteractable>().activated.Invoke(new ActivateEventArgs());
            else item.GetComponent<XRBaseInteractable>().selectEntered.Invoke(new SelectEnterEventArgs());
        }
        return Snapshot();
    }
    public static string Snapshot()
    {
        var root=GameObject.Find("Christmas Island Integration");
        return Newtonsoft.Json.JsonConvert.SerializeObject(new {
            playing=EditorApplication.isPlaying,
            items=root.GetComponentsInChildren<InteractableItem>().Select(i=>new {i.name,i.eventNumber,i.interactionCount,
                i.GetComponent<ChristmasItemAction>().completedCount,i.GetComponent<ChristmasItemAction>().active,
                position=i.transform.position.ToString(),
                motion=i.GetComponent<ChristmasItemAction>().motionPart?i.GetComponent<ChristmasItemAction>().motionPart.localRotation.eulerAngles.ToString():"none"}).ToArray(),
            ferris=root.GetComponentInChildren<FerrisWheelSpinner>().spinning,
            wheel=root.GetComponentInChildren<FerrisWheelSpinner>().wheel.localRotation.eulerAngles.ToString(),
            carousel=root.GetComponentInChildren<CarouselSpinner>().spinning,
            rotor=root.GetComponentInChildren<CarouselSpinner>().rotor.localRotation.eulerAngles.ToString(),
            particleCount=root.GetComponentInChildren<ParticleSystem>().particleCount
        },Newtonsoft.Json.Formatting.Indented);
    }
}
#endif
