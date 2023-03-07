using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockGraph : MonoBehaviour
{
    public Dictionary<string, BlockEntity> BlockMap;
    public List<GameObject> BlockObjects;
    public bool DebugOn = false;
    public GameObject Locators;
    

    private int _selectedBlock = 0; //Determines which block is selected
    public BlockEntity CurrentBlock; //The block that is currently selected

    private PlayerCursor cursor;
    private GameObject Stage;
    void Start()
    {
        cursor = GameObject.FindGameObjectWithTag("GameManager").GetComponent<PlayerCursor>();
        //Remove this for when we want blocks to ascend
        GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>().MapLoaded = true;

    }

    public void CreateGraph(GameObject Stage_m = null)
    {
        if (Stage_m != null)
            Stage = Stage_m;
        if (BlockMap == null)
        {
            BlockMap = new Dictionary<string, BlockEntity>();
        }else
        {
            BlockMap.Clear();
        }
        if (BlockObjects == null)
        {
            BlockObjects = new List<GameObject>();
        }

        GameObject blocks = null;

        foreach (Transform transform in Stage.transform)
        {
            if (transform.tag == "BlocksGraph")
            {
                blocks = transform.gameObject;
            }
        }

        //We want to add all the blocks into a list, then we can iterate through the list to add all of its neighbors.
        if (BlockObjects.Count == 0)
        {
            foreach (Transform child in blocks.GetComponentInChildren<Transform>())
            {
                BlockObjects.Add(child.gameObject);
            }
        }


        //The neighbors are stored within the BlockEntity as neighbors.
        foreach (GameObject obj in BlockObjects)
        {

            //We need to add the block component, as they are just game objects at this point.
            BlockEntity block = obj.GetComponent<BlockEntity>();
            if (block == null)
            {
                block = obj.AddComponent<BlockEntity>();
            }
            block.CreateReferences();
            
            //We get the scale and subtract it by 1, so that it calculates the top center surface of the block.
            //Vector3 neighborAdjusted = block.transform.localPosition + (block.transform.localScale - Vector3.one);
            BlockMap.Add(block.hashKey, block);
        }
        
        /*
        foreach (GameObject obj in BlockObjects)
        {
            BlockEntity block = obj.GetComponent<BlockEntity>();
            block.neighbors.Clear();
            AddNeighbors(block);
            
        }

        if (Stage_m != null)
        {
            //StartCoroutine(AscendAllBlocks());
            //StartCoroutine(CheckBlocksForUpdates());
        }
        */

    }

    void AddNeighbors(BlockEntity block)
    {
        Vector3[] neighbors = new Vector3[18];
        //Connected adjacent blocks in the XYZ directions.
        neighbors[0] = block.transform.localPosition + new Vector3(1, 0, 0);
        neighbors[1] = block.transform.localPosition + new Vector3(-1, 0, 0);
        neighbors[2] = block.transform.localPosition + new Vector3(0, 1, 0);
        neighbors[3] = block.transform.localPosition + new Vector3(0, -1, 0);
        neighbors[4] = block.transform.localPosition + new Vector3(0, 0, 1);
        neighbors[5] = block.transform.localPosition + new Vector3(0, 0, -1);
        //Diagonals XZ
        neighbors[6] = block.transform.localPosition + new Vector3(1, 0, 1);
        neighbors[7] = block.transform.localPosition + new Vector3(-1, 0, 1);
        neighbors[8] = block.transform.localPosition + new Vector3(1, 0, -1);
        neighbors[9] = block.transform.localPosition + new Vector3(-1, 0, -1);
        //Adajacent blocks above
        neighbors[10] = block.transform.localPosition + new Vector3(1, 1, 0);
        neighbors[11] = block.transform.localPosition + new Vector3(-1, 1, 0);
        neighbors[12] = block.transform.localPosition + new Vector3(0, 1, 1);
        neighbors[13] = block.transform.localPosition + new Vector3(0, 1, -1);
        //Adjacent blocks below
        neighbors[14] = block.transform.localPosition + new Vector3(1, -1, 0);
        neighbors[15] = block.transform.localPosition + new Vector3(-1, -1, 0);
        neighbors[16] = block.transform.localPosition + new Vector3(0, -1, 1);
        neighbors[17] = block.transform.localPosition + new Vector3(0, -1, -1);

        int index = 0;
        foreach (Vector3 neighbor in neighbors)
        {
            Vector3 neighborAdjusted = neighbor + (block.transform.localScale - Vector3.one);
            if (BlockMap.ContainsKey(neighborAdjusted.ToString()))
            {
                block.neighbors.Add(index.ToString(), BlockMap[neighborAdjusted.ToString()]);
            }
            index++;
        }
    }

    IEnumerator AscendAllBlocks()
    {
        //For all blocks go down y axis by -19.93
        foreach (GameObject obj in BlockObjects)
        {
            obj.transform.localPosition = new Vector3(obj.transform.localPosition.x, obj.transform.localPosition.y - 20f, obj.transform.localPosition.z);
        }
        yield return new WaitForSeconds(0.5f);
        foreach (KeyValuePair<string, BlockEntity> block in BlockMap)
        {
            block.Value.startDescend();
            yield return new WaitForSeconds(.015f);
        }
        foreach (KeyValuePair<string, BlockEntity> block in BlockMap)
        {
            //round position to integer.
            block.Value.transform.localPosition = new Vector3(Mathf.Round(block.Value.transform.localPosition.x), Mathf.Round(block.Value.transform.localPosition.y), Mathf.Round(block.Value.transform.localPosition.z));
        }
        yield return new WaitForSeconds(2f);
        GameObject.FindGameObjectWithTag("GameManager").GetComponent<EggGameManager>().MapLoaded = true;
    }

    void Update()
    {
        SetSelectedBlock();
        if (DebugOn)
            DebugCall();

    }

    void SetSelectedBlock()
    {
        if(BlockMap.ContainsKey(((BlockObjects[_selectedBlock].transform.localScale - Vector3.one) + BlockObjects[_selectedBlock].transform.localPosition).ToString()))
            CurrentBlock = BlockMap[((BlockObjects[_selectedBlock].transform.localScale - Vector3.one) + BlockObjects[_selectedBlock].transform.localPosition).ToString()];

    }        

    public void DisplayIndicators()
    {
        GameObject block = cursor.GetCursorPointsToObject();
        if (block != null)
        {
            BlockEntity blockEntity = block.GetComponent<BlockEntity>();
            if (blockEntity != null)
            {
                AddIndicators(blockEntity);
                _selectedBlock = BlockObjects.IndexOf(block);
            }
        }
    }

    public List<BlockEntity> GetBlocksLevelSetByDistance(BlockEntity startingBlock, int dist)
    {
        //Breadth first search to get all blocks at a certain level.
        List<BlockEntity> blocks = new List<BlockEntity>();

        foreach (KeyValuePair<string, BlockEntity> blockMap in BlockMap)
        {
            BlockEntity block = blockMap.Value;
            if (block.hashKey == startingBlock.hashKey)
                continue;
            if (Vector3.Distance(startingBlock.CenterOfBlock(), block.CenterOfBlock()) <= dist)
                blocks.Add(block);
        }

        return blocks;
    }

    public List<BlockEntity> GetBlocksLevelSet(BlockEntity startingBlock, int level)
    {
        //Breadth first search to get all blocks at a certain level.
        List<BlockEntity> blocks = new List<BlockEntity>();
        Queue<BlockEntity> queue = new Queue<BlockEntity>();
        HashSet<BlockEntity> visitedBlocks = new HashSet<BlockEntity>();
        queue.Enqueue(startingBlock);
        int currentLevel = 0;

        int nextLevel = 0;
        int levelIndex = 0;
        while (queue.Count > 0 && currentLevel < level)
        {
            //Begin to queue the blocks in until max level is reached or no more neighbors.
            //Deque and store all the neighbors into list.
            BlockEntity d_block = queue.Dequeue();
            visitedBlocks.Add(d_block);
            foreach (KeyValuePair<string, BlockEntity> blockMap in d_block.neighbors)
            {
                BlockEntity block = blockMap.Value;
                if (visitedBlocks.Contains(block) == false)
                {
                    blocks.Add(block);
                    queue.Enqueue(block);
                }
            }

            if (levelIndex >= nextLevel)
            {
                nextLevel = queue.Count;
                levelIndex = 0;
                currentLevel++;
            }else
                levelIndex++;
        }
        return blocks;
    }

    public void CollapseBlock()
    {
        GameObject block = cursor.GetCursorPointsToObject();
        if (block != null)
        {
            BlockEntity blockEntity = block.GetComponent<BlockEntity>();
            if (blockEntity != null)
            {
                blockEntity.CalculateBlockDrop();
                _selectedBlock = BlockObjects.IndexOf(block);
                //CreateGraph();
            }
        }

    }

    public void AddIndicators(BlockEntity block)
    {
        foreach(KeyValuePair<string, BlockEntity> blockobj in BlockMap)
        {
            blockobj.Value.RemoveIndicator();
        }
        block.SetIndicator(Locators);
        foreach (KeyValuePair<string, BlockEntity> neighbor in block.neighbors)
        {
            neighbor.Value.SetIndicator(Locators);
        }
    }

    public void AddIndicators(List<BlockEntity> blocks)
    {
        foreach (KeyValuePair<string, BlockEntity> blockobj in BlockMap)
        {
            blockobj.Value.RemoveIndicator();
        }

        foreach (BlockEntity block in blocks)
        {
            block.SetIndicator(Locators);
        }
    }

    public void RemoveAllIndicators()
    {
        foreach (KeyValuePair<string, BlockEntity> blockobj in BlockMap)
        {
            blockobj.Value.RemoveIndicator();
        }
    }

    IEnumerator CheckBlocksForUpdates()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            string[] keys = BlockMap.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                if (BlockMap.ContainsKey(key))
                {
                    if ((int)BlockMap[key].state == 4)
                    {
                        BlockMap[key].state = 0;
                        CreateGraph();
                    }
                }
            }
        }
    }

    public BlockEntity GetBlockFromIndex(int index)
    {

        BlockEntity block = BlockObjects[index].GetComponent<BlockEntity>();//BlockMap[((BlockObjects[index].transform.localScale - Vector3.one) + BlockObjects[index].transform.localPosition).ToString()];
        return block;
    }

    public void Reset()
    {
        
    }

    void DebugCall()
    {
        //Prints the last block pointed at.
        //Debug.Log(BlockMap[((BlockObjects[_selectedBlock].transform.localScale - Vector3.one) + BlockObjects[_selectedBlock].transform.localPosition).ToString()].name);
        Debug.Log(CurrentBlock.name);
        Debug.Log(GetBlockFromIndex(10).name);
        foreach (KeyValuePair<string, BlockEntity> neighbor in CurrentBlock.neighbors)
        {
            Debug.Log(neighbor.Value.name);
        }
    }

    private void OnDrawGizmos()
    {
        if (DebugOn)
        {
            foreach (BlockEntity block in BlockMap.Values)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(block.CenterOfBlock(), 0.1f);
            }

        }
    }
}
