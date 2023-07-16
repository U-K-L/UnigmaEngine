using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPointsHemisphere : MonoBehaviour
{
    // Start is called before the first frame update
    public ComputeShader RandomPointsOnSphere;
    private ComputeBuffer _ResultBuffer;
    public int positionsCount = 100;
    private Vector3[] positions;
    public GameObject plane;
    void Start()
    {
        positions = new Vector3[positionsCount];
        _ResultBuffer = new ComputeBuffer(positions.Length, 12);
        StartCoroutine(UpdatePositions());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator UpdatePositions()
    {
        while (true)
        {
            RandomPointsOnSphere.SetBuffer(0, "Result", _ResultBuffer);
            RandomPointsOnSphere.SetVector("_Seed", new Vector2(Random.value, Random.value));
            RandomPointsOnSphere.SetVector("_Normal", plane.GetComponent<MeshFilter>().mesh.normals[0]);
            RandomPointsOnSphere.Dispatch(0, positions.Length, 1, 1);
            _ResultBuffer.GetData(positions);
            //Do so randomly.
            /*
            for (int i = 0; i < positions.Count; i++)
            {
                positions[i] = new Vector3(Random.value, Random.value, Random.value);
            }
            */
            //Wait seconds.
            yield return new WaitForSeconds(0.1f);
        }

    }

    //Draw gizomos for spheres
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 5);

        //Draw yellow spheres for each point
        Gizmos.color = Color.yellow;
        if (positions != null)
        {
            foreach (Vector3 pos in positions)
            {
                Gizmos.DrawSphere(transform.position + pos, 0.1f);
            }
        }
    }
}
