using UnityEngine;
namespace DiwaliGameplay {
public sealed class DiyaBillboard : MonoBehaviour {
 void LateUpdate(){var camera=Camera.main;if(camera)transform.rotation=Quaternion.LookRotation(transform.position-camera.transform.position,camera.transform.up);}
}
}
