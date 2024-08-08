using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameThrower : MonoBehaviour
{
    public float sensitivity = 0.1f; // Sensitivity of the movement

    private Vector3 mouseStartPos;
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Store the mouse start position when the left mouse button is pressed
            mouseStartPos = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            // Calculate the mouse movement
            Vector3 mouseDelta = Input.mousePosition - mouseStartPos;

            // Move the object based on mouse movement
            transform.position -= new Vector3(mouseDelta.x, mouseDelta.y, 0) * sensitivity;

            // Update the mouse start position
            mouseStartPos = Input.mousePosition;
        }
    }
}
