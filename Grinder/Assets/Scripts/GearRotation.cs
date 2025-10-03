using UnityEngine;

public class GearRotate : MonoBehaviour
{
    [Tooltip("Degrees per second. Positive = CCW, Negative = CW")]
    public float degreesPerSecond = 180f;

    void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
