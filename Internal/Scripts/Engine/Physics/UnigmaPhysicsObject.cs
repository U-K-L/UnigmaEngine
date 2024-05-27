using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnigmaPhysicsObject : MonoBehaviour
{
    public Vector3 velocity;
    public Vector3 acceleration;


    public Collider collider;
    public Rigidbody rigidbody;

    public float Beta;

    //Make this a singleton
    UnigmaSpaceTime SpaceTimeVectorField;

    bool ObjectSetup = false;

    private void Awake()
    {
        SetUpObject();
    }
    public virtual void UpdatePhysics()
    {
        if (!ObjectSetup)
            SetUpObject();
        //UpdatePosition();
        //UpdateVelocity();
        //UpdateAcceleration();

    }

    void UpdateVelocity()
    {
        velocity += acceleration * Time.fixedDeltaTime;
    }

    void UpdateAcceleration()
    {
        acceleration = Vector3.zero;
        float minDist = float.PositiveInfinity;
        float maxDist = 10.0f;
        Vector3 finalforce = Vector4.zero;
        if (SpaceTimeVectorField != null)
        {
            foreach (UnigmaSpaceTime.VectorPoint vp in SpaceTimeVectorField.VectorField)
            {
                float distance = Vector3.Distance(vp.position, transform.position);

                if (minDist > distance && maxDist > distance)
                {
                    finalforce = vp.direction;
                    minDist = distance;
                }

            }
        }

        acceleration = finalforce;
    }

    void UpdatePosition()
    {
        transform.position = transform.position + velocity * Time.fixedDeltaTime;//Vector3.Lerp(transform.position, transform.position + velocity * Time.fixedDeltaTime, Time.fixedDeltaTime);
    }

    public virtual void HandleMovementCollision()
    {

    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Colliding");
    }

    void SetUpObject()
    {
        if(collider == null)
            collider = GetComponent<Collider>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider>() as Collider;
        if (rigidbody == null)
        {
            rigidbody = transform.gameObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        if (SpaceTimeVectorField == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("GameManager");
            if (obj != null)
                SpaceTimeVectorField = SpaceTimeVectorField.GetComponent<UnigmaSpaceTime>();
        }

        if (Beta == 0)
            Beta = 0.001f;
    }
}
