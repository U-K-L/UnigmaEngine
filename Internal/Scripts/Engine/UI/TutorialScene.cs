using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialScene : MonoBehaviour
{
    // Start is called before the first frame update

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 Ceuler = Camera.main.transform.rotation.eulerAngles;
        Vector3 Teuler = gameObject.transform.rotation.eulerAngles;
        Vector3 swizzle = new Vector3(Teuler.x, Ceuler.y, Ceuler.z);
        gameObject.transform.rotation = Quaternion.Euler(swizzle);
    }
}
