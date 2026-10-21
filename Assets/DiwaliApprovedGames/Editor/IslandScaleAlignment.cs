#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public static class IslandScaleAlignment
{
    const string Reports = "AgentScripts/ApprovedPaletteImport";
    static float[] V(Vector3 v) => new[] {v.x,v.y,v.z};
    static void Write(string name, object value) => File.WriteAllText(Reports+"/"+name, Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));
    static Bounds VertexBounds(GameObject go)
    {
        var filter=go.GetComponent<MeshFilter>();
        using(var data=UnityEditor.MeshUtility.AcquireReadOnlyMeshData(filter.sharedMesh))
        using(var vertices=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp))
        {
            data[0].GetVertices(vertices);
            var b=new Bounds(go.transform.TransformPoint(vertices[0]),Vector3.zero);
            for(int i=1;i<vertices.Length;i++) b.Encapsulate(go.transform.TransformPoint(vertices[i]));
            return b;
        }
    }
    [MenuItem("Diwali Island/Scale Alignment/Inspect")]
    public static void Inspect()
    {
        var diwali=VertexBounds(GameObject.Find("Courtyard stone foundation"));
        var christmas=VertexBounds(GameObject.Find("Island_Terrain"));
        Write("island-size-inspection.json",new {diwaliSize=V(diwali.size),christmasSize=V(christmas.size),scaleFactor=Mathf.Max(christmas.size.x,christmas.size.z)/Mathf.Max(diwali.size.x,diwali.size.z)});
    }
    [MenuItem("Diwali Island/Scale Alignment/Match Christmas")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode first.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/DiwaliFestivalGround.unity")throw new InvalidOperationException("Wrong scene.");
        var root=GameObject.Find("Diwali Festival Ground").transform;
        if((root.localScale-Vector3.one).sqrMagnitude>.00001f)throw new InvalidOperationException("Diwali has already been resized; inspect before resizing again.");
        var diwali=VertexBounds(GameObject.Find("Courtyard stone foundation"));
        var christmasObject=GameObject.Find("Island_Terrain");var christmas=VertexBounds(christmasObject);
        float factor=Mathf.Max(christmas.size.x,christmas.size.z)/Mathf.Max(diwali.size.x,diwali.size.z);
        if(factor<1 || factor>3)throw new InvalidOperationException("Unexpected size ratio.");
        string backup="Scene_Backups/ApprovedPaletteImport/before-island-scale-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ")+".unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup));
        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new IOException("Backup failed.");
        var background=new[]{GameObject.Find("Earth beyond the walls").transform,GameObject.Find("Rolling earth landscape").transform};
        var positions=background.Select(t=>t.position).ToArray();var scales=background.Select(t=>t.lossyScale).ToArray();
        var rig=GameObject.Find("XR Origin Hands (XR Rig)").transform;var rigBefore=rig.position;
        Vector3 anchor=new Vector3(0,0,-30);
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Match Diwali island footprint to Christmas island");
        Undo.RecordObject(root,"Resize Diwali island");root.position=anchor+(root.position-anchor)*factor;root.localScale=Vector3.one*factor;
        for(int i=0;i<background.Length;i++)
        {
            Undo.RecordObject(background[i],"Preserve shared background landscape");background[i].position=positions[i];background[i].localScale=scales[i]/factor;
        }
        foreach(var light in root.GetComponentsInChildren<Light>(true))
            if(light.type==LightType.Point || light.type==LightType.Spot){Undo.RecordObject(light,"Match light coverage to island scale");light.range*=factor;}
        Undo.RecordObject(rig,"Keep player at Diwali entrance");
        rig.position=new Vector3(anchor.x+(rigBefore.x-anchor.x)*factor,rigBefore.y,anchor.z+(rigBefore.z-anchor.z)*factor);
        Undo.FlushUndoRecordObjects();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Undo.CollapseUndoOperations(group);
        var after=VertexBounds(GameObject.Find("Courtyard stone foundation"));
        var christmasAfter=VertexBounds(christmasObject);
        if((christmasAfter.size-christmas.size).sqrMagnitude>.00001f)throw new InvalidOperationException("Christmas island changed.");
        Write("island-scale-alignment.json",new {factor,backup,diwaliBefore=V(diwali.size),diwaliAfter=V(after.size),christmas=V(christmas.size),position=V(root.position),scale=V(root.localScale),entranceAnchor=V(anchor),rigPosition=V(rig.position),backgroundPositions=background.Select(t=>V(t.position)).ToArray(),walkwayUnchanged=true,saved=!scene.isDirty});
        var view=SceneView.lastActiveSceneView;if(view)view.LookAt(new Vector3(0,0,-40),Quaternion.Euler(65,0,0),140,false,true);
    }
    [MenuItem("Diwali Island/Scale Alignment/Clear Expanded Grounds")]
    public static void ClearGrounds()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode first.");
        var area=VertexBounds(GameObject.Find("Courtyard stone foundation"));
        var landscape=GameObject.Find("Rolling earth landscape");var filter=landscape.GetComponent<MeshFilter>();
        string path="Assets/DiwaliApprovedGames/ExpandedGroundsLandscape.asset";
        if(AssetDatabase.LoadAssetAtPath<Mesh>(path))throw new InvalidOperationException("Expanded ground clearance already exists.");
        var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name="Landscape cleared around expanded Diwali grounds";
        var vertices=mesh.vertices;int changed=0;
        for(int i=0;i<vertices.Length;i++)
        {
            var w=landscape.transform.TransformPoint(vertices[i]);
            float outside=Mathf.Max(Mathf.Abs(w.x-area.center.x)-area.extents.x-3,Mathf.Abs(w.z-area.center.z)-area.extents.z-3);
            float weight=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(outside/7));
            if(weight<=0 || w.y<=-.22f)continue;
            w.y=Mathf.Lerp(w.y,-.22f,weight);vertices[i]=landscape.transform.InverseTransformPoint(w);changed++;
        }
        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        Undo.RecordObject(filter,"Clear hills from expanded grounds");filter.sharedMesh=mesh;
        var collider=landscape.GetComponent<MeshCollider>();if(collider){Undo.RecordObject(collider,"Match landscape collider");collider.sharedMesh=mesh;}
        Undo.FlushUndoRecordObjects();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(landscape.scene);EditorSceneManager.SaveScene(landscape.scene);
        Write("expanded-grounds-clearance.json",new {changedVertices=changed,mesh=path,saved=!landscape.scene.isDirty});
    }
    [MenuItem("Diwali Island/Scale Alignment/Render Both Islands")]
    public static void Render()
    {
        string folder=Path.GetFullPath("../Scene_Previews/Island-Scale-Alignment");Directory.CreateDirectory(folder);
        var go=new GameObject("Temporary island scale review camera");go.hideFlags=HideFlags.HideAndDontSave;
        var camera=go.AddComponent<Camera>();camera.GetUniversalAdditionalCameraData().allowXRRendering=false;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;camera.fieldOfView=55;camera.farClipPlane=700;
        go.transform.position=new Vector3(100,180,-155);go.transform.LookAt(new Vector3(0,0,-40));
        var rt=new RenderTexture(1800,1200,24);var previous=RenderTexture.active;Texture2D pixels=null;bool fog=RenderSettings.fog;RenderSettings.fog=false;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);pixels.Apply();File.WriteAllBytes(folder+"/Both-Islands.png",pixels.EncodeToPNG());}
        finally{RenderSettings.fog=fog;camera.targetTexture=null;RenderTexture.active=previous;if(pixels)UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
}
#endif
