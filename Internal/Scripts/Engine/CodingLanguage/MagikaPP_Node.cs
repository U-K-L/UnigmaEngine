using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class MagikaPP_Node
{
    public string type; //What type of node is this?
    public Dictionary<string, MagikaPP_Node> children; //The children of this node
    public string AData; //Arbitrary data.
    public int ID = -2; //The ID of this node. -2 means its not set yet.
    public List<int> ID_Children; //The IDs of the children of this node.

    public MagikaPP_Node(string type, string AData)
    {
        this.type = type;
        this.AData = AData;
        children = new Dictionary<string, MagikaPP_Node>();
        ID_Children = new List<int>();
    }

    public virtual void CreateNode(string[] tokens, MagikaPP_Node current)
    {
        this.ID = Int32.Parse(tokens[0]);
        ID_Children.Add(Int32.Parse(tokens[2]));
    }

    public virtual void ConnectToNode(Dictionary<int, MagikaPP_Node> nodes)
    {
        if (ID_Children.Count > 0)
        {
            MagikaPP_Node targetNode = nodes[ID_Children[0]];
            children.Add("next", targetNode);
        }

    }

    public virtual MagikaPP_Node GetNext()
    {
        Debug.Log(this.type);
        if (!this.children.ContainsKey("next"))
            return null;
        return this.children["next"];
    }

    public virtual object[] InterpretNode(AgentAI agent)
    {
        return null;
    }

    public override string ToString()
    {
        return ID + " , " + type + " , " + AData + " , " + ID_Children.ToString();
    }

    public virtual string Compile()
    {
        return "";
    }
}
