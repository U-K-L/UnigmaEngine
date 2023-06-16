using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeuralNetworkLayer : MonoBehaviour
{
    float[] _weights;
    float[] _bias;
    float[] _inputs;

    void CreateNN(float[] inputs)
    {
        _inputs = inputs;
        _weights = new float[inputs.Length* inputs.Length];
        _bias = new float[inputs.Length];

        //Randomize weights and bias.
        for (int i = 0; i < _weights.Length; i++)
        {
            _weights[i] = Random.Range(-1.0f, 1.0f);
        }

        for (int i = 0; i < _bias.Length; i++)
        {
            _bias[i] = Random.Range(-1.0f, 1.0f);
        }
    }
    
    void Start()
    {
        CreateNN(new float[] { 1, 2, 3});
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(UnigmaFastMath._instance.GetDotProductResult());
        ForwardPropagation();
    }

    void ForwardPropagation()
    {
        //Just calculate all the neurons against the weights using a dot product.
        //Then add the bias.
        float[] Y = UnigmaFastMath._instance.Mul(_inputs, _weights, 0);
        Y = UnigmaFastMath._instance.Add(Y,_bias);
        UnigmaHelpers.PrintOutMatrix(Y, 3, 3);
    }

    void BackwardPropagation()
    {

    }
}
