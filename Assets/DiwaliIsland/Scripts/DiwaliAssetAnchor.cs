using UnityEngine;

namespace DiwaliIsland
{
    public sealed class DiwaliAssetAnchor : MonoBehaviour
    {
        [TextArea] public string purpose;
        public Vector3 suggestedSize = Vector3.one;
        public Color guideColor = new Color(1f, 0.65f, 0.2f, 0.8f);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = guideColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.up * suggestedSize.y * 0.5f, suggestedSize);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.45f);
        }
    }
}
