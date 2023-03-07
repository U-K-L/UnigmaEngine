using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EggGameMaster : MonoBehaviour
{

    float _loadProgress;

    bool startLoadingStage = false;
    TitleScreenUI titleUI;
    GameObject titleOBJ;

    public static EggGameMaster Instance;

    public Material background;
    public enum GameMode
    {
        TitleScreen,
        Singleplayer,
        Multiplayer
    }

    public GameMode gameMode;
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
        
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        GameObject menuButtonPrefab = Resources.Load<GameObject>("UI/MenuButton");
        titleOBJ = new GameObject("TitleScreen");
        titleOBJ.transform.parent = transform;
        titleOBJ.transform.position = Vector3.zero;
        titleUI = titleOBJ.AddComponent<TitleScreenUI>();
        titleUI.buttonPrefab = menuButtonPrefab;
        titleUI.buttons = 2;
        RenderSettings.skybox = background;
        background.SetFloat("_Transition", 1);
        //Instantiate(titleOBJ, transform);
    }

    void Update()
    {
        
    }

    public void Singleplayer()
    {
        Debug.Log("Singleplayer");
        gameMode = GameMode.Singleplayer;
        titleOBJ.gameObject.SetActive(false);
        BeginLoadLevel(titleUI.GetCurrentLevel());
    }

    public void Multiplayer()
    {
        Debug.Log("Multiplayer");
        gameMode = GameMode.Multiplayer;
        titleOBJ.gameObject.SetActive(false);
        BeginLoadLevel(titleUI.GetCurrentLevel());
    }

    public void TitleScreen()
    {
        Debug.Log("TitleScreen");
        gameMode = GameMode.TitleScreen;
        titleOBJ.gameObject.SetActive(true);
        BeginLoadLevel("TitleEgg");
    }

    public void BeginLoadLevel(string levelName)
    {
        StartCoroutine(LoadLevelAsync(levelName));
    }

    private IEnumerator LoadLevelAsync(string levelName)
    {
        startLoadingStage = true;
        var progress = SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Single);

        while (!progress.isDone)
        {
            _loadProgress = progress.progress;
            yield return null;
        }
    }
}
