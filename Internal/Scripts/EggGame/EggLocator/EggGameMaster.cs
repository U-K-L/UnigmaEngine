using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EggGameMaster : MonoBehaviour
{

    float _loadProgress;

    bool startLoadingStage = false;
    TitleScreenUI titleUI;
    SelectionScreenUI selectionScreenUI;
    StageSelectionScreenUI stageSelectionScreenUI;
    GameObject titleOBJ;

    public static EggGameMaster Instance;

    public EggStages currentStage;

    public Material background;
    public enum GameMode
    {
        TitleScreen,
        Singleplayer,
        Multiplayer
    }

    public GameMode gameMode;
    public bool _matchReady = false;

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
        //Set State to Stage Selection.
        //SelectionScreen();
        StageSelectionScreen();
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
    
    public void SelectionScreen()
    {
        GameObject menuButtonPrefab = Resources.Load<GameObject>("UI/MenuButton");
        GameObject obj = new GameObject("SelectionScreen");
        obj.transform.position = new Vector3(10,68.5f, -2);
        obj.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);
        obj.transform.parent = transform;
        selectionScreenUI = obj.AddComponent<SelectionScreenUI>();
    }

    public void StageSelectionScreen()
    {
        GameObject menuButtonPrefab = Resources.Load<GameObject>("UI/MenuButton");
        GameObject obj = new GameObject("StageSelectionScreen");
        obj.transform.position = new Vector3(10, 68.5f, -2);
        obj.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);
        obj.transform.parent = transform;
        stageSelectionScreenUI = obj.AddComponent<StageSelectionScreenUI>();
    }

    public void SetCurrentStage(EggStages stage)
    {
        _matchReady = true;
        currentStage = stage;
    }

    public void ReloadStage()
    {
        stageSelectionScreenUI.gameObject.SetActive(true);
        stageSelectionScreenUI.enabled = true;
        stageSelectionScreenUI.Reload();
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

    public bool MatchReady()
    {
        return _matchReady;
    }
}
