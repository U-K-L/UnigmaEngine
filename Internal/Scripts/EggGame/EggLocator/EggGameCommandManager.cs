using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EggGameCommandManager : MonoBehaviour
{
    // Start is called before the first frame update
    private CommandNode root;
    private List<CommandNode> _commandList = new List<CommandNode>();
    private Stack<CommandNode> _commandStack = new Stack<CommandNode>();
    private Queue<CommandNode> _commandQueue = new Queue<CommandNode>();

    private EggGameManager _gameManager;

    public static bool locked = false; //Used to lock the executing actions command.

    void Start()
    {
        root = new CommandNode("-1", "root", -1, null);
        root.visited = true;
        _gameManager = GetComponent<EggGameManager>();
        
    }

    // Update is called once per frame
    void Update()
    {
        //Rhythm based execution of commands.
        /*
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Executing commands");
            ExecuteCommands();
        }
        */
    }

    public void StartExecutingCommands()
    {
        StartCoroutine(StartExecutionOfCommands());
    }

    public void StopExecutingCommands()
    {
        StopCoroutine(StartExecutionOfCommands());
    }

    IEnumerator StartExecutionOfCommands()
    {
        while (true)
        {
            Debug.Log("Executing commands");
            yield return new WaitForSeconds(8.0f);
            ExecuteCommands();
        }
    }

    public void AddCommand(EggLocatorUnit unit, string id, string command, int priority, Object[] objects)
    {
        if (unit._agent.CommandQueue.Count > unit._agent.MaxCommands)
        {
            Debug.Log("Too many commands");
            return;
        }
        CommandNode node = new CommandNode(id, command, priority, objects);
        unit._agent.CommandQueue.Add(node);
    }
    
    private void SortQueue()
    {
        //Perform buble sort on list
        for (int i = 0; i < _commandList.Count; i++)
        {
            for (int j = 0; j < _commandList.Count; j++)
            {
                if (_commandList[i].priority > _commandList[j].priority)
                {
                    //swap the two nodes;
                    CommandNode temp = _commandList[i];
                    _commandList[i] = _commandList[j];
                    _commandList[j] = temp;
                }
            }
        }

        //Insert into queue
        _commandQueue = new Queue<CommandNode>(_commandList);
    }

    void CreateCommandList()
    {
        _commandList.Clear();
        foreach (KeyValuePair<string, EggLocatorUnit> UnitPair in _gameManager.GlobalUnits)
        {
            EggLocatorUnit unit = UnitPair.Value;
            foreach (CommandNode node in unit._agent.CommandQueue)
            {
                _commandList.Add(node);
            }
        }
    }

    public void ExecuteCommands()
    {
        CreateCommandList();
        SortQueue();
        CreateCommandTree();

        StopCoroutine(ExecuteCommandTree());
        StartCoroutine(ExecuteCommandTree());
    }

    public void CreateCommandTree()
    {
        CommandNode rootNode = _commandQueue.Dequeue();
        CommandNode previousNode = rootNode;
        previousNode.parent = root;
        root.children.Add(previousNode);
        while (_commandQueue.Count > 0)
        {
            CommandNode currentNode = _commandQueue.Dequeue();

            if (previousNode.priority > currentNode.priority)
            {
                previousNode.children.Add(currentNode);
                currentNode.parent = previousNode;
            }
            else if(previousNode.priority == currentNode.priority)
            {
                previousNode.parent.children.Add(currentNode);
                currentNode.parent = previousNode.parent;
            }

            previousNode = currentNode;
        }
    }

    IEnumerator ExecuteCommandTree()
    {
        _gameManager.LockPlayers();
        int level = 0;
        Queue<CommandNode> queue = new Queue<CommandNode>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            Debug.Log("queue continues");
            CommandNode node = queue.Dequeue();
            if (node.visited == false)
            {
                ExecuteCommandNode(node, level);
                node.visited = true;
            }

            while (locked)
            {
                //wait
                Debug.Log("locked 1");
                yield return new WaitForSeconds(0.1f);
            }

            for (int i = 0; i < node.children.Count; i++)
            {
                queue.Enqueue(node.children[i]);
                if (node.children[i].visited == false)
                {
                    ExecuteCommandNode(node.children[i], level);
                    node.children[i].visited = true;
                }

            }
            
            level++;
            
            while (locked)
            {
                //wait
                Debug.Log("locked 2");
                yield return new WaitForSeconds(0.1f);
            }

        }

        Debug.Log("Unlock PPlayers");
        _gameManager.UnlockPlayers();
        ClearCommands();
    }

    void ExecuteCommandNode(CommandNode node, int id)
    {
        Debug.Log(id + " Executing: " + PrintNode(node));
        CommandCenter(node);
    }

    private void CommandCenter(CommandNode command)
    {
        //decide what to do with each command.
        if (command.command == "jump")
        {
            Lock();
            //StopAllCoroutines();
            StartCoroutine(_gameManager.CommandJump(command.id, command.objects));
        }
    }

    void ClearCommands()
    {
        root.children.Clear();
        _commandList.Clear();
        _commandQueue.Clear();
        _commandStack.Clear();
        _gameManager.ClearAllCommands();
    }

    public static void Unlock()
    {
        locked = false;
    }

    public static void Lock()
    {
        locked = true;
    }

    private void DebugCommands()
    {
        
    }

    private void PrintQueue()
    {
        CommandNode[] nodes = _commandQueue.ToArray();

        string debug = "";
        for (int i = 0; i < nodes.Length; i++)
        {
            debug += PrintNode(nodes[i]) + "\n";
        }

        Debug.Log(debug);
    }



    private string PrintNode(CommandNode node)
    {
        return "Node: " + node.id + " ; " + node.command + " ; " + node.priority;
    }

}
