using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnigmaSpaceTime : MonoBehaviour
{
    public struct VectorPoint
    {
        public Vector3 position;
        public Vector4 direction;
    }

    public Vector3 SpaceTimeSize;

    public int SpaceTimeResolution;

    public VectorPoint[] VectorField;

    private void Awake()
    {
        int numOfVectors = Mathf.CeilToInt(SpaceTimeResolution) * Mathf.CeilToInt(SpaceTimeResolution) * Mathf.CeilToInt(SpaceTimeResolution);
        VectorField = new VectorPoint[numOfVectors];

        for (int i = 0; i < VectorField.Length; i++)
        {
            VectorField[i].direction = Vector4.zero;
            VectorField[i].position = Vector3.zero;
        }

        ShapeSpaceTime();
    }

    void ShapeSpaceTime()
    {
        int xSize = Mathf.CeilToInt(SpaceTimeResolution);
        int ySize = Mathf.CeilToInt(SpaceTimeResolution);
        int zSize = Mathf.CeilToInt(SpaceTimeResolution);

        float spacing = (SpaceTimeSize.x / (SpaceTimeResolution-1));
        float halfContainerSize = SpaceTimeSize.x / 2.0f;
        for (int i = 0; i < xSize; i++)
        {
            for (int j = 0; j < ySize; j++)
            {
                for (int k = 0; k < zSize; k++)
                {
                    int index = i * ySize * zSize + j * zSize + k;

                    VectorField[index].position = new Vector3(i * spacing - halfContainerSize, j * spacing - halfContainerSize, k * spacing - halfContainerSize);
                    VectorField[index].direction = new Vector4(0, -9.8f, 0, 1.0f);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        //Set int for simulation
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, SpaceTimeSize);

        
        if (VectorField != null)
        {
            float spacing = (SpaceTimeSize.x / (SpaceTimeResolution - 1)) *0.5f;
            foreach (VectorPoint vp in VectorField)
            {
                Ray ray = new Ray(vp.position, new Vector3(vp.direction.x, vp.direction.y, vp.direction.z) * spacing);
                Gizmos.color = new Vector4(0.0f + vp.direction.w, 0.6f, 1.0f, 1.0f);
                Gizmos.DrawRay(ray);
                //Gizmos.DrawSphere(vp.position, 0.025f);
            }
        }
    }
}
