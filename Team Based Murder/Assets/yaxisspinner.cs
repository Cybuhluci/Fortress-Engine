using UnityEngine;

public class yaxisspinner : MonoBehaviour
{ 
    public float rotationSpeed = 50f; // Default speed, adjust in Inspector

    // Update is called once per frame
    void Update()
    {
        // Rotate the GameObject around its local Y-axis
        // Time.deltaTime ensures the rotation speed is frame-rate independent
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
