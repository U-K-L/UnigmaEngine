using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SmearEffect : MonoBehaviour
{
    public Queue<Vector3> _recentPositions = new Queue<Vector3>();
    public bool sendFrameToGPU = true;

    [SerializeField]
    int _frameLag = 0;

    Material _smearMat = null;
    Renderer renderer;
    public Material smearMat
    {
        get
        {
            renderer = GetComponent<Renderer>();
            if (!_smearMat)
                _smearMat = renderer.material;

            if (!_smearMat.HasProperty("_PrevPosition"))
                _smearMat.shader = Shader.Find("Custom/Smear");

            return _smearMat;
        }
    }

    void LateUpdate()
    {
        if (_recentPositions.Count > _frameLag)
            smearMat.SetVector("_PrevPosition", _recentPositions.Dequeue());

        if (sendFrameToGPU)
        {
            smearMat.SetVector("_Position", transform.position);
            smearMat.SetFloat("_SetFramesGPU", 1f);
        }
        else
        {
            smearMat.SetFloat("_SetFramesGPU", 0f);
        }
        _recentPositions.Enqueue(transform.position);
    }
}