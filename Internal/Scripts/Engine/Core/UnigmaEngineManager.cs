using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using static UnityEngine.UI.CanvasScaler;

namespace UnigmaEngine
{
    public class UnigmaEngineManager : MonoBehaviour
    {

        public string InitialScene; //The name of the initial scene to be loaded.

        [HideInInspector]
        public UnigmaPhysicsManager unigmaPhysicsManager;
        [HideInInspector]
        public UnigmaRendererManager unigmaRendererManager;
        [HideInInspector]
        public UnigmaSceneManager unigmaSceneManager;
        [HideInInspector]
        public UnigmaNativeManager unigmaNativeManager;

        public static UnigmaEngineManager Instance;

        public LayerMask RayTracingLayers;
        public LayerMask PhysicsLayers;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            unigmaNativeManager = gameObject.AddComponent<UnigmaNativeManager>() as UnigmaNativeManager;
            unigmaPhysicsManager = gameObject.AddComponent<UnigmaPhysicsManager>() as UnigmaPhysicsManager;
            unigmaRendererManager = gameObject.AddComponent<UnigmaRendererManager>() as UnigmaRendererManager;
            unigmaSceneManager = gameObject.AddComponent<UnigmaSceneManager>() as UnigmaSceneManager;

            //Disable until initialized
            unigmaNativeManager.enabled = true;
            unigmaPhysicsManager.enabled = true;
            unigmaRendererManager.enabled = true;
            unigmaSceneManager.enabled = true;

            unigmaSceneManager.Initialize();
        }

        void Start()
        {
            StartUnigmaEngine();
        }

        void StartUnigmaEngine()
        {
            Debug.Log("Starting Unigma Engine");
            unigmaSceneManager.LoadScene(InitialScene);
            unigmaRendererManager.Initialize(UnigmaSceneManager.currentScene);
            unigmaPhysicsManager.Initialize(UnigmaSceneManager.currentScene);
        }

    }
}