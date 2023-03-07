using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
public class MagikaPP_Parser{
    private List<string[]> _tokens;
    public Dictionary<int, MagikaPP_Node> nodes;

    public MagikaPP_Parser()
    {
        nodes = new Dictionary<int, MagikaPP_Node>();
        _tokens = ReadTextFile();
        PopulateNodesDictionary();
        ConnectNodes();
    }

    List<string[]> ReadTextFile()
    {
        List<string[]> tokens = new List<string[]>();
        foreach (string line in System.IO.File.ReadLines(Application.streamingAssetsPath + "/MagikaCode/SeekCodeC.txt"))
        {
            tokens.Add(line.Split(','));
        }
        return tokens;
    }

    public void PopulateNodesDictionary()
    {
        for (int i = 0; i < _tokens.Count; i++)
        {
            string[] token = _tokens[i];
            nodes.Add(Int32.Parse(token[0]), CreateNode(token));
            if(nodes.ContainsKey(Int32.Parse(token[0])))
                Debug.Log(nodes[Int32.Parse(token[0])].ToString());

        }
    }

    public void ConnectNodes()
    {
        foreach (KeyValuePair<int, MagikaPP_Node> node in nodes)
        {
            node.Value.ConnectToNode(nodes);
        }
    }

    //Creates the AST to be used by the interpreter
    public MagikaPP_Node CreateAbstractSyntaxTree()
    {
        MagikaPP_Node root = null;
        MagikaPP_Node head = null;
        MagikaPP_Node current = null;
        current = nodes[0];

        //Look through each token and create a node for it.
        for (int i = 0; i < _tokens.Count; i++)
        {
            string[] token = _tokens[i];
            //CreateNodeGivenType(token, current);
            current = current.GetNext();

        }

        return root;
    }

    public void CreateNodeGivenType(string[] tokens, MagikaPP_Node current)
    {
        current.children.Add("next", CreateNode(tokens));
    }

    MagikaPP_Node CreateNode(string[] tokens)
    {
        if (string.Equals(tokens[1], "start"))
        {
            MagikaPP_Node node = new MagikaPP_Node(tokens[1], "");
            node.CreateNode(tokens, node);
            return node;
        }

        if (string.Equals(tokens[1], "end"))
        {
            MagikaPP_Node node = new MagikaPP_Node(tokens[1], "");
            node.CreateNode(tokens, node);
            return node;
        }
        if (string.Equals(tokens[1], "message"))
        {
            MagikaPP_Node node = new MagikaPP_Node(tokens[1], "");
            node.CreateNode(tokens, node);
            return node;
        }
        if (string.Equals(tokens[1], "seek"))
        {
            Seek_MagikaPP seekNode = new Seek_MagikaPP(tokens[1], "");
            seekNode.CreateNode(tokens, seekNode);
            return seekNode;
        }
        return null;
    }

}
