using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnigmaEngine
{
    public class UnigmaPhysicsObject : MonoBehaviour
    {
        public bool isMassless = false;
        public bool influenceSpaceTime = false;
        public uint objectId = 0;
        public Vector3 velocity;
        public Vector3 acceleration;


        public Collider collider;
        public Rigidbody rigidbody;

        public float Beta;

        public Vector4 netForce;

        public int emitterType = -1;

        private float cohesion = 25.0f;

        public float kelvin = 300.0f;
        public float gravityStrength = 0.0f;
        public float gravityRadius = 0.0f;
        //Make this a singleton
        UnigmaSpaceTime SpaceTimeVectorField;

        bool ObjectSetup = false;

        public PhysicsObject physicsObject;

        float collisionForce = 1.0f;

        private void Awake()
        {

        }

        private void Start()
        {

        }

        public void Initialize()
        {
            SetUpObject();
            if (influenceSpaceTime)
                SetUpBuffers();
            SpaceTimeVectorField = UnigmaPhysicsManager.Instance.unigmaSpaceTime;
        }

        void SetUpBuffers()
        {
            physicsObject = new PhysicsObject();
            UpdatePhysicsBuffers();
        }

        private void Update()
        {
            UpdatePhysics();

            if (influenceSpaceTime)
                UpdatePhysicsBuffers();
        }

        void UpdatePhysicsBuffers()
        {
            //Tie this with a universal game object manager array.
            physicsObject.objectId = objectId;
            physicsObject.position = transform.position;
            physicsObject.strength = gravityStrength;
            physicsObject.radius = gravityRadius;
            physicsObject.kelvin = kelvin;

            UnigmaPhysicsManager.Instance.UodatePhysicsArray(objectId, physicsObject);
        }

        public virtual void UpdatePhysics()
        {
            if (!ObjectSetup)
                SetUpObject();

            if (!isMassless)
            {
                UpdatePosition();
                UpdateVelocity();
                UpdateAcceleration();
                UpdateForceApplied();
            }

        }

        void UpdateForceApplied()
        {
            float totalForce = netForce.magnitude;

            if (totalForce > cohesion)
            {
                rigidbody.useGravity = true;
                rigidbody.isKinematic = false;
            }

            netForce *= 0.85f;
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
            for (int i = 0; i < UnigmaPhysicsManager.Instance.unigmaSpaceTime.VectorFieldNative.Length; i++)
            {
                UnigmaSpaceTime.SpaceTimePoint vp = UnigmaPhysicsManager.Instance.unigmaSpaceTime.VectorFieldNative[i];
                float distance = Vector3.Distance(vp.position, transform.position);

                if (minDist > distance && maxDist > distance)
                {
                    finalforce = vp.force;
                    minDist = distance;
                }

            }


            acceleration = finalforce;
        }

        void UpdatePosition()
        {
            transform.position = transform.position + velocity * Time.fixedDeltaTime;//Vector3.Lerp(transform.position, transform.position + velocity * Time.fixedDeltaTime, Time.fixedDeltaTime);
        }

        bool IsAtRest()
        {
            RaycastHit hit = new RaycastHit();
            Ray ray = new Ray();
            ray.origin = transform.position;
            ray.direction = Vector3.down;

            if (Physics.Raycast(ray, 1))
            {
                return true;
            }

            return false;
        }

        public virtual void HandleMovementCollision()
        {

        }

        void OnCollisionEnter(Collision collision)
        {
            //Vector3 oppForce = collision.contacts[0].normal;
            //transform.position += 10*oppForce * Time.fixedDeltaTime;
            Debug.Log("Colliding");
        }

        void SetUpObject()
        {
            if (collider == null)
                collider = GetComponent<Collider>();
            if (collider == null)
                collider = gameObject.AddComponent<BoxCollider>() as Collider;
            if (rigidbody == null)
            {
                rigidbody = transform.gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            if (Beta == 0)
                Beta = 0.001f;
        }
    }
}
