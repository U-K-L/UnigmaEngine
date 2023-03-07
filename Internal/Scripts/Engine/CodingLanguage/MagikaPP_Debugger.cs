using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagikaPP_Debugger : MonoBehaviour
{
    // Start is called before the first frame update
    MagikaPP_Parser _parser;
    void Start()
    {
        _parser = new MagikaPP_Parser();
    }

    // Update is called once per frame
    void Update()
    {
        DebugParser();
    }

    void DebugParser()
    {
        MagikaPP_Node head = _parser.CreateAbstractSyntaxTree();
        Stack<MagikaPP_Node> stack = new Stack<MagikaPP_Node>();
        stack.Push(head);
        while (stack.Count > 0)
        {
            MagikaPP_Node node = stack.Pop();
            foreach (KeyValuePair<string, MagikaPP_Node> child in node.children)
            {
                stack.Push(child.Value);
            }
            Debug.Log(node.ToString());
        }
    }        
}
