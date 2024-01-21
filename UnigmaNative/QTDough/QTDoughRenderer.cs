using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unigma
{
    public class QTDoughRenderer : MonoBehaviour
    {
        QTDoughApplication QTDApp;
        // Start is called before the first frame update
        void Start()
        {
            QTDApp = QTDoughApplication.Instance;
        }

        // Update is called once per frame
        void Update()
        {
            Debug.Log("Coming from QTDoughRenderer devices: " + QTDApp.GetFoo());
        }

        void OnApplicationQuit()
        {
            QTDApp.OnApplicationQuit();
        }
    }
}
