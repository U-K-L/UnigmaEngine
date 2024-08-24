using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnigmaEngineManager : MonoBehaviour
{
    public string InitialScene; //The name of the initial scene to be loaded.

    [HideInInspector]
    public UnigmaPhysicsManager unigmaPhysicsManager;
    [HideInInspector]
    public UnigmaRendererManager unigmaRendererManager;
    [HideInInspector]
    public UnigmaSceneManager unigmaSceneManager;

    public static UnigmaEngineManager Instance;

    public LayerMask RayTracingLayers;
    public LayerMask FluidInteractionLayers;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        unigmaPhysicsManager = gameObject.AddComponent<UnigmaPhysicsManager>() as UnigmaPhysicsManager;
        unigmaRendererManager = gameObject.AddComponent<UnigmaRendererManager>() as UnigmaRendererManager;
        unigmaSceneManager = gameObject.AddComponent<UnigmaSceneManager>() as UnigmaSceneManager;

        //Disable until initialized
        unigmaPhysicsManager.enabled = false;
        unigmaRendererManager.enabled = false;
        unigmaSceneManager.enabled = false;

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
        //unigmaPhysicsManager.Initialize(UnigmaSceneManager.currentScene);
    }
}
