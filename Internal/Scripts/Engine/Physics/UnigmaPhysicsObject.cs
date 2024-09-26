using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnigmaEngine
{
    public class UnigmaPhysicsObject : MonoBehaviour
    {
        int _physicsId;
        bool initialized = false;
        UnigmaGameObject unigmaGameObject;
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
            unigmaGameObject = GetComponent<UnigmaGameObject>();
            SetUpObject();
            //if (influenceSpaceTime)
            SetUpBuffers();
        }

        void SetUpBuffers()
        {
            physicsObject = new PhysicsObject();


            unigmaGameObject.unigmaGameObject.physicsId = (uint)UnigmaPhysicsManager.Instance._physicsObjects.Count;
            _physicsId = (int)unigmaGameObject.unigmaGameObject.physicsId;
            physicsObject.objectId = (uint)_physicsId;
        }

        private void Update()
        {
            if (UnigmaPhysicsManager.Instance.PhysicsObjectsArray.Length > 0 && !initialized)
            {
                SetPhysicsBufferData();
                initialized = true;
            }
            UpdatePhysics();

                //if (influenceSpaceTime)
            UpdatePhysicsBuffers();
            //TransferPhysicsBufferToUnigmaPhysics();
        }

        public void SetPhysicsBufferData()
        {
            //Tie this with a universal game object manager array.
            physicsObject.objectId = (uint)_physicsId;
            physicsObject.position = transform.position;
            physicsObject.localToWorld = transform.localToWorldMatrix;
            physicsObject.strength = gravityStrength;
            physicsObject.radius = gravityRadius;
            physicsObject.kelvin = kelvin;

            UnigmaPhysicsManager.Instance.UodatePhysicsArray(unigmaGameObject.unigmaGameObject.physicsId, physicsObject);
        }

        void UpdatePhysicsBuffers()
        {
            //Tie this with a universal game object manager array.
            physicsObject.objectId = (uint)_physicsId;
            physicsObject.position = transform.position;
            physicsObject.localToWorld = transform.localToWorldMatrix;
            physicsObject.strength = gravityStrength;
            physicsObject.radius = gravityRadius;
            physicsObject.kelvin = kelvin;
            physicsObject.acceleration = UnigmaPhysicsManager.Instance.PhysicsObjectsArray[_physicsId].acceleration;
            physicsObject.velocity = UnigmaPhysicsManager.Instance.PhysicsObjectsArray[_physicsId].velocity;

            UnigmaPhysicsManager.Instance.UodatePhysicsArray(unigmaGameObject.unigmaGameObject.physicsId, physicsObject);
        }

        void TransferPhysicsBufferToUnigmaPhysics()
        {
            Vector3 pObjPos = UnigmaPhysicsManager.Instance.PhysicsObjectsArray[(int)unigmaGameObject.unigmaGameObject.physicsId].position;
            transform.position = Vector3.Lerp(transform.position, pObjPos, Time.deltaTime);
        }

        public virtual void UpdatePhysics()
        {
            if (!ObjectSetup)
                SetUpObject();

            if (!isMassless)
            {
                UpdatePosition();
                //UpdateVelocity();
                //UpdateAcceleration();
                //UpdateForceApplied();
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
            Vector3 finalforce = Vector3.down;//Vector4.zero;
            /*
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
            */

            acceleration = finalforce;
        }

        void UpdatePosition()
        {
            Vector3 pObjVelocity = UnigmaPhysicsManager.Instance.PhysicsObjectsArray[_physicsId].velocity;
            Vector3 newPos = transform.position + pObjVelocity * Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime);
            //transform.position = transform.position + velocity * Time.fixedDeltaTime;//Vector3.Lerp(transform.position, transform.position + velocity * Time.fixedDeltaTime, Time.fixedDeltaTime);
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
                rigidbody = GetComponent<Rigidbody>();
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
