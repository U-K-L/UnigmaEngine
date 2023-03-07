using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchClickDetection : MonoBehaviour
{
    // Start is called before the first frame update
    private Camera _cam;
    
    void Start()
    {
        _cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMouseCursorPosition();
        DefineCursorSettings();

    }

    void UpdateMouseCursorPosition()
    {
      
    }
    
    void DefineCursorSettings()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }
}
