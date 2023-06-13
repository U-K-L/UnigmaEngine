using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeuralNetworkLayer : MonoBehaviour
{
    void Start()
    {
        float[] A = { 1, 2, 3, 4 };
        float[] B = { 5, 6, 7, 8 };
        UnigmaFastMath._instance.Dot(A, B);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(UnigmaFastMath._instance.GetDotProductResult());
    }
}
