#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiwaliGameplay {
public static class SweetCheckoutDisplayInstaller {
 const int ScreenSubmesh = 5;
 sealed class Face {
  public Vector3 normal, anchor, weightedCenter;
  public float area;
  public readonly List<Vector3> vertices = new List<Vector3>();
 }

 [MenuItem("Diwali/Gameplay/Fit Sweet checkout monitor")]
 public static void Apply() {
  if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first");
  var game = Object.FindFirstObjectByType<SweetGame>();
  if (!game) throw new System.InvalidOperationException("No SweetGame in the current scene");
  var venue = GameObjectUtility.FindExact("SweetShop - Approved Warm Palette") ?? GameObjectUtility.FindExact("SweetShop - Native PBR");
  var counter = venue ? GameObjectUtility.FindUnder(venue, "Cube.012") : null;
  var filter = counter ? counter.GetComponent<MeshFilter>() : null;
  var renderer = counter ? counter.GetComponent<MeshRenderer>() : null;
  if (!filter || !filter.sharedMesh || !renderer || renderer.sharedMaterials.Length <= ScreenSubmesh || filter.sharedMesh.subMeshCount <= ScreenSubmesh)
   throw new System.InvalidOperationException("Checkout counter screen submesh 5 is missing; main activity panel remains available");
  var material = renderer.sharedMaterials[ScreenSubmesh];
  if (!material)
   throw new System.InvalidOperationException("Counter material slot 5 has no atlas material");
  var points = ReadTriangles(filter.sharedMesh);
  if (!TryFace(counter, points, game.workspaceForward, out var center, out var normal, out var up, out var size)) {
   // The authored screen is a single face; the reversed counter uses its other side.
   if (!TryFace(counter, points, -game.workspaceForward, out center, out normal, out up, out size))
    throw new System.InvalidOperationException("No checkout screen plane found; main activity panel remains available");
   normal = -normal;
  }
  float front = float.NegativeInfinity;
  foreach (var point in points) front = Mathf.Max(front, Vector3.Dot(counter.TransformPoint(point), normal));
  center += normal * (front - Vector3.Dot(center, normal));
  var display = game.GetComponent<SweetCheckoutDisplay>();
  if (!display) display = Undo.AddComponent<SweetCheckoutDisplay>(game.gameObject);
  Undo.RecordObject(display, "Fit Sweet checkout monitor");
  display.screen = counter;
  display.localCenter = counter.InverseTransformPoint(center);
  display.localNormal = counter.InverseTransformDirection(normal).normalized;
  display.localUp = counter.InverseTransformDirection(up).normalized;
  var right = Vector3.Cross(normal, up).normalized;
  display.localSize = new Vector2(counter.InverseTransformVector(right * size.x).magnitude,
   counter.InverseTransformVector(up * size.y).magnitude);
  display.configured = true;
  EditorUtility.SetDirty(display);
  EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
  Debug.Log("Sweet checkout monitor fitted to authored submesh 5; face size " + size + ", center " + center + ", outward " + normal);
 }

 static List<Vector3> ReadTriangles(Mesh mesh) {
  var points = new List<Vector3>();
  using (var array = MeshUtility.AcquireReadOnlyMeshData(mesh)) {
   var data = array[0];
   var descriptor = data.GetSubMesh(ScreenSubmesh);
   if (descriptor.topology != MeshTopology.Triangles) return points;
   using (var vertices = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp)) {
    data.GetVertices(vertices);
    if (data.indexFormat == IndexFormat.UInt16) {
     var indices = data.GetIndexData<ushort>();
     for (int i = 0; i < descriptor.indexCount; i++) points.Add(vertices[indices[descriptor.indexStart + i] + descriptor.baseVertex]);
    } else {
     var indices = data.GetIndexData<uint>();
     for (int i = 0; i < descriptor.indexCount; i++) points.Add(vertices[(int)indices[descriptor.indexStart + i] + descriptor.baseVertex]);
    }
   }
  }
  return points;
 }

 static bool TryFace(Transform frame, List<Vector3> localPoints, Vector3 toward, out Vector3 center, out Vector3 normal, out Vector3 up, out Vector2 size) {
  center = normal = up = Vector3.zero; size = Vector2.zero;
  if (toward.sqrMagnitude < .001f) return false;
  toward.Normalize();
  var faces = new List<Face>();
  for (int i = 0; i + 2 < localPoints.Count; i += 3) {
   var a = frame.TransformPoint(localPoints[i]);
   var b = frame.TransformPoint(localPoints[i + 1]);
   var c = frame.TransformPoint(localPoints[i + 2]);
   var cross = Vector3.Cross(b - a, c - a);
   float area = cross.magnitude * .5f;
   if (area < .000001f) continue;
   var n = cross.normalized;
   if (Vector3.Dot(n, toward) < .25f) continue;
   Face face = null;
   foreach (var candidate in faces)
    if (Vector3.Dot(candidate.normal, n) > .995f && Mathf.Abs(Vector3.Dot(a - candidate.anchor, n)) < .001f) { face = candidate; break; }
   if (face == null) { face = new Face { normal = n, anchor = a }; faces.Add(face); }
   face.area += area;
   face.weightedCenter += (a + b + c) / 3 * area;
   face.vertices.Add(a); face.vertices.Add(b); face.vertices.Add(c);
  }
  Face best = null;
  foreach (var face in faces) if (best == null || face.area > best.area) best = face;
  if (best == null) return false;
  normal = best.normal;
  up = Vector3.ProjectOnPlane(Vector3.up, normal).normalized;
  if (up.sqrMagnitude < .1f) return false;
  var right = Vector3.Cross(normal, up).normalized;
  center = best.weightedCenter / best.area;
  var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
  var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
  foreach (var point in best.vertices) {
   var offset = point - center;
   var projected = new Vector2(Vector3.Dot(offset, right), Vector3.Dot(offset, up));
   min = Vector2.Min(min, projected); max = Vector2.Max(max, projected);
  }
  center += right * ((min.x + max.x) * .5f) + up * ((min.y + max.y) * .5f);
  size = max - min;
  return size.x > .03f && size.y > .03f;
 }
}
}
#endif
