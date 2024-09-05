using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnigmaEngine
{
    public class AgentInanimate : UnigmaPhysicsObject
    {
        private void FixedUpdate()
        {
            base.UpdatePhysics();
        }

        void OnCollisionEnter(Collision collision)
        {
            PushObjectOut(collision);
        }

        private void OnCollisionExit(Collision collision)
        {

        }

        private void OnCollisionStay(Collision collision)
        {
            PushObjectOut(collision);
        }

        void PushObjectOut(Collision collision)
        {
            velocity *= 0.5f;

            float magnitude = velocity.magnitude;
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint cPoint = collision.GetContact(i);
                //if (cPoint.thisCollider.gameObject.GetInstanceID() == this.gameObject.GetInstanceID())
                //    continue;
                Vector3 normal = cPoint.normal;

                float collisionDepth = Vector3.Distance(cPoint.point, this.transform.position);

                velocity += (-Beta / Time.fixedDeltaTime) * -normal;


                Debug.Log(normal + " " + collision.GetContact(i).thisCollider.transform.name);
            }

        }

    }
}