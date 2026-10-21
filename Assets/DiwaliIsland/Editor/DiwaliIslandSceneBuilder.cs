using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;

namespace DiwaliIsland.Editor
{
    public static class DiwaliIslandSceneBuilder
    {
        const string AssetRoot = "Assets/DiwaliIsland";
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static TeleportationProvider teleport;
        static TMP_FontAsset font;
        static int meshNumber;
        static Transform root, terrain, paths, venues, decor, signs, navigationRoot;
        static readonly Vector3[] Locations =
        {
            new Vector3(-16,0,4), new Vector3(-10,0,17),
            new Vector3(9,0,18), new Vector3(17,0,1), new Vector3(4,0,-25)
        };
        static readonly string[] Titles =
        {
            "RANGOLI PAVILION", "SWEET MEMORY SHOP", "LANTERN GARDEN",
            "DIYA COURTYARD", "FESTIVAL GAMES PIER"
        };
        static readonly string[] Hints =
        {
            "Grab the shapes. Complete the pattern.",
            "Remember the order. Serve the sweets.",
            "Place the routes. Rotate to connect. Light the garden.",
            "Find the hidden treasures. Bring them to the ledges.",
            "Basket toss. Ball target. Ring placement."
        };
        static readonly string[] Accents = { "Plum", "Terracotta", "Teal", "Gold", "Navy" };

        [MenuItem("Diwali Island/Build Environment In Working Scene")]
        public static void Build()
        {
            if (SceneManager.GetActiveScene().path != "Assets/Scenes/DiwaliIsland.unity")
                throw new InvalidOperationException("Open the DiwaliIsland working scene first.");
            if (GameObject.Find("Diwali Island") != null)
                throw new InvalidOperationException("Island already exists; this builder will not replace your edits.");
            Directory.CreateDirectory(AssetRoot + "/Materials");
            Directory.CreateDirectory(AssetRoot + "/Geometry");
            Materials.Clear();
            meshNumber = 0;
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular SDF.asset");
            if (font == null) font = TMP_Settings.defaultFontAsset;

            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                if (new[] { "Interactables", "UI", "Environment", "Teleport Area Setup", "Lighting" }.Contains(go.name))
                {
                    Undo.RecordObject(go, "Hide template environment in island copy");
                    go.SetActive(false);
                }

            var rig = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (rig == null) throw new InvalidOperationException("Existing XR Origin not found.");
            teleport = rig.GetComponentInChildren<TeleportationProvider>(true);
            if (teleport == null) throw new InvalidOperationException("Existing teleport provider not found.");
            if (UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>() == null)
                new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();

            root = Group("Diwali Island", null);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Diwali island environment");
            terrain = Group("01 Island and Sea", root);
            paths = Group("02 Plaza and Walkways", root);
            venues = Group("03 Five Venues", root);
            decor = Group("04 Festival Decorations", root);
            signs = Group("05 Wayfinding", root);
            navigationRoot = Group("06 XR Destinations and Menus", root);
            Palette();
            Landscape();
            Plaza();
            var destinations = new Transform[6];
            for (int i = 0; i < Locations.Length; i++)
            {
                var venue = Group((i + 1).ToString("00") + " " + Titles[i], venues);
                venue.position = Locations[i];
                venue.rotation = Quaternion.LookRotation(new Vector3(Locations[i].x, 0, Locations[i].z).normalized);
                if (i == 0) Rangoli(venue);
                if (i == 1) Sweets(venue);
                if (i == 2) LanternGarden(venue);
                if (i == 3) DiyaCourt(venue);
                if (i == 4) GamesPier(venue);

                var landing = Group("Arrival - " + Titles[i], navigationRoot);
                landing.position = venue.TransformPoint(new Vector3(0, 0.09f, -4.4f));
                landing.rotation = venue.rotation;
                destinations[i] = landing;
                Walkway(landing.position, i);
                var sign = Group("Venue Sign", venue);
                Box("Sign backing", sign, new Vector3(0,2.65f,-3.1f), new Vector3(5.4f,0.78f,0.16f), Accents[i]);
                Text(Titles[i], sign, new Vector3(0,2.68f,-3.20f), 0.26f, new Vector2(5.2f,0.6f), "Cream");
                Box("Activity instruction sign",venue,new Vector3(-3.3f,1.45f,-3.56f),new Vector3(2.1f,0.92f,0.1f),"Cream");
                Box("Instruction sign post",venue,new Vector3(-3.3f,0.52f,-3.56f),new Vector3(0.12f,1.04f,0.12f),"Wood");
                Text(Hints[i], venue, new Vector3(-3.3f,1.45f,-3.62f), 0.1583f, new Vector2(1.95f,0.74f), "Ink");
                Garland(venue, new Vector3(-2.9f,2.18f,-3.1f), new Vector3(2.9f,2.18f,-3.1f), 0.22f);
                Planter(venue, new Vector3(-3.35f,0,-2.8f));
                Planter(venue, new Vector3(3.35f,0,-2.8f));
            }
            destinations[5] = Group("Arrival - Festival Plaza", navigationRoot);
            destinations[5].position = new Vector3(-4.2f,0.095f,-3.7f);
            destinations[5].rotation = Quaternion.Euler(0,25,0);
            Welcome();
            Lights();
            IslandPipeline();
            var nav = navigationRoot.gameObject.AddComponent<DiwaliIslandNavigation>();
            nav.provider = teleport;
            nav.destinations = destinations;
            CreateTravelPanel(nav, signs, new Vector3(-5.7f,1.7f,-8.4f), Quaternion.Euler(0,18,0), false);
            var leftController = rig.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Left Controller");
            if (leftController != null)
            {
                var handMenu = CreateTravelPanel(nav, leftController, new Vector3(0.10f,0.16f,0.20f), Quaternion.Euler(35,0,0), true);
                handMenu.SetActive(false);
                nav.controllerMenu = handMenu;
            }
            Undo.RecordObject(rig.transform, "Position island XR start");
            rig.transform.SetPositionAndRotation(new Vector3(-7.2f,0.08f,-14.5f), Quaternion.Euler(0,22,0));
            PreviewCameras();
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                if (renderer.GetComponent<TextMeshPro>() == null)
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,
                        StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Selection.activeGameObject = root.gameObject;
            var view = SceneView.lastActiveSceneView;
            if (view != null)
                view.LookAt(new Vector3(0,1.5f,1), Quaternion.Euler(42,-24,0), 48f, false, true);
            Debug.Log("DIWALI_BUILD_COMPLETE: five venues, no Blender imports, placement anchors, XR teleport surfaces and travel menus.");
        }

        static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static void Palette()
        {
            Mat("Grass", "#476358"); Mat("Sand", "#BEA07B"); Mat("Rock", "#5D5367");
            Mat("RockLight", "#85747F"); Mat("Sea", "#245268");
            Mat("Teal", "#237A78"); Mat("TealRoof", "#315F67");
            Mat("Plum", "#7B335A"); Mat("Terracotta", "#BA6545");
            Mat("Wood", "#754939"); Mat("WoodLight", "#AC7653");
            Mat("Gold", "#E3AF50", 0.25f); Mat("Cream", "#F5DFB7");
            Mat("Ink", "#392A42"); Mat("Navy", "#304B69");
            Mat("Paving", "#90745E"); Mat("Orange", "#EE943C"); Mat("Leaves", "#284C46");
            Mat("Glow", "#FFE9A5", 0f, 1.9f);
            Mat("PetalPink", "#CF7276"); Mat("PetalMint", "#86B8A3");
        }

        static Material Mat(string name, string hex, float metal = 0f, float emission = 0f)
        {
            if (Materials.TryGetValue(name, out var old)) return old;
            ColorUtility.TryParseHtmlString(hex, out var color);
            var path = AssetRoot + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Metallic", metal);
                mat.SetFloat("_Smoothness", 0.24f);
                if (emission > 0f)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * emission);
                }
                AssetDatabase.CreateAsset(mat, path);
            }
            Materials[name] = mat;
            return mat;
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, string material, bool collision = false)
        {
            return Primitive(name, PrimitiveType.Cube, parent, position, size, material, collision);
        }

        static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 size, string material, bool collision = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Materials[material];
            if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            else go.isStatic = true;
            return go;
        }

        static GameObject MeshObject(string name, Transform parent, List<Vector3> vertices, List<int> triangles, string material, bool collision = false)
        {
            var mesh = new Mesh { name = name };
            var flatVertices = triangles.Select(i => vertices[i]).ToList();
            mesh.SetVertices(flatVertices);
            mesh.SetTriangles(Enumerable.Range(0, flatVertices.Count).ToArray(), 0);
            var uv = flatVertices.Select(v => new Vector2(v.x * 0.1f, v.z * 0.1f)).ToList();
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, AssetRoot + "/Geometry/" + (++meshNumber).ToString("000") + "-" + name.Replace(" ", "-") + ".asset");
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = Materials[material];
            if (collision) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        static void Walkable(GameObject go)
        {
            var area = go.AddComponent<TeleportationArea>();
            area.teleportationProvider = teleport;
            area.interactionLayers = new InteractionLayerMask { value = int.MinValue };
            area.matchOrientation = MatchOrientation.WorldSpaceUp;
        }

        static float Shore(float angle)
        {
            return 26.5f + Mathf.Sin(angle * 3f + 0.5f) * 1.25f + Mathf.Sin(angle * 7f) * 0.55f;
        }

        static void Landscape()
        {
            Primitive("Quiet sea - no collision", PrimitiveType.Plane, terrain,
                new Vector3(0,-1.12f,0), new Vector3(38,1,38), "Sea");
            var v = new List<Vector3> { Vector3.zero };
            var t = new List<int>();
            const int n = 72;
            for (int i=0;i<n;i++)
            {
                float a = i * Mathf.PI * 2f / n;
                float r = Shore(a);
                v.Add(new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));
            }
            for (int i=0;i<n;i++) { t.Add(0); t.Add((i+1)%n+1); t.Add(i+1); }
            Walkable(MeshObject("Island walkable sand surface",terrain,v,t,"Sand",true));
            var grassV = new List<Vector3> { new Vector3(0,0.018f,0) };
            for(int i=1;i<v.Count;i++) grassV.Add(v[i]*0.89f+Vector3.up*0.018f);
            Walkable(MeshObject("Low-poly grassy interior",terrain,grassV,new List<int>(t),"Grass",true));
            var sides = new List<Vector3>(); var faces = new List<int>();
            for(int i=0;i<n;i++)
            {
                Vector3 a=v[i+1], b=v[(i+1)%n+1];
                int q=sides.Count;
                sides.Add(a); sides.Add(b); sides.Add(a+Vector3.down*2.7f); sides.Add(b+Vector3.down*2.7f);
                faces.AddRange(new[]{q,q+1,q+2,q+1,q+3,q+2});
            }
            MeshObject("Faceted shoreline cliff",terrain,sides,faces,"Rock",true);
            for(int i=0;i<18;i++)
            {
                float a=i*Mathf.PI*2/18;
                Vector3 p=new Vector3(Mathf.Cos(a)*Shore(a),-0.72f,Mathf.Sin(a)*Shore(a));
                var rock=Primitive("Shore boulder "+i,PrimitiveType.Sphere,terrain,p,new Vector3(3.8f,1.7f,2.9f),i%2==0?"Rock":"RockLight");
                rock.transform.localRotation=Quaternion.Euler(0,i*37,14);
            }
            for(int i=0;i<5;i++)
            {
                float a=(i+1)*1.22f;
                var island=Primitive("Distant islet "+i,PrimitiveType.Sphere,terrain,
                    new Vector3(Mathf.Cos(a)*105,-2.2f,Mathf.Sin(a)*105),new Vector3(16+i*3,6,13+i*2),"Rock");
            }
            foreach(var p in new[]{new Vector3(-20,0,-9),new Vector3(-20,0,15),new Vector3(18,0,17),new Vector3(20,0,-9),new Vector3(-2,0,24)})
                Palm(p);
        }

        static void Ring(string name, Transform parent, Vector3 center, float inside, float outside, string material)
        {
            const int n=64; var v=new List<Vector3>(); var t=new List<int>();
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n; int q=v.Count;
                v.Add(center+new Vector3(Mathf.Cos(a)*inside,0,Mathf.Sin(a)*inside));
                v.Add(center+new Vector3(Mathf.Cos(a)*outside,0,Mathf.Sin(a)*outside));
                v.Add(center+new Vector3(Mathf.Cos(b)*inside,0,Mathf.Sin(b)*inside));
                v.Add(center+new Vector3(Mathf.Cos(b)*outside,0,Mathf.Sin(b)*outside));
                t.AddRange(new[]{q,q+2,q+1,q+1,q+2,q+3});
            }
            MeshObject(name,parent,v,t,material);
        }

        static void Plaza()
        {
            var floor=Primitive("Festival plaza floor",PrimitiveType.Cylinder,paths,
                new Vector3(0,0.045f,0),new Vector3(12,0.045f,12),"Paving",true);
            Walkable(floor);
            Ring("Plaza gold edging",paths,new Vector3(0,0.095f,0),5.8f,6f,"Gold");
            Ring("Mandala outer border",paths,new Vector3(0,0.101f,0),3.5f,3.65f,"Cream");
            Ring("Mandala inner border",paths,new Vector3(0,0.105f,0),0.8f,1.0f,"Gold");
            for(int i=0;i<12;i++)
            {
                var p=Group("Plaza petal "+i,paths);
                p.localRotation=Quaternion.Euler(0,i*30,0);
                var v=new List<Vector3>{new Vector3(0,0.108f,0.9f),new Vector3(-0.65f,0.108f,2),new Vector3(0,0.108f,3.3f),new Vector3(0.65f,0.108f,2)};
                MeshObject("Mandala petal "+i,p,v,new List<int>{0,1,2,0,2,3},i%2==0?"PetalPink":"PetalMint");
            }
            Primitive("Mandala center",PrimitiveType.Cylinder,paths,new Vector3(0,0.11f,0),new Vector3(1.3f,0.008f,1.3f),"Gold");
            for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI*2/10;
                var lamp=Group("Plaza diya "+i,decor);
                lamp.position=new Vector3(Mathf.Cos(a)*6.15f,0.07f,Mathf.Sin(a)*6.15f);
                DecorDiya(lamp,Vector3.zero);
            }
        }

        static void Walkway(Vector3 end,int number)
        {
            Vector3 d=new Vector3(end.x,0,end.z).normalized;
            Vector3 start=d*5.8f, side=Vector3.Cross(Vector3.up,d);
            Vector3 control=(start+end)*0.5f+side*(number%2==0?1.3f:-1.3f);
            var parent=Group("Path "+(number+1)+" to "+Titles[number],paths);
            const int n=16;
            for(int i=0;i<n;i++)
            {
                float u=i/(float)n,w=(i+1)/(float)n;
                Vector3 a=(1-u)*(1-u)*start+2*(1-u)*u*control+u*u*end;
                Vector3 b=(1-w)*(1-w)*start+2*(1-w)*w*control+w*w*end;
                var slab=Box("Paved segment "+i,parent,(a+b)*0.5f+Vector3.up*0.042f,
                    new Vector3(2.65f,0.035f,Vector3.Distance(a,b)+0.07f),i%3==0?"Sand":"Paving",true);
                slab.transform.rotation=Quaternion.LookRotation(b-a);
                Walkable(slab);
                if(i%4==2)
                {
                    Vector3 normal=Vector3.Cross(Vector3.up,(b-a).normalized);
                    DecorDiya(parent,(a+b)*0.5f+normal*1.55f+Vector3.up*0.09f);
                    DecorDiya(parent,(a+b)*0.5f-normal*1.55f+Vector3.up*0.09f);
                }
            }
        }

        static void Floor(Transform p,float width,float depth,string mat)
        {
            var floor=Box("Venue walkable floor",p,new Vector3(0,0.025f,0),new Vector3(width,0.05f,depth),mat,true);
            Walkable(floor);
            Box("Front threshold gold strip",p,new Vector3(0,0.058f,-depth*0.5f+0.08f),new Vector3(width,0.015f,0.16f),"Gold");
        }

        static void Post(Transform p,Vector3 pos,float height=2.9f)
        {
            Box("Timber column",p,pos+Vector3.up*height*0.5f,new Vector3(0.24f,height,0.24f),"Wood",true);
            Box("Column foot",p,pos+Vector3.up*0.16f,new Vector3(0.4f,0.32f,0.4f),"Gold");
            Box("Column capital",p,pos+Vector3.up*(height-0.1f),new Vector3(0.42f,0.20f,0.42f),"Gold");
        }

        static void Table(Transform p,Vector3 pos,float width,float depth,string mat)
        {
            Box("Activity table top",p,pos+Vector3.up*0.91f,new Vector3(width,0.12f,depth),mat,true);
            foreach(float x in new[]{-1f,1f}) foreach(float z in new[]{-1f,1f})
                Box("Table leg",p,pos+new Vector3(x*(width*0.5f-0.16f),0.43f,z*(depth*0.5f-0.16f)),
                    new Vector3(0.16f,0.86f,0.16f),"Wood",true);
        }

        static void Anchor(Transform p,string name,Vector3 pos,Vector3 size,string purpose)
        {
            var a=Group("PLACE ASSET - "+name,p);
            a.localPosition=pos;
            var marker=a.gameObject.AddComponent<DiwaliAssetAnchor>();
            marker.purpose=purpose; marker.suggestedSize=size;
        }

        static void Rangoli(Transform p)
        {
            Floor(p,7.4f,6.8f,"WoodLight");
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI*2/8;
                Post(p,new Vector3(Mathf.Cos(a)*3.45f,0,Mathf.Sin(a)*3f));
            }
            HipRoof(p,3.95f,2.95f,1.3f,8,"TealRoof");
            Table(p,new Vector3(0,0,0.7f),2.9f,2.25f,"Cream");
            Table(p,new Vector3(-2.2f,0,-0.9f),1.0f,1.4f,"Plum");
            Anchor(p,"Pattern Board",new Vector3(0,0.98f,0.7f),new Vector3(2.5f,0.18f,2),"Hexagonal board and its scripted sliding cover.");
            Anchor(p,"Piece Holder",new Vector3(-2.2f,0.98f,-0.9f),new Vector3(0.85f,0.6f,1.2f),"Rack for sun, petal, diamond, star and C border.");
            foreach(var n in new[]{"Sun","Petal","Diamond Tile","Star","C Border"})
                Anchor(p,n,new Vector3(-2.2f,1.02f,-0.9f),Vector3.one*0.35f,"Grabbable rangoli piece; replace with your model.");
            Anchor(p,"Cover Motion Pivot",new Vector3(0,1.02f,0.7f),new Vector3(2.4f,0.04f,2f),"Controller Reveal moves cover horizontally.");
            HangLamp(p,new Vector3(-2,2.6f,-2.5f));
            HangLamp(p,new Vector3(2,2.6f,-2.5f));
        }

        static void HipRoof(Transform p,float radius,float baseY,float height,int sides,string mat)
        {
            var v=new List<Vector3>(); var t=new List<int>();
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides; int q=v.Count;
                v.Add(new Vector3(Mathf.Cos(a)*radius,baseY,Mathf.Sin(a)*radius));
                v.Add(new Vector3(Mathf.Cos(b)*radius,baseY,Mathf.Sin(b)*radius));
                v.Add(new Vector3(0,baseY+height,0));
                t.AddRange(new[]{q,q+2,q+1,q+1,q+2,q});
                Beam("Gold eave",p,v[q],v[q+1],0.13f,"Gold");
            }
            MeshObject("Hip canopy",p,v,t,mat);
            Primitive("Roof finial",PrimitiveType.Sphere,p,new Vector3(0,baseY+height+0.08f,0),Vector3.one*0.25f,"Gold");
        }

        static void Sweets(Transform p)
        {
            Floor(p,7.6f,6.6f,"Sand");
            Box("Rear wall",p,new Vector3(0,1.4f,3.2f),new Vector3(7.6f,2.8f,0.18f),"Cream",true);
            Box("West wall",p,new Vector3(-3.7f,1.4f,0),new Vector3(0.18f,2.8f,6.6f),"Terracotta",true);
            Box("East rear wall",p,new Vector3(3.7f,1.4f,1.7f),new Vector3(0.18f,2.8f,3.2f),"Terracotta",true);
            Box("Side doorway lintel",p,new Vector3(3.7f,2.5f,-1.7f),new Vector3(0.18f,0.6f,3.4f),"Terracotta",true);
            Post(p,new Vector3(-3.6f,0,-3.1f)); Post(p,new Vector3(3.6f,0,-3.1f));
            GableRoof(p,8.3f,7.3f,2.95f,1.25f,"Terracotta");
            Box("Shop fascia",p,new Vector3(0,2.42f,-3.19f),new Vector3(7.5f,0.22f,0.20f),"Teal");
            Box("Serving counter",p,new Vector3(0,0.51f,-1.35f),new Vector3(5.8f,1.02f,1.25f),"Teal",true);
            Box("Counter top",p,new Vector3(0,1.08f,-1.35f),new Vector3(6f,0.12f,1.4f),"WoodLight",true);
            Box("Counter gold edging",p,new Vector3(0,0.98f,-2.04f),new Vector3(6f,0.08f,0.06f),"Gold");
            for(int i=0;i<3;i++)
                Box("Rear display shelf",p,new Vector3(0,0.7f+i*0.6f,3.02f),new Vector3(5.2f,0.10f,0.5f),"WoodLight",true);
            Anchor(p,"Hinged Sweet Box",new Vector3(-1.7f,1.15f,-1.3f),new Vector3(1.8f,0.55f,0.75f),"Three-compartment source box; flat lid opens from Start.");
            Anchor(p,"Serving Tray",new Vector3(0.2f,1.15f,-1.3f),new Vector3(1.2f,0.1f,0.6f),"Track sweet type IDs in release order.");
            Anchor(p,"Cashier Till",new Vector3(2f,1.15f,-1.25f),new Vector3(1.05f,0.6f,0.75f),"Tray dock, sliding drawer and coin recess.");
            Anchor(p,"Controller Order UI",new Vector3(0,1.65f,-2.3f),new Vector3(0.7f,0.45f,0.02f),"Order preview is VR UI, not a picture-board model.");
            HangLamp(p,new Vector3(-2.6f,2.6f,-2.6f)); HangLamp(p,new Vector3(2.6f,2.6f,-2.6f));
        }

        static void GableRoof(Transform p,float width,float depth,float y,float height,string mat)
        {
            var v=new List<Vector3>{
                new Vector3(-width/2,y,-depth/2),new Vector3(-width/2,y,depth/2),
                new Vector3(0,y+height,-depth/2),new Vector3(0,y+height,depth/2),
                new Vector3(width/2,y,-depth/2),new Vector3(width/2,y,depth/2)};
            var t=new List<int>{0,2,1,1,2,3,2,4,3,3,4,5,1,2,0,3,2,1,3,4,2,5,4,3};
            MeshObject("Gabled roof",p,v,t,mat);
            Beam("Gold ridge",p,v[2],v[3],0.13f,"Gold");
            Beam("Front roof west trim",p,v[0],v[2],0.13f,"Gold");
            Beam("Front roof east trim",p,v[2],v[4],0.13f,"Gold");
            Beam("Back roof west trim",p,v[1],v[3],0.13f,"Gold");
            Beam("Back roof east trim",p,v[3],v[5],0.13f,"Gold");
        }

        static void LanternGarden(Transform p)
        {
            Floor(p,8.0f,7.5f,"Paving");
            Box("West low wall",p,new Vector3(-3.85f,0.45f,0.8f),new Vector3(0.2f,0.9f,5.9f),"Terracotta",true);
            Box("East low wall",p,new Vector3(3.85f,0.45f,0.8f),new Vector3(0.2f,0.9f,5.9f),"Terracotta",true);
            Box("Back wall",p,new Vector3(0,1.15f,3.6f),new Vector3(8f,2.3f,0.18f),"Wood",true);
            for(int i=0;i<4;i++) { Post(p,new Vector3(-3.75f,0,-3.3f+i*2.2f)); Post(p,new Vector3(3.75f,0,-3.3f+i*2.2f)); }
            var v=new List<Vector3>();var t=new List<int>(); const int n=16;
            for(int i=0;i<n;i++)
            {
                float a=i*Mathf.PI/n,b=(i+1)*Mathf.PI/n; int q=v.Count;
                Vector3 a1=new Vector3(Mathf.Cos(a)*4.1f,2.85f+Mathf.Sin(a)*1.4f,-3.9f);
                Vector3 b1=new Vector3(Mathf.Cos(b)*4.1f,2.85f+Mathf.Sin(b)*1.4f,-3.9f);
                v.Add(a1);v.Add(b1);v.Add(a1+Vector3.forward*7.8f);v.Add(b1+Vector3.forward*7.8f);
                t.AddRange(new[]{q,q+2,q+1,q+1,q+2,q+3,q+1,q+2,q,q+3,q+2,q+1});
            }
            MeshObject("Arched garden canopy",p,v,t,"TealRoof");
            for(int rib=0;rib<5;rib++)
            {
                for(int i=0;i<n;i++)
                {
                    float a=i*Mathf.PI/n,b=(i+1)*Mathf.PI/n;
                    Beam("Arched roof rib",p,
                        new Vector3(Mathf.Cos(a)*4.06f,2.81f+Mathf.Sin(a)*1.37f,-3.9f+rib*1.95f),
                        new Vector3(Mathf.Cos(b)*4.06f,2.81f+Mathf.Sin(b)*1.37f,-3.9f+rib*1.95f),0.11f,"Gold");
                }
            }
            Table(p,new Vector3(0.6f,0,-1f),3.2f,1.6f,"WoodLight");
            Table(p,new Vector3(-2.7f,0,-0.5f),1.0f,1.4f,"Teal");
            foreach(var x in new[]{-2.7f,0f,2.7f})
                Box("Fixture plinth",p,new Vector3(x,0.36f,2.65f),new Vector3(1.3f,0.72f,1.3f),"Terracotta",true);
            Anchor(p,"Routing Console Base",new Vector3(0.6f,0.98f,-1),new Vector3(2.9f,0.5f,1.3f),"Simple base, three wells, crystal socket. No buttons on object.");
            Anchor(p,"Route Piece Holder",new Vector3(-2.7f,0.98f,-0.5f),new Vector3(0.8f,0.9f,1.1f),"Stepped holder replaces cable harness.");
            Anchor(p,"Power Box",new Vector3(-2.8f,0.08f,-2.15f),new Vector3(0.7f,0.6f,0.6f),"Curved hinged lid; contains crystal.");
            Anchor(p,"Hanging Lantern",new Vector3(-2.7f,1.7f,2.65f),new Vector3(0.55f,1f,0.55f),"First route output.");
            Anchor(p,"Lotus Light",new Vector3(0,0.74f,2.65f),new Vector3(1.2f,0.6f,1.2f),"Second route output.");
            Anchor(p,"Rotating Beacon",new Vector3(2.7f,0.74f,2.65f),new Vector3(0.8f,1f,0.65f),"Third route output; rotate housing on successful Light.");
            Anchor(p,"Elbow Fork Straight Pieces",new Vector3(-2.7f,1.02f,-0.5f),new Vector3(0.6f,0.65f,1f),"Three independent grabbable models. Controller face button rotates selected inserted piece.");
            HangLamp(p,new Vector3(-3.25f,2.5f,-2.6f));HangLamp(p,new Vector3(3.25f,2.5f,-2.6f));
        }

        static void DiyaCourt(Transform p)
        {
            Floor(p,8f,7.0f,"Cream");
            const int n=12;
            for(int i=0;i<n;i++)
            {
                float a=-Mathf.PI/2+i*Mathf.PI/n,b=-Mathf.PI/2+(i+1)*Mathf.PI/n;
                Vector3 pa=new Vector3(Mathf.Sin(a)*3.7f,1.15f,Mathf.Cos(a)*3.1f);
                Vector3 pb=new Vector3(Mathf.Sin(b)*3.7f,1.15f,Mathf.Cos(b)*3.1f);
                var wall=Box("Alcove curved wall segment",p,(pa+pb)*0.5f,new Vector3(Vector3.Distance(pa,pb)+0.05f,2.3f,0.24f),"Sand",true);
                wall.transform.localRotation=Quaternion.LookRotation(Vector3.Cross(pb-pa,Vector3.up));
                Beam("Alcove gold coping",p,pa+Vector3.up*1.16f,pb+Vector3.up*1.16f,0.11f,"Gold");
            }
            Post(p,new Vector3(-3.3f,0,-2.3f));Post(p,new Vector3(3.3f,0,-2.3f));
            var v=new List<Vector3>();var t=new List<int>();
            const int rings=6,steps=24;
            for(int j=0;j<rings;j++)
            {
                float a=j*Mathf.PI*0.5f/rings,b=(j+1)*Mathf.PI*0.5f/rings;
                for(int i=0;i<steps;i++)
                {
                    float c=i*Mathf.PI*2/steps,d=(i+1)*Mathf.PI*2/steps;int q=v.Count;
                    v.Add(Dome(a,c));v.Add(Dome(a,d));v.Add(Dome(b,c));v.Add(Dome(b,d));
                    t.AddRange(new[]{q,q+2,q+1,q+1,q+2,q+3,q+1,q+2,q,q+3,q+2,q+1});
                }
            }
            MeshObject("Domed alcove roof",p,v,t,"Cream");
            Primitive("Dome finial",PrimitiveType.Sphere,p,new Vector3(0,4.45f,0),Vector3.one*0.28f,"Gold");
            foreach(var x in new[]{-2.2f,0f,2.2f})
                Box("Treasure display ledge",p,new Vector3(x,1.0f,2.38f),new Vector3(1.3f,0.13f,0.65f),"WoodLight",true);
            Anchor(p,"Sliding Lattice Screen",new Vector3(-2.2f,0.2f,2.45f),new Vector3(1.5f,1.8f,0.15f),"Controller Open Screen moves it sideways.");
            Anchor(p,"Hidden Diya",new Vector3(-2.2f,1.08f,2.38f),new Vector3(0.4f,0.2f,0.4f),"Treasure behind lattice screen.");
            Anchor(p,"Plant Pot",new Vector3(2.5f,0.05f,-0.25f),new Vector3(0.75f,1.1f,0.75f),"Lift whole plant to reveal key.");
            Anchor(p,"Chest and Key",new Vector3(-2.5f,0.08f,-0.4f),new Vector3(0.9f,0.65f,0.7f),"Key unlocks scripted chest lid. Feather inside.");
            Anchor(p,"Lower Drawer",new Vector3(0,0.25f,2.48f),new Vector3(1.1f,0.45f,0.65f),"Controller Open Drawer reveals ribbon scroll.");
            Anchor(p,"Scroll Display",new Vector3(2.2f,1.08f,2.38f),new Vector3(0.6f,0.25f,0.45f),"Final recovered scroll display.");
            HangLamp(p,new Vector3(-2.8f,2.5f,-2f));HangLamp(p,new Vector3(2.8f,2.5f,-2f));
        }

        static Vector3 Dome(float elevation,float around)
        {
            return new Vector3(Mathf.Cos(around)*Mathf.Cos(elevation)*4.1f,
                2.85f+Mathf.Sin(elevation)*1.5f,Mathf.Sin(around)*Mathf.Cos(elevation)*3.8f);
        }

        static void GamesPier(Transform p)
        {
            Floor(p,7.8f,7.0f,"WoodLight");
            for(int i=0;i<24;i++)
                Box("Deck plank",p,new Vector3(0,0.055f,-3.45f+i*0.30f),new Vector3(7.7f,0.025f,0.27f),i%3==0?"Wood":"WoodLight");
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<4;i++)
                {
                    Post(p,new Vector3(side*3.65f,0,-2.9f+i*1.95f));
                    Box("Pier support pile",p,new Vector3(side*3.3f,-1.3f,-2.8f+i*1.9f),new Vector3(0.28f,2.6f,0.28f),"Wood");
                }
                Box("Side deck rail",p,new Vector3(side*3.75f,0.9f,0),new Vector3(0.15f,0.15f,7f),"Wood",true);
            }
            Box("Rear half wall",p,new Vector3(0,0.72f,3.4f),new Vector3(7.7f,1.44f,0.18f),"Wood",true);
            var roof=Box("Single-slope arcade roof",p,new Vector3(0,3.25f,0),new Vector3(8.5f,0.22f,7.8f),"TealRoof");
            roof.transform.localRotation=Quaternion.Euler(-7,0,0);
            Box("Open-front prize counter",p,new Vector3(0,0.49f,-1.55f),new Vector3(6.0f,0.98f,0.85f),"Plum",true);
            Box("Counter gold edge",p,new Vector3(0,1.03f,-1.55f),new Vector3(6.1f,0.09f,1f),"Gold");
            for(int i=0;i<3;i++) Box("Built-in prop shelf",p,new Vector3(-2.4f,0.6f+i*0.48f,2.9f),new Vector3(1.55f,0.08f,0.5f),"WoodLight",true);
            Anchor(p,"Target Stand",new Vector3(0,0.08f,1.9f),new Vector3(1.3f,1.6f,0.6f),"Fixed stand and rails, modeled separately from board.");
            Anchor(p,"Bullseye Board Motion",new Vector3(0,1.25f,1.9f),new Vector3(1.15f,1.2f,0.12f),"Only board rises on Start, then stays fixed.");
            Anchor(p,"Basket",new Vector3(2.3f,0.08f,1.8f),new Vector3(1.1f,0.6f,0.8f),"Large catch zone for festival shuttle.");
            Anchor(p,"Peg",new Vector3(1.6f,1.10f,-1.55f),new Vector3(0.5f,0.7f,0.5f),"Generous release-to-snap ring zone.");
            Anchor(p,"Ball Ring Shuttle Source",new Vector3(-2.4f,1.10f,2.8f),new Vector3(1.3f,0.8f,0.45f),"Source props: paneled ball, grip ring, rigid four-fin shuttle.");
            Anchor(p,"Prize Drawer",new Vector3(-1.5f,0.55f,-1.9f),new Vector3(0.8f,0.4f,0.5f),"Claim slides integrated prize drawer.");
            Anchor(p,"Prize Token Preview",new Vector3(0,1.10f,-1.55f),new Vector3(0.5f,0.5f,0.12f),"Token rotates on controller Preview.");
            HangLamp(p,new Vector3(-3,2.6f,-2.8f));HangLamp(p,new Vector3(3,2.6f,-2.8f));
        }

        static void Beam(string name,Transform p,Vector3 a,Vector3 b,float width,string material)
        {
            var go=Box(name,p,(a+b)*0.5f,new Vector3(width,width,Vector3.Distance(a,b)),material);
            go.transform.localRotation=Quaternion.LookRotation(b-a);
        }

        static void Garland(Transform p,Vector3 a,Vector3 b,float sag)
        {
            Vector3 last=a;
            for(int i=1;i<=16;i++)
            {
                float u=i/16f;
                Vector3 pos=Vector3.Lerp(a,b,u)+Vector3.down*(Mathf.Sin(u*Mathf.PI)*sag);
                Beam("Marigold garland cord",p,last,pos,0.025f,"Gold");
                Primitive("Marigold bloom",PrimitiveType.Sphere,p,pos,new Vector3(0.16f,0.18f,0.16f),i%2==0?"Orange":"Gold");
                if(i%2==0)
                {
                    var v=new List<Vector3>{pos+new Vector3(-0.14f,0,0),pos+new Vector3(0.14f,0,0),pos+Vector3.down*0.3f};
                    MeshObject("Toran flag "+meshNumber,p,v,new List<int>{0,1,2,2,1,0},i%4==0?"Plum":"Teal");
                }
                last=pos;
            }
        }

        static void DecorDiya(Transform p,Vector3 pos)
        {
            Primitive("Festival oil-lamp bowl",PrimitiveType.Sphere,p,pos+Vector3.up*0.065f,new Vector3(0.34f,0.11f,0.27f),"Terracotta");
            Primitive("Warm lamp glow",PrimitiveType.Sphere,p,pos+Vector3.up*0.13f,new Vector3(0.13f,0.07f,0.13f),"Glow");
            Primitive("Lamp flame",PrimitiveType.Sphere,p,pos+new Vector3(0,0.23f,0),new Vector3(0.055f,0.16f,0.055f),"Glow");
        }

        static void HangLamp(Transform p,Vector3 pos)
        {
            Beam("Lantern hanger",p,pos,pos+Vector3.up*0.35f,0.025f,"Gold");
            Primitive("Decor lantern glow",PrimitiveType.Cylinder,p,pos-Vector3.up*0.17f,new Vector3(0.17f,0.18f,0.17f),"Glow");
            Primitive("Lantern top cap",PrimitiveType.Cylinder,p,pos+Vector3.up*0.03f,new Vector3(0.28f,0.04f,0.28f),"Gold");
            Primitive("Lantern bottom cap",PrimitiveType.Cylinder,p,pos-Vector3.up*0.37f,new Vector3(0.24f,0.035f,0.24f),"Gold");
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI/2;
                Beam("Lantern brass frame",p,pos+new Vector3(Mathf.Cos(a)*0.1f,0.02f,Mathf.Sin(a)*0.1f),
                    pos+new Vector3(Mathf.Cos(a)*0.1f,-0.36f,Mathf.Sin(a)*0.1f),0.025f,"Gold");
            }
        }

        static void Planter(Transform p,Vector3 pos)
        {
            Box("Built-in planter",p,pos+Vector3.up*0.22f,new Vector3(0.75f,0.44f,0.75f),"Terracotta",true);
            for(int i=0;i<4;i++)
                Primitive("Planter foliage",PrimitiveType.Sphere,p,pos+new Vector3((i%2-0.5f)*0.32f,0.59f,(i/2-0.5f)*0.32f),new Vector3(0.46f,0.55f,0.46f),"Leaves");
        }

        static void Palm(Vector3 pos)
        {
            var p=Group("Coastal palm",decor);p.position=pos;
            for(int i=0;i<5;i++)
                Beam("Palm trunk segment",p,new Vector3(i*0.09f,i*0.85f,0),new Vector3((i+1)*0.09f,(i+1)*0.85f,0),0.20f,"WoodLight");
            for(int i=0;i<7;i++)
            {
                float a=i*Mathf.PI*2/7;
                Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Vector3 side=Vector3.Cross(Vector3.up,dir);
                Vector3 center=new Vector3(0.45f,4.25f,0);
                var v=new List<Vector3>{center,center+dir*1.1f+side*0.38f+Vector3.up*0.3f,center+dir*2.35f-Vector3.up*0.6f,center+dir*1.1f-side*0.38f+Vector3.up*0.3f};
                MeshObject("Palm frond "+meshNumber,p,v,new List<int>{0,2,1,0,3,2,1,2,0,2,3,0},"Leaves");
            }
        }

        static void Welcome()
        {
            var p=Group("Welcome Toran Arch",signs);
            p.position=new Vector3(-7.2f,0.07f,-15.7f);
            p.localRotation=Quaternion.Euler(0,22,0);
            Post(p,new Vector3(-2,0,0),3.44f);Post(p,new Vector3(2,0,0),3.44f);
            Box("Welcome arch beam",p,new Vector3(0,3.48f,0),new Vector3(4.8f,0.35f,0.3f),"Wood");
            Box("Island title backing",p,new Vector3(0,2.99f,-0.18f),new Vector3(4.4f,0.96f,0.12f),"Plum");
            Text("DIWALI ISLAND",p,new Vector3(0,3.15f,-0.25f),0.3167f,new Vector2(4.2f,0.50f),"Cream");
            Garland(p,new Vector3(-2.2f,2.42f,0),new Vector3(2.2f,2.42f,0),0.35f);
            Text("Five places. Five ways to celebrate.",p,new Vector3(0,2.70f,-0.25f),0.1375f,new Vector2(4.2f,0.30f),"Cream");
            Text("Point to the paths to teleport.\nLeft controller secondary button opens the travel menu.",
                signs,new Vector3(-5.7f,0.40f,-8.55f),0.125f,new Vector2(3f,0.45f),"Cream");
            var controls=signs.GetComponentsInChildren<TextMeshPro>().First(t=>t.text.StartsWith("Point to"));
            controls.transform.localRotation=Quaternion.Euler(0,18,0);
            var backing=Box("Controller instructions backing",signs,controls.transform.localPosition+controls.transform.forward*0.06f,new Vector3(3.1f,0.55f,0.10f),"Navy");
            backing.transform.localRotation=controls.transform.localRotation;
        }

        static void Text(string text,Transform p,Vector3 position,float size,Vector2 bounds,string material)
        {
            var go=new GameObject(text,typeof(TextMeshPro));
            go.transform.SetParent(p,false);go.transform.localPosition=position;
            var tmp=go.GetComponent<TextMeshPro>();tmp.font=font;
            tmp.text=text;tmp.fontSize=size*12f;tmp.alignment=TextAlignmentOptions.Center;
            tmp.enableAutoSizing=true;tmp.fontSizeMax=size*12f;tmp.fontSizeMin=size*8f;
            tmp.color=Materials[material].GetColor("_BaseColor");
            tmp.rectTransform.sizeDelta=bounds;
            tmp.textWrappingMode=TextWrappingModes.Normal;
        }

        static GameObject CreateTravelPanel(DiwaliIslandNavigation nav,Transform parent,Vector3 position,Quaternion rotation,bool handheld)
        {
            var go=new GameObject(handheld?"Controller Travel Menu":"Island Travel Directory",typeof(RectTransform),typeof(Canvas),typeof(TrackedDeviceGraphicRaycaster));
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=rotation;
            go.transform.localScale=Vector3.one*(handheld?0.00075f:0.0032f);
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            canvas.worldCamera=UnityEngine.Object.FindFirstObjectByType<XROrigin>().Camera;
            var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(980,640);
            var bg=UIRect("Background",go.transform,new Vector2(0,0),new Vector2(980,640));
            bg.AddComponent<Image>().color=new Color(0.13f,0.17f,0.21f,0.98f);
            UIString("EXPLORE THE ISLAND",go.transform,new Vector2(0,265),new Vector2(920,70),43,new Color(1f,0.83f,0.55f));
            UIString(handheld?"Point and select a destination":"Choose a venue to teleport",go.transform,new Vector2(0,215),new Vector2(920,45),26,Color.white);
            for(int i=0;i<6;i++)
            {
                var buttonGO=UIRect("Visit "+i,go.transform,new Vector2(0,155-i*72),new Vector2(885,62));
                var img=buttonGO.AddComponent<Image>();
                img.color=i<5?Materials[Accents[i]].GetColor("_BaseColor"):new Color(0.34f,0.39f,0.40f);
                var button=buttonGO.AddComponent<Button>();
                button.targetGraphic=img;
                UnityEventTools.AddIntPersistentListener(button.onClick,nav.VisitVenue,i);
                UIString(i<5?Titles[i]:"RETURN TO PLAZA",buttonGO.transform,Vector2.zero,new Vector2(865,55),29,Color.white);
            }
            return go;
        }

        static GameObject UIRect(string name,Transform p,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(p,false);
            var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(0.5f,0.5f);
            rt.anchoredPosition=position;rt.sizeDelta=size;
            return go;
        }

        static void UIString(string text,Transform p,Vector2 position,Vector2 size,float fontSize,Color color)
        {
            var go=UIRect(text,p,position,size);
            var tmp=go.AddComponent<TextMeshProUGUI>();tmp.font=font;tmp.text=text;
            tmp.fontSize=fontSize;tmp.alignment=TextAlignmentOptions.Center;tmp.color=color;
            tmp.raycastTarget=false;tmp.textWrappingMode=TextWrappingModes.Normal;
        }

        static void Lights()
        {
            var p=Group("07 Dusk Lighting",root);
            var sun=new GameObject("Warm dusk sun",typeof(Light));sun.transform.SetParent(p,false);
            sun.transform.rotation=Quaternion.Euler(35,-38,0);
            var l=sun.GetComponent<Light>();l.type=LightType.Directional;l.color=new Color(1f,0.79f,0.60f);
            l.intensity=1.65f;l.shadows=LightShadows.Hard;l.shadowStrength=0.65f;l.shadowBias=0.07f;
            RenderSettings.sun=l;
            var sky=new Material(Shader.Find("DiwaliIsland/DuskSky")) {name="Diwali Dusk Sky"};
            AssetDatabase.CreateAsset(sky,AssetRoot+"/Materials/DiwaliDuskSky.mat");
            RenderSettings.skybox=sky;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(0.51f,0.48f,0.61f);
            RenderSettings.ambientEquatorColor=new Color(0.38f,0.34f,0.40f);
            RenderSettings.ambientGroundColor=new Color(0.22f,0.24f,0.25f);
            RenderSettings.ambientIntensity=1f;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=new Color(0.39f,0.43f,0.51f);
            RenderSettings.fogStartDistance=60;RenderSettings.fogEndDistance=155;
            foreach(var pos in Locations)
            {
                var lamp=new GameObject("Venue warm fill",typeof(Light));lamp.transform.SetParent(p,false);
                lamp.transform.position=pos+Vector3.up*2.4f;
                var light=lamp.GetComponent<Light>();light.type=LightType.Point;
                light.color=new Color(1,0.67f,0.34f);light.intensity=1.3f;light.range=7;
                light.shadows=LightShadows.None;
            }
        }

        static void IslandPipeline()
        {
            const string path=AssetRoot+"/DiwaliIslandPipeline.asset";
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if(pipeline==null)
            {
                pipeline=UnityEngine.Object.Instantiate((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline);
                pipeline.name="Diwali Island URP";
                pipeline.shadowDistance=45;
                pipeline.mainLightShadowmapResolution=1024;
                AssetDatabase.CreateAsset(pipeline,path);
            }
            var rendering=root.gameObject.AddComponent<DiwaliIslandRendering>();
            rendering.enabled=false;
            rendering.islandPipeline=pipeline;
            rendering.enabled=true;
        }

        static void PreviewCameras()
        {
            var p=Group("08 Preview Cameras - Disabled During Gameplay",root);
            PreviewCamera("Island Overview",p,new Vector3(39,43,-51),new Vector3(0,0.8f,-1),39);
            PreviewCamera("Welcome View",p,new Vector3(-8.5f,1.7f,-19.5f),new Vector3(0,1.1f,4),64);
        }

        static void PreviewCamera(string name,Transform p,Vector3 pos,Vector3 target,float fov)
        {
            var go=new GameObject(name,typeof(Camera));go.transform.SetParent(p,false);
            go.transform.position=pos;go.transform.LookAt(target);
            var c=go.GetComponent<Camera>();c.enabled=false;c.fieldOfView=fov;
            c.nearClipPlane=0.1f;c.farClipPlane=220;c.clearFlags=CameraClearFlags.Skybox;
            c.GetUniversalAdditionalCameraData().allowXRRendering=false;
            c.allowHDR=true;
        }

        [MenuItem("Diwali Island/Save Overview And Welcome Previews")]
        public static void CapturePreviews()
        {
            var folder=Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,"Scene_Previews");
            Directory.CreateDirectory(folder);
            var previousPipeline=QualitySettings.renderPipeline;
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetRoot+"/DiwaliIslandPipeline.asset");
            float previousShadowDistance=pipeline.shadowDistance;
            try
            {
            QualitySettings.renderPipeline=pipeline;
            pipeline.shadowDistance=110;
            foreach(var name in new[]{"Island Overview","Welcome View"})
            {
                var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(c=>c.name==name);
                var render=new RenderTexture(1920,1080,24);
                var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
                try
                {
                    camera.targetTexture=render;camera.Render();RenderTexture.active=render;
                    var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                    texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
                    var path=Path.Combine(folder,name.Replace(" ","-")+"-2026-10-12.png");
                    File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                    Debug.Log("Saved scene preview "+path);
                }
                finally
                {
                    camera.targetTexture=oldTarget;RenderTexture.active=oldActive;
                    render.Release();UnityEngine.Object.DestroyImmediate(render);
                }
            }
            }
            finally
            {
                pipeline.shadowDistance=previousShadowDistance;
                QualitySettings.renderPipeline=previousPipeline;
            }
        }
    }
}

