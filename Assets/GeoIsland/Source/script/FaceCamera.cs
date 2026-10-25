using UnityEngine;

// Keeps a world-space sign turned toward the player's view so it is readable from anywhere.
public class FaceCamera : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (!cam) return;
        var away = transform.position - cam.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude > .0001f) transform.rotation = Quaternion.LookRotation(away);
    }
}
