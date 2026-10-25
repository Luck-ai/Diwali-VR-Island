using UnityEngine;

public class Spinner : MonoBehaviour
{
    public float speed =90f;
    public Vector3 rotationAxis = Vector3.up;
    public bool IsSpinning => spinning;
    bool spinning;
    public void ToggleSpin()
    {
        spinning=!spinning;
    }
    void Update()
    {
        if (spinning)
            transform.Rotate(rotationAxis.normalized, speed * Time.deltaTime, Space.Self);
    }
}
