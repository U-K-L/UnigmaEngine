using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnigmaEngine
{
    public class UnigmaSprite : MonoBehaviour
    {
        public Vector3 offset;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            RotateRenderPlaneToCamera();
        }

        void RotateRenderPlaneToCamera()
        {
            Vector3 Ceuler = Camera.main.transform.rotation.eulerAngles;
            Vector3 Teuler = gameObject.transform.rotation.eulerAngles;
            float xAngle = Teuler.x;
            float DotAngle = Vector3.Dot(Vector3.down, Camera.main.transform.forward);
            if ( (DotAngle > 0.02 && DotAngle < 0.6))
            {
                xAngle = Mathf.Lerp(Teuler.x, Ceuler.x, Time.deltaTime);
            }
            Vector3 swizzle = new Vector3(xAngle, Ceuler.y, Ceuler.z) + offset;
            gameObject.transform.rotation = Quaternion.Euler(swizzle);
        }
    }
}
