using UnityEngine;

public class Spinner : MonoBehaviour
{
    public float speed =90f;
    bool spinning;
    public void ToggleSpin()
    {
        spinning=!spinning;
    }
    void Update()
    {
        if (spinning)
            transform.Rotate(0, speed * Time.deltaTime, 0);
    }
}
