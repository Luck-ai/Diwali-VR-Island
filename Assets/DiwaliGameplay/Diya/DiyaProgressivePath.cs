using UnityEngine;

namespace DiwaliGameplay {
public sealed class DiyaProgressivePath : MonoBehaviour {
 public Shader pathShader;
 public int pathMaterialCount;
 DiyaGame owner;
 Renderer floor;
 readonly Vector4[] visited = new Vector4[32];
 int count;
 MaterialPropertyBlock block;
 Material pathMaterial;
 Material[] originalMaterials;

 void Start() {
  owner = GetComponent<DiyaGame>();
  var venue = GameObjectUtility.FindExact("Diya - Approved Warm Palette");
  var art = venue ? GameObjectUtility.FindUnder(venue, "Plane.002") : null;
  if (!art) return;
  floor = art.GetComponent<Renderer>();
  var shader = pathShader ? pathShader : Shader.Find("Diwali/Progressive Orange Path");
  if (!floor || !shader) return;
  originalMaterials = floor.sharedMaterials;
  if (originalMaterials.Length < 2 || !originalMaterials[1]) return;
  // Keep the authored atlas and UV transform when adding visited-path emission.
  pathMaterial = new Material(originalMaterials[1]) { name = "Diya authored path (progressive)" };
  pathMaterial.shader = shader;
  var materials = (Material[])originalMaterials.Clone();
  materials[1] = pathMaterial;
  floor.sharedMaterials = materials;
  pathMaterialCount = 1;
  block = new MaterialPropertyBlock();
 }

 void LateUpdate() {
  if (!owner || !floor || block == null) return;
  if (owner.Step < 8) count = 0;
  else if (owner.Step <= 9 && owner.diya && owner.diya.IsHeld) {
   var camera = Camera.main;
   var p = camera ? camera.transform.position : owner.diya.transform.position;
   if (count == 0 || Vector2.Distance(new Vector2(p.x, p.z), new Vector2(visited[count - 1].x, visited[count - 1].z)) > .5f) {
    if (count < visited.Length) visited[count++] = p;
    else {
     for (int i = 1; i < count; i++) visited[i - 1] = visited[i];
     visited[count - 1] = p;
    }
   }
  }
  floor.GetPropertyBlock(block, 1);
  block.SetInt("_VisitedCount", count);
  block.SetVectorArray("_Visited", visited);
  floor.SetPropertyBlock(block, 1);
 }

 void OnDestroy() {
  if (floor && originalMaterials != null) {
   floor.SetPropertyBlock(null, 1);
   floor.sharedMaterials = originalMaterials;
  }
  if (pathMaterial) Destroy(pathMaterial);
 }
}
}
