using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class CubeSurfaces : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject blockObj;
    public Vector3 dimensions = new Vector3(3,1,3);
    public Vector3 offsets = new Vector3(4.15f, 1, 4.04f);
    private Dictionary<string, GameObject> blockMatrix;
    private Vector3 originOffsets;
    void Start()
    {
        originOffsets = offsets;
        blockMatrix = new Dictionary<string, GameObject>();
        createBlockMatrix();
    }

    // Update is called once per frame
    void Update()
    {
        int sum = (int)(dimensions.x * dimensions.y * dimensions.z);
        if (blockMatrix.Count != sum || !offsets.Equals(originOffsets))
        {
            destroyBlockMatrix();
            createBlockMatrix();
        }
            
    }

    void createBlockMatrix()
    {
        Vector3 pos = transform.position;
        for (int i = 0; i < dimensions.x; i++)
        {
            for (int j = 0; j < dimensions.y; j++)
            {

                for (int k = 0; k < dimensions.z; k++)
                {
                    //Vector3 offsetBlock = new Vector3(pos.x * dimensions.x * i, pos.y * dimensions.y * j, pos.z * dimensions.z * k)
                    Vector3 offsetBlock = (Vector3.right * i/offsets.x) + (Vector3.down * j/offsets.y) + (Vector3.forward * k/offsets.z);
                    GameObject block = Instantiate(blockObj, pos + offsetBlock, transform.rotation, transform);
                    block.GetComponent<BlockEntity>().hashKey = block.transform.position.ToString();
                    blockMatrix.Add(block.GetComponent<BlockEntity>().hashKey, block);
                    originOffsets = offsets;
                }
            }
        }
    }

    void destroyBlockMatrix()
    {
        foreach(KeyValuePair<string, GameObject> block in blockMatrix)
        {

            Destroy(block.Value);
        }
        blockMatrix.Clear();
    }
}
