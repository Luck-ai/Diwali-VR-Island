using UnityEngine;
namespace DiwaliGameplay {
public sealed class DiyaProgressivePath:MonoBehaviour {
 public Shader pathShader;DiyaGame owner;Renderer floor;Vector4[] visited=new Vector4[32];int count;MaterialPropertyBlock block;public int pathMaterialCount;
 void Start(){owner=GetComponent<DiyaGame>();var venue=GameObjectUtility.FindExact("Diya - Approved Warm Palette");var art=venue?GameObjectUtility.FindUnder(venue,"Plane.002"):null;if(!art)return;floor=art.GetComponent<Renderer>();var shader=pathShader?pathShader:Shader.Find("Diwali/Progressive Orange Path");if(!shader)return;var materials=floor.materials;if(materials.Length>1){materials[1]=new Material(shader);floor.materials=materials;pathMaterialCount=1;}block=new MaterialPropertyBlock();}
 void LateUpdate(){if(!owner||!floor||block==null)return;if(owner.Step<8){count=0;}else if(owner.Step<=9&&owner.diya.IsHeld){var camera=Camera.main;var p=camera?camera.transform.position:owner.diya.transform.position;if(count==0||Vector2.Distance(new Vector2(p.x,p.z),new Vector2(visited[count-1].x,visited[count-1].z))>.5f){if(count<visited.Length)visited[count++]=p;else {for(int i=1;i<count;i++)visited[i-1]=visited[i];visited[count-1]=p;}}}block.SetInt("_VisitedCount",count);block.SetVectorArray("_Visited",visited);floor.SetPropertyBlock(block);}
}
}
