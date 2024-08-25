using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Scene Manager handles scenes in Unigma. These scenes can exist as possible chunks, where each chunk represents a map centered in the camera.
 * So for example, a player can enter different rooms of a house each room being scene or chunk. Around 4 scene chunks are held in memory at a time
 * as to allow intereaction between chunks and save on loading time.
 */
public class UnigmaSceneManager : MonoBehaviour
{
    public static UnigmaScene currentScene;
    public static int MaxNumActiveScenes = 4;

    static UnigmaSceneManager Instance;
    [HideInInspector]
    public UnigmaScene[] unigmaScenes;
    public Dictionary<string, UnigmaScene> unigmaScenesDictionary;
    public Queue<string> unigmaSceneQueue;

    [HideInInspector]
    public CameraController unigmaCamera;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        unigmaSceneQueue = new Queue<string>();
        unigmaScenesDictionary = new Dictionary<string, UnigmaScene>();
    }

    public void Initialize()
    {
        Debug.Log("Initializing scene manager");

        //Initialize Camera.
        GameObject camPrefab = Resources.Load<GameObject>("Cameras/UnigmaCamera");
        GameObject cameraObj = Instantiate(camPrefab) as GameObject;
        unigmaCamera = cameraObj.GetComponent<CameraController>();
        unigmaCamera.gameObject.AddComponent<GraphicalOcculusion>();
    }

    private void Start()
    {
        SetCameraGraphicalSettings();
    }

    private void Update()
    {
        SetCameraGraphicalSettings();
    }
    public void LoadScene(string sceneName)
    {
        Debug.Log("Begin loading Scene: " + sceneName);
        //Check if it exists within the queue.
        bool needsUpdate = UpdateQueue(sceneName);

        if (needsUpdate)
        {
            GameObject scenePrefab = Resources.Load<GameObject>("UnigmaScenes/" + sceneName) as GameObject;
            GameObject sceneInstance = Instantiate(scenePrefab, transform) as GameObject;
            UnigmaScene scene = sceneInstance.GetComponent<UnigmaScene>();

            Debug.Log("Scene: " + scene.sceneName + " was loaded");

            scene.LoadScene();

            unigmaScenesDictionary.Add(sceneName, scene);
        }
        else
        {
            Debug.Log("This scene: " + sceneName + " has already been loaded.");
        }

        //Sets this scene to current scene.
        currentScene = unigmaScenesDictionary[sceneName];
        currentScene.LaunchScene(); //Launches the scene which does any effects needed to start it.
        unigmaCamera.simplePivot = currentScene.PivotPoint;
    }

    bool UpdateQueue(string sceneName)
    {
        bool containsScene = unigmaSceneQueue.Contains(sceneName);

        //If queue exists no update is required.
        if (containsScene)
            return false;

        //Otherwise check if queue should append this data.
        if (unigmaSceneQueue.Count >= MaxNumActiveScenes)
        {
            string deQueuedScene = unigmaSceneQueue.Dequeue();
            unigmaScenesDictionary[deQueuedScene].UnloadScene();
            unigmaScenesDictionary.Remove(deQueuedScene);
        }

        //Finally enqueue scene.
        unigmaSceneQueue.Enqueue(sceneName);

        return true;
    }

    void SetCameraGraphicalSettings()
    {
        UnigmaCommandBuffers unigmaGraphics = unigmaCamera.gameObject.GetComponent<UnigmaCommandBuffers>();
        if (UnigmaSettings.GetIsRayTracingEnabled())
        {
            Debug.Log("Graphics being Set from Settings");
            if(unigmaGraphics.compositeMaterial != null)
                unigmaGraphics.compositeMaterial.SetVector("_GlobalIlluminationWeights", currentScene.currentGraphicalSettings.GlobalIlluminationWeights);
        }
    }
}
