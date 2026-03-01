using UnityEngine;

public class TimeScaleTester : MonoBehaviour
{
    // Public variable to control the game speed. 
    // 1 = normal speed, 0.5 = half speed, 0 = paused
    public float timeSpeed = 1.0f;

    void Update()
    {
        Time.timeScale = timeSpeed;
        // It's also recommended to adjust fixedDeltaTime for consistent physics
        Time.fixedDeltaTime = Time.timeScale * 0.02f; // Default fixedDeltaTime is 0.02f
    }
}
