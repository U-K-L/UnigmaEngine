using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterExpand : MonoBehaviour
{
    public Vector3 finalSize;
    private EggGameManager _gameManager;
    public float speed = 1.0f;    
    // Start is called before the first frame update
    void Start()
    {
        _gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>();
        //Set scale to 0
        transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void StartExpand()
    {
        StartCoroutine(Expand());
    }

    IEnumerator Expand()
    {
        //Expand to final size
        while (transform.localScale.x < finalSize.x)
        {
            transform.localScale += new Vector3(speed * Time.deltaTime, speed * Time.deltaTime, speed * Time.deltaTime);
            yield return null;
        }
    }
}
