using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EggGameCommandManager : MonoBehaviour
{
    // Start is called before the first frame update

    private class CommandNode
    {
        public string id; //character name.
        public int priority;
        public string command;
        public List<CommandNode> children = new List<CommandNode>();
        public CommandNode parent;
        public Object[] objects;
        public bool visited = false;

        public CommandNode(string id, string command, int priority, Object[] objects)
        {
            this.id = id;
            this.command = command;
            this.priority = priority;
            this.objects = objects;
        }
    }

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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Executing commands");
            ExecuteCommands();
        }
    }

    public void AddCommand(string id, string command, int priority, Object[] objects)
    {
        CommandNode node = new CommandNode(id, command, priority, objects);

        _commandList.Add(node);
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

    public void ExecuteCommands()
    {
        SortQueue();
        CreateCommandTree();

        StopAllCoroutines();
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
