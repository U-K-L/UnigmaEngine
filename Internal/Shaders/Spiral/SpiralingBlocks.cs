using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiralingBlocks : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject blockPrefab;
    private BlockEntity[] _blocks;
    public int numBlocks = 1;

    public delegate Vector3 Function(Vector3 p, float x, float speed);
    public enum FunctionType { Line, SineWave }
    public FunctionType functionType;

    public Function[] functions = {Line, SineWave };

    public float speed;

    public bool animate;

    public int character;

    EggLocatorUnit Unit;
    void Start()
    {
        _blocks = new BlockEntity[numBlocks];

        //for loop and instantiate blocks
        for (int i = 0; i < numBlocks; i++)
        {
            _blocks[i] = Instantiate(blockPrefab, this.transform).AddComponent<BlockEntity>();
            _blocks[i].transform.position += StartPositions(i);
        }

        if (animate)
            InitializeCharacter();
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < numBlocks; i++)
        {
            Vector3 p = _blocks[i].transform.localPosition;
            //Convert to domain of [-1,1].
            float x = ((p.x / (numBlocks / 2)) - numBlocks / 2);
            _blocks[i].transform.localPosition = functions[(int)functionType](p, x, speed);
        }
    }

    static Vector3 SineWave(Vector3 p, float x, float speed)
    {
        Vector3 newPos = p;
        newPos.y = 4f*Mathf.Sin( Mathf.PI * (x  + Time.time * speed));
        return newPos;
    }
    
    static Vector3 Line(Vector3 p, float x, float speed)
    {
        return new Vector3(p.x, 0, 0);
    }

    Vector3 StartPositions(float x)
    {
        return Vector3.right * x;
    }

    void InitializeCharacter()
    {
        //Create player controlled character.
        GameObject characterObj = EggGameData.LoadCharacterFromIndex(character);
        characterObj = GameObject.Instantiate(characterObj);
        Unit = characterObj.GetComponentInChildren<EggLocatorUnit>();
        //Temporary hack before making a proper character select screen.
        //Debug.Log("Created");
        Unit.SetCurrentBlock(_blocks[0]);
        characterObj.transform.position = Unit.GetCurrentBlock().CenterOfBlock() + Vector3.up * 72.25f;

        StartCoroutine(Jump());
    }

    IEnumerator Jump()
    {
        int i = 0;
        int delta = 1;
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if(Unit._agent.IsTrulyAirborne() == false)
                Unit.JumpToBlock(_blocks[i]);
            if (i >= numBlocks-1)
                delta = 0;
            if(i <= 0)
                delta = 1;
            
            i += delta;

        }
    }
}
