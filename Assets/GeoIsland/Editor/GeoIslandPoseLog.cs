#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

// Prints the current pose of chosen Geo objects to the Console once per editor session,
// so it can be recorded before the object is changed.
[InitializeOnLoad]
public static class GeoIslandPoseLog
{
    static readonly string[] Names = { "Cube.105" };

    static GeoIslandPoseLog()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool("GeoIsland.PoseLogged", false)) return;
            if (Log()) SessionState.SetBool("GeoIsland.PoseLogged", true);
        };
    }

    [MenuItem("Tools/Geo Island/Log Saved Poses")]
    static void LogMenu() => Log();

    public static bool Log()
    {
        var root = GameObject.Find("Geo Island Integration");
        if (!root) return false;
        foreach (var name in Names)
        {
            var t = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);
            if (!t) { Debug.LogWarning("Geo pose: " + name + " not found."); continue; }
            Debug.Log($"Geo pose: {name} parent={t.parent.name} " +
                $"localPosition={t.localPosition.ToString("F5")} localEuler={t.localEulerAngles.ToString("F4")} localScale={t.localScale.ToString("F5")} " +
                $"worldPosition={t.position.ToString("F5")} worldEuler={t.eulerAngles.ToString("F4")} worldRotation={t.rotation.ToString("F6")}");
        }
        return true;
    }
}
#endif
