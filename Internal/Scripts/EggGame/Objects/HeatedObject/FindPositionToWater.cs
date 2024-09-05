using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnigmaEngine;
namespace EggGame
{
    public class FindPositionToWater : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            //Get the mean position.
            Vector3 mean = Vector3.zero;
            //Highest bidder algorithm.
                        /*
            int maxCount = 0;
            int index = 0;
            for (int i = 0; i < UnigmaSpaceTime.Instance.VectorFieldNative.Length; i++)
            {
                int count = (int)UnigmaSpaceTime.Instance.VectorFieldNative[i].particlesCount;
                if (count > maxCount)
                {
                    maxCount = count;
                    index = i;
                }
            }

            mean = UnigmaSpaceTime.Instance.VectorFieldNative[index].position;
            */
            for (int i = 0; i < UnigmaSpaceTime.Instance.VectorFieldNative.Length; i++)
            {
                Vector3 lambda = UnigmaSpaceTime.Instance.VectorFieldNative[i].position * (float)((float)UnigmaSpaceTime.Instance.VectorFieldNative[i].particlesCount / (float)(FluidSimulationManager.Instance.NumOfParticles+1));
                Debug.Log("Lambda: "  + lambda.ToString("F7") + " for: " + i  + " Particle Count: " + UnigmaSpaceTime.Instance.VectorFieldNative[i].particlesCount);
                mean += lambda;
            }

            transform.position = mean;
        }
    }
}
