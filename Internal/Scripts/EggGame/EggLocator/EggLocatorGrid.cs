/* Created by U.K.L. on 8/16/2022
 * 
 * The manager class for controlling the state and stages of the game.
 * 
 */


using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EggLocatorGrid : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject emptyBlockObj;
    public Vector3 dimensions = new Vector3(3, 1, 3);
    public Vector3 offsets = new Vector3(4.15f, 1, 4.04f);
    private Dictionary<string, GameObject> blockMatrix;
    private Vector3 originOffsets;
    public GameObject locators;
    public List<GameObject> indicators;
    public int numberOfEggs;
    private GameObject[] _eggUnits;
    private bool _eggsCreated = false;
    private GameObject _scoreCanvas;
    private TextMeshProUGUI _TextCanvas;
    public bool gameStarted = false;

    public GameObject _audioManager;
    private EggLocatorUnit kanaloaUnitPlayer;
    public static int roundsPlayed = 0;
    public static int TotalEggs = 0;

    public enum GameState {Starting, Playing, Ended};
    public GameState gamestate;

    public GameObject cursor;
    public GameObject _cam;

    private bool _wonGame = false;
    public EggJumpMaps map;

    private GameObject kanaloa;
    void Start()
    {
        dimensions.x = map.size[0];
        dimensions.z = map.size[1];
        dimensions.y = map.PrefabMatrix.Count;
        _cam = Instantiate(_cam);
        _cam.name = "Main Camera";
        Instantiate(cursor);
        gamestate = GameState.Starting;
        roundsPlayed += 1;
        numberOfEggs = Mathf.CeilToInt(Random.value * 2f) + Mathf.Clamp(2*roundsPlayed, 0, 25);
        if (GameObject.FindGameObjectWithTag("Audio") == null)
            _audioManager = Instantiate(_audioManager);
        _scoreCanvas = GameObject.Find("ScoreCanvas");
        _TextCanvas = _scoreCanvas.GetComponentInChildren<TextMeshProUGUI>();
        _eggUnits = new GameObject[numberOfEggs];
        originOffsets = offsets;
        blockMatrix = new Dictionary<string, GameObject>();
        createBlockMatrix();

        
    }

    // Update is called once per frame
    void Update()
    {

        
        //Create the eggs.
        if (_eggsCreated == false && gameStarted == true)
        {
            _eggsCreated = true;
            createAgents();
            StartCoroutine(CreateEggs());

        }

        int sum = (int)(dimensions.x * dimensions.y * dimensions.z);
        if (blockMatrix.Count != sum || !offsets.Equals(originOffsets))
        {
            destroyBlockMatrix();
            createBlockMatrix();
        }
        if(gameStarted)
            CheckWinConditions();

        UpdateCamera();
    }

    void createAgents()
    {
        //Create player controlled character.
        kanaloa = Resources.Load<GameObject>("Characters/Kanaloa/EggPrefabs/Kanaloa");
        kanaloa = GameObject.Instantiate(kanaloa);
        EggLocatorUnit Kanaloa_Unit = kanaloa.GetComponent<EggLocatorUnit>();
        Kanaloa_Unit.SetCurrentBlock(blockMatrix["" + 3 + "," + 0 + "," + 3].GetComponent<BlockEntity>());
        kanaloa.transform.position = Kanaloa_Unit.GetCurrentBlock().transform.position + Vector3.up * 42.25f;
        Kanaloa_Unit.isPlayer = true;

        kanaloaUnitPlayer = Kanaloa_Unit;


    }
    IEnumerator CreateEggs()
    {
        InstantiateAllEggs();
        yield return new WaitForSeconds(0.01f);
        for (int i = 0; i < numberOfEggs; i++)
        {
            GameObject egg = _eggUnits[i];
            int x = (int)(Random.value * (dimensions.x - 1f));
            int y = (int)(Random.value * (dimensions.y - 1f));
            int z = (int)(Random.value * (dimensions.z - 1f));
            BlockEntity block = blockMatrix["" + x + "," + y + "," + z].GetComponent<BlockEntity>();
            if (block.HasAgentOnMe())
            {
                bool foundSpot = false;
                for (int j = 0; j < dimensions.x; j++)
                {
                    if (foundSpot)
                        break;
                    for (int k = 0; k < dimensions.y; k++)
                    {
                        if (foundSpot)
                            break;
                        for (int l = 0; l < dimensions.z; l++)
                        {
                            if (blockMatrix["" + j + "," + k + "," + l].GetComponent<BlockEntity>().HasAgentOnMe() == false)
                            {
                                createEgg(new Vector3(j, k, l), egg);
                                foundSpot = true;
                            }
                            if (foundSpot)
                                break;
                        }

                    }
                }
            }
            else
            {
                createEgg(new Vector3(x,y,z), egg);
            }
            
        }
    }

    public void InstantiateAllEggs()
    {
        for (int i = 0; i < numberOfEggs; i++)
        {
            GameObject eggObj = Resources.Load<GameObject>("Characters/SunnyEgg/Sunny");
            GameObject egg = Instantiate(eggObj);
            _eggUnits[i] = egg;
        }

        
    }
    void createEgg(Vector3 pos, GameObject egg)
    {
        EggLocatorUnit egg_Unit = egg.GetComponent<EggLocatorUnit>();
        egg_Unit.SetCurrentBlock(blockMatrix["" + pos.x + "," + pos.y + "," + pos.z].GetComponent<BlockEntity>());
        egg.transform.position = egg_Unit.GetCurrentBlock().transform.position + Vector3.up * 62.25f;
    }
    void createBlockMatrix()
    {

        Vector3 pos = transform.position;
        for (int i = 0; i < dimensions.x; i++)
        {
            for (int j = 0; j < dimensions.y; j++)
            {

                for (int k = 0; k < dimensions.z; k++)
                {
                    //Vector3 offsetBlock = new Vector3(pos.x * dimensions.x * i, pos.y * dimensions.y * j, pos.z * dimensions.z * k)
                    Vector3 offsetBlock = (Vector3.right * i / offsets.x) + (Vector3.down * j / offsets.y) + (Vector3.forward * k / offsets.z);
                    GameObject block = Instantiate(emptyBlockObj, pos + offsetBlock, transform.rotation, transform);
                    if (map.PrefabMatrix[j][i, k] != null)
                    {
                        block = Instantiate(map.PrefabMatrix[j][i, k], pos + offsetBlock, transform.rotation, transform);
                    }
                    string hashkey = "" + i + "," + j + "," + k;
                    block.GetComponent<BlockEntity>().hashKey = hashkey;
                    block.GetComponent<BlockEntity>().KeyPos = new Vector3(i, j, k);
                    block.GetComponent<BlockEntity>().setupBlock();
                    block.name += hashkey;
                    blockMatrix.Add(hashkey, block);
                    originOffsets = offsets;
                }
            }
        }
        StopAllCoroutines();
        StartCoroutine(descendBlock());
    }

    void destroyBlockMatrix()
    {
        StartCoroutine(ascendBlock());

    }

    IEnumerator descendBlock()
    {
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < dimensions.x; i++)
        {
            for (int j = 0; j < dimensions.y; j++)
            {

                for (int k = 0; k < dimensions.z; k++)
                {
                    GameObject block = blockMatrix["" + i + "," + j + "," + k];
                    block.GetComponent<BlockEntity>().startDescend();
                    yield return new WaitForSeconds(.01f);
                }
            }
        }
        yield return new WaitForSeconds(0.1f);
        gameStarted = true;
        yield return new WaitForSeconds(2f);
        gamestate = GameState.Playing;
    }

    IEnumerator ascendBlock()
    {
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < dimensions.x; i++)
        {
            for (int j = 0; j < dimensions.y; j++)
            {

                for (int k = 0; k < dimensions.z; k++)
                {
                    GameObject block = blockMatrix["" + i + "," + j + "," + k];
                    block.GetComponent<BlockEntity>().startDescend();
                    yield return new WaitForSeconds(.015f);
                }
            }
        }
        yield return new WaitForSeconds(2f);
        gameStarted = false;
        foreach (KeyValuePair<string, GameObject> block in blockMatrix)
        {

            Destroy(block.Value);
        }
        blockMatrix.Clear();

        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        if (_wonGame)
        {
            BeginLoadLevel(1);
        }
        else
        {
            BeginLoadLevel(0);
        }
    }

    public Dictionary<string, GameObject> GetBlockMatrix()
    {
        return blockMatrix;
    }

    public void SetLocators(BlockEntity block)
    {
        if (block.blockFallen == true)
            return;
        if (block._indicator == null)
        {
            GameObject locator = Instantiate(locators, block.transform);
            block._indicator = locator;
            block._indicator.transform.localPosition = Vector3.zero;
            block._indicator.transform.localScale = block._indicator.transform.localScale * 0.35f;
            block._indicator.transform.position += Vector3.up * 1.5f;
        }


        indicators.Add(block._indicator);
    }

    public void RemoveAllLocators()
    {
        foreach (GameObject obj in indicators)
        {
            Destroy(obj);
        }
        indicators.Clear();
    }

    public void CheckWinConditions()
    {
        if (kanaloaUnitPlayer.dead && _wonGame == false)
        {
            YouLose();
            return;
        }
        _TextCanvas.text = "Eggs: " + (numberOfEggs - (AgentPhysicsManager.GetAllAgents().Count - 1)) + "/" + numberOfEggs + "              Total Eggs: " + TotalEggs;
        if (AgentPhysicsManager.GetAllAgents().Count < 2)
        {
            foreach (KeyValuePair<string, AgentPhysics> entry in AgentPhysicsManager.GetAllAgents())
            {
                AgentPhysics agent = entry.Value;
                if (agent.GetComponent<EggLocatorUnit>().isPlayer)
                    YouWin();
            }
        }

    }

    public void YouWin()
    {
        _TextCanvas.text = "You won!!!"  + "              Total Eggs: " + TotalEggs;
        _wonGame = true;
        destroyBlockMatrix();
    }

    public void YouLose()
    {
        _TextCanvas.text = "You Lost!!!" + "              Total Eggs: " + TotalEggs;
        roundsPlayed = 0;
        TotalEggs = 0;
        _wonGame = false;
        destroyBlockMatrix();
    }


    public void BeginLoadLevel(int x)
    {
        StartCoroutine(LoadLevelAsync(x));
    }
    
    private IEnumerator LoadLevelAsync(int x)
    {
        var progress = SceneManager.LoadSceneAsync(x, LoadSceneMode.Single);

        while (!progress.isDone)
        {
            yield return null;
        }
    }

    public void UpdateCamera()
    {
        _cam.GetComponent<CameraController>().moveCamera(kanaloa.transform.position);
        AgentPhysics agent = kanaloa.GetComponent<AgentPhysics>();
        if (agent)
        {
            agent.currentlyFollowed = true;
        }
    }
}
