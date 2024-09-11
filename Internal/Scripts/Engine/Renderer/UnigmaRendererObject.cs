using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnigmaEngine;

namespace UnigmaEngine
{
    public class UnigmaRendererObject : MonoBehaviour
    {
        public Renderer _renderer;
        public Material _MaterialBase;
        public Material _MaterialPasses;
        public UnigmaGameObject uobj;
        private void Awake()
        {
            uobj = GetComponent<UnigmaGameObject>();
            _renderer = GetComponent<Renderer>();
            CreatePassMaterial();
            gameObject.AddComponent<IsometricDepthNormalObject>();
            OutlineColor outlineObj = gameObject.AddComponent<OutlineColor>();

            outlineObj.useShader = true;
        }

        public UnigmaRendererObjectStruct unigmaRendererObject;

        public void Initialize()
        {
            unigmaRendererObject = new UnigmaRendererObjectStruct();
            UpdateRendererObject();

        }

        void CreatePassMaterial()
        {
            
            //_MaterialBase.SetInt("_ObjectID", (int)uobj.id);
            _MaterialBase = _renderer.material;

            string shaderName = _renderer.material.shader.name + "Passes";

            shaderName = shaderName.Split("/")[1];

            _MaterialPasses = new Material(Resources.Load<Shader>(shaderName));

            Debug.Log("Unigma LOADED Shader: " + shaderName);
        }

        void UpdateMaterials()
        {
            _MaterialBase.SetInt("_ObjectID", (int)uobj.id);
            _MaterialPasses.CopyPropertiesFromMaterial(_MaterialBase);
        }

        // Update is called once per frame
        void Update()
        {
            UpdateMaterials();
        }


        void UpdateRendererObject()
        {
            unigmaRendererObject.localToWorld = _renderer.localToWorldMatrix;
            unigmaRendererObject.AABBMin = _renderer.bounds.min;
            unigmaRendererObject.AABBMax = _renderer.bounds.max;

            //If a box collider is present use that instead.
            BoxCollider boxCollide = GetComponent<BoxCollider>();
            Rigidbody rb = GetComponent<Rigidbody>();
            if (boxCollide != null && rb != null)
            {
                Vector3 minPoint = transform.TransformPoint(boxCollide.center + new Vector3(-boxCollide.size.x, -boxCollide.size.y, -boxCollide.size.z) * 0.5f);
                Vector3 maxPoint = transform.TransformPoint(boxCollide.center + new Vector3(boxCollide.size.x, boxCollide.size.y, boxCollide.size.z) * 0.5f);

                unigmaRendererObject.AABBMin = minPoint;
                unigmaRendererObject.AABBMax = maxPoint;
            }
        }
    }

}