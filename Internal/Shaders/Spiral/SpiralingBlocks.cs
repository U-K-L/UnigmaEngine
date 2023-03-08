using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiralingBlocks : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject blockPrefab;
    private GameObject[] _blocks;
    public int numBlocks = 1;

    void Start()
    {
        _blocks = new GameObject[numBlocks];
        
        //for loop and instantiate blocks
        for (int i = 0; i < numBlocks; i++)
        {
            _blocks[i] = Instantiate(blockPrefab, this.transform);
            _blocks[i].transform.position += VVFSpiral(i);
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < numBlocks; i++)
        {
            _blocks[i].transform.position = Spiral(_blocks[i].transform.position, 0.5f);
        }
    }

    Vector3 Spiral(Vector3 p, float speed)
    {
        Vector3 newPos = p;

        /*
        speed += Mathf.Abs(p.y * 0.1f);
        
        Vector3 vectorValueFunction = new Vector3(3f*Mathf.Sin(Time.time*speed),1*speed, 3f*Mathf.Cos(Time.time*speed));
        newPos += vectorValueFunction * Time.deltaTime;
        */

        newPos.y += Mathf.Sin(Mathf.PI * (Time.time * speed + p.x));

        return newPos;
    }

    Vector3 VVFSpiral(float x)
    {
        return Vector3.right * x;
        //return new Vector3(10f*Mathf.Sin(x *0.2f), 1.5f*x, 10f * Mathf.Cos(x * 0.2f));
    }
}
