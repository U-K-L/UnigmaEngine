using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FillWater : MonoBehaviour
{
    // Start is called before the first frame update
    public Vector3 finalSize;
    private EggGameManager _gameManager;
    private MeshRenderer _renderer;
    public float speed = 1.0f;
    public GameObject dependentObj; //Object that appears after this loaded.

    private bool coroutineStarted = false;
    void Start()
    {
        _gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>();
        _renderer = GetComponent<MeshRenderer>();
        _renderer.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (_gameManager.MapLoaded && !coroutineStarted)
            StartElongation();
        
    }

    public void StartElongation()
    {
        coroutineStarted = true;
        _renderer.enabled = true;
        StopAllCoroutines();
        StartCoroutine(Elongate());
    }

    IEnumerator Elongate()
    {
        //Make game object grow in size only in y direction
        while (transform.localScale.y < finalSize.y)
        {
            transform.localScale += new Vector3(0, 0.01f, 0);
            yield return new WaitForSeconds(0.01f * speed);
        }

        if (dependentObj)
        {
            dependentObj.GetComponent<MeshRenderer>().enabled = true;
            dependentObj.GetComponent<WaterExpand>().StartExpand();
        }            
    }
}
