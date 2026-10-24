#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiwaliGameplay.Editor {
public static class FestivalPathwayPolish {
    const string Source = "Assets/DiwaliGameplay/Wayfinding/";
    const string Folder = Source + "PathPolish/";
    const string SurfaceSuffix = " walking surface";
    sealed class Segment { public Vector3 a, b, side; public float length; public MeshFilter surface; }

    [MenuItem("Diwali/Polish existing festival pathways")]
    public static void Apply() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply pathway polish in Edit mode.");
        var root = GameObjectUtility.FindExact("Festival walking routes");
        if (!root) throw new InvalidOperationException("Festival walking routes is missing. No routes were created or moved.");
        var surfaces = root.GetComponentsInChildren<MeshFilter>(true).Where(x => x.name.EndsWith(SurfaceSuffix, StringComparison.Ordinal) && !x.name.StartsWith("Rounded Rangoli", StringComparison.Ordinal)).ToArray();
        if (surfaces.Length != 8) throw new InvalidOperationException("Expected eight authored festival walking surfaces, found " + surfaces.Length);
        var segments = new List<Segment>();
        foreach (var filter in surfaces) {
            var original = AssetDatabase.LoadAssetAtPath<Mesh>(Source + filter.name + ".asset");
            if (!original || original.vertexCount % 4 != 0) throw new InvalidOperationException("Unsupported original route mesh: " + filter.name);
            string route = filter.name.Substring(0, filter.name.Length - SurfaceSuffix.Length);
            var existingDetail = filter.transform.parent.Find(route + " inlaid direction arrows");
            if (!filter.GetComponent<MeshRenderer>() || !existingDetail || !existingDetail.GetComponent<MeshFilter>() || !existingDetail.GetComponent<MeshRenderer>())
                throw new InvalidOperationException("Original route renderer/inlay missing: " + route);
            var vertices = original.vertices;
            for (int i = 0; i < vertices.Length; i += 4) {
                var a = filter.transform.TransformPoint((vertices[i] + vertices[i + 1]) * .5f);
                var b = filter.transform.TransformPoint((vertices[i + 2] + vertices[i + 3]) * .5f);
                segments.Add(new Segment { a = a, b = b, side = Vector3.Cross(Vector3.up, (b-a).normalized), length = Vector3.Distance(a,b), surface = filter });
            }
        }
        if (!AssetDatabase.LoadAssetAtPath<Material>("Assets/DiwaliMaterialUpgrade/Materials/Tile_A551D1A044AFA3D3.mat"))
            throw new InvalidOperationException("Existing tile PBR material is missing. No pathway was changed.");
        EnsureFolder();
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Polish existing festival pathway appearance");
        var paving = PavingMaterial();
        var inlay = InlayMaterial();
        int markers = 0;
        foreach (var filter in surfaces) {
            var original = AssetDatabase.LoadAssetAtPath<Mesh>(Source + filter.name + ".asset");
            var mesh = UnityEngine.Object.Instantiate(original);
            mesh.name = filter.name + " tiled visual";
            var uv = new Vector2[mesh.vertexCount];
            var normals = new Vector3[mesh.vertexCount];
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++) {
                var world = filter.transform.TransformPoint(verts[i]);
                // The source PBR atlas contains eight tiles per repeat: each tile is 55 cm.
                uv[i] = new Vector2(world.x / 4.4f, world.z / 4.4f);
                normals[i] = filter.transform.InverseTransformDirection(Vector3.up);
            }
            mesh.uv = uv; mesh.normals = normals; mesh.RecalculateTangents();
            Undo.RecordObject(filter, "Assign tiled visual mesh");
            filter.sharedMesh = StoreMesh(mesh, filter.name + " tiled visual");
            Assign(filter.GetComponent<MeshRenderer>(), paving);
            var routeName = filter.name.Substring(0, filter.name.Length - SurfaceSuffix.Length);
            var detail = filter.transform.parent.Find(routeName + " inlaid direction arrows");
            if (!detail) throw new InvalidOperationException("Original path inlay missing: " + routeName);
            var details = new DetailMesh(detail);
            foreach (var seg in segments.Where(s => s.surface == filter)) {
                foreach (float sign in new[] {-1f, 1f}) {
                    // Clip decorative borders at every joining route so they never bar a junction.
                    for (float d = 0; d < seg.length; d += .10f) {
                        float end = Mathf.Min(d + .10f, seg.length);
                        var mid = seg.a + (seg.b-seg.a).normalized * ((d+end)*.5f) + seg.side * (sign*.965f);
                        if (segments.Any(s => s != seg && Inside(mid, s, .99f))) continue;
                        var a = Vector3.Lerp(seg.a, seg.b, d/seg.length) + Vector3.up*.006f;
                        var b = Vector3.Lerp(seg.a, seg.b, end/seg.length) + Vector3.up*.006f;
                        details.Quad(a+seg.side*(sign*.92f), a+seg.side*(sign*1.01f), b+seg.side*(sign*1.01f), b+seg.side*(sign*.92f));
                    }
                }
                if (seg.length < 8) continue;
                var center = (seg.a + seg.b)*.5f + Vector3.up*.014f;
                if (segments.Any(s => s != seg && Inside(center, s, 1.15f))) continue;
                var forward = (seg.b-seg.a).normalized;
                // One small open chevron per straight run, in the same brass as the edge trim.
                var tip = center + forward*.14f;
                var left = center-forward*.10f+seg.side*.12f;
                var right = center-forward*.10f-seg.side*.12f;
                details.Bar(left, tip, .022f); details.Bar(tip, right, .022f); markers++;
            }
            var detailFilter = detail.GetComponent<MeshFilter>();
            Undo.RecordObject(detailFilter, "Replace oversized route arrows");
            detailFilter.sharedMesh = StoreMesh(details.Build(routeName + " restrained inlay"), routeName + " restrained inlay");
            Assign(detail.GetComponent<MeshRenderer>(), inlay);
        }
        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Debug.Log("FestivalPathwayPolish: eight existing path surfaces tiled, junction edges clipped, " + markers + " small direction chevrons. 16 renderers / 2 shared materials; no added objects, colliders, lights or route changes. Scene marked dirty, not saved.");
        if (root.Find("Rounded Rangoli promenade walking surface")) FestivalRoundedRoutes.Apply();
    }

    static bool Inside(Vector3 point, Segment s, float halfWidth) {
        var direction = (s.b-s.a).normalized;
        float along = Vector3.Dot(point-s.a,direction);
        return along > -.025f && along < s.length+.025f && Mathf.Abs(Vector3.Dot(point-s.a,s.side)) < halfWidth;
    }
    static void EnsureFolder() { if (!AssetDatabase.IsValidFolder(Folder.TrimEnd('/'))) AssetDatabase.CreateFolder(Source.TrimEnd('/'), "PathPolish"); }
    static Material PavingMaterial() {
        const string templatePath = "Assets/DiwaliMaterialUpgrade/Materials/Tile_A551D1A044AFA3D3.mat";
        var template = AssetDatabase.LoadAssetAtPath<Material>(templatePath);
        if (!template) throw new InvalidOperationException("Existing tile PBR material is missing: " + templatePath);
        var mat = GetMaterial("Warm sandstone paving", template);
        mat.SetColor("_BaseColor", new Color(.78f,.59f,.40f,1));
        mat.SetColor("_Color", new Color(.78f,.59f,.40f,1));
        mat.SetFloat("_Metallic",0); mat.SetFloat("_Smoothness",.18f); mat.SetFloat("_BumpScale",.32f);
        mat.SetTextureScale("_BaseMap",Vector2.one); mat.SetTextureScale("_BumpMap",Vector2.one);
        mat.SetTexture("_MetallicGlossMap",null); mat.DisableKeyword("_METALLICSPECGLOSSMAP");
        mat.SetColor("_EmissionColor",Color.black); mat.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(mat); return mat;
    }
    static Material InlayMaterial() {
        string texturePath=Folder+"Burgundy brass edge.png";
        if (!AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)) {
            var texture = new Texture2D(64,8,TextureFormat.RGBA32,false);
            for(int y=0;y<8;y++) for(int x=0;x<64;x++) texture.SetPixel(x,y,x<9||x>54 ? new Color(.64f,.43f,.18f) : new Color(.25f,.075f,.10f));
            texture.Apply(); System.IO.File.WriteAllBytes(texturePath,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.wrapMode=TextureWrapMode.Clamp; importer.mipmapEnabled=true; importer.filterMode=FilterMode.Bilinear; importer.SaveAndReimport();
        }
        var mat=GetMaterial("Burgundy and aged brass inlay",null);
        mat.SetColor("_BaseColor",Color.white); mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        mat.SetFloat("_Metallic",.15f); mat.SetFloat("_Smoothness",.23f);
        mat.SetColor("_EmissionColor",Color.black); mat.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(mat); return mat;
    }
    static Material GetMaterial(string name, Material template) {
        string path=Folder+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat=template ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.name=name; AssetDatabase.CreateAsset(mat,path); }
        else Undo.RecordObject(mat,"Update pathway material");
        return mat;
    }
    static Mesh StoreMesh(Mesh mesh,string name) {
        string path=Folder+name+".asset"; var stored=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!stored) {AssetDatabase.CreateAsset(mesh,path);return mesh;}
        Undo.RecordObject(stored,"Refresh pathway visual mesh"); EditorUtility.CopySerialized(mesh,stored); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(stored);return stored;
    }
    static void Assign(MeshRenderer renderer,Material material) {
        Undo.RecordObject(renderer,"Assign shared pathway material");renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;EditorUtility.SetDirty(renderer);
    }
    sealed class DetailMesh {
        readonly Transform transform; readonly List<Vector3> vertices=new List<Vector3>(); readonly List<Vector2> uv=new List<Vector2>(); readonly List<int> triangles=new List<int>();
        public DetailMesh(Transform transform) {this.transform=transform;}
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool brass=false) {
            int n=vertices.Count;
            vertices.AddRange(new[]{transform.InverseTransformPoint(a),transform.InverseTransformPoint(b),transform.InverseTransformPoint(c),transform.InverseTransformPoint(d)});
            uv.AddRange(brass ? new[]{new Vector2(.03f,.5f),new Vector2(.03f,.5f),new Vector2(.03f,.5f),new Vector2(.03f,.5f)} : new[]{new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(0,.5f)});
            triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2,n,n+1,n+2,n,n+2,n+3});
        }
        public void Bar(Vector3 a,Vector3 b,float width) {var s=Vector3.Cross(Vector3.up,(b-a).normalized)*width*.5f;Quad(a-s,a+s,b+s,b-s,true);}
        public Mesh Build(string name) {var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);var normals=Enumerable.Repeat(transform.InverseTransformDirection(Vector3.up),vertices.Count).ToArray();mesh.normals=normals;mesh.RecalculateBounds();return mesh;}
    }
}
}
#endif
