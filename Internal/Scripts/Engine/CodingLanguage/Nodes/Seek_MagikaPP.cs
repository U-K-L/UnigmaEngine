using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Seek_MagikaPP : MagikaPP_Node
{
    public Seek_MagikaPP(string type, string AData) : base(type, AData)
    {
        this.ID_Children = new List<int>();
    }

    /*
     * Seek has two arguments
     * 1) The target to seek
     * 2) How long it shall continue seeking.
     * 3) If nothing is in the second argument then it will seek until it finds the target.
    */
    public override void CreateNode(string[] tokens, MagikaPP_Node current)
    {
        //The GUI will always give three strings. Even if the player doesn't use the third argument.
        string id = tokens[0];
        string type = tokens[1];
        string target = tokens[2];
        string time = tokens[3];
        
        ID_Children.Add(Int32.Parse(tokens[4]));
        this.ID = Int32.Parse(id);

        MagikaPP_Node targetting = new MagikaPP_Node("target", target);
        MagikaPP_Node timing = new MagikaPP_Node("time", time);

        this.children.Add("target", targetting);
        this.children.Add("time", timing);
        


    }
    
    public override MagikaPP_Node GetNext()
    {
        if (!this.children.ContainsKey("next"))
            return null;
        return this.children["next"];
    }

    public override object[] InterpretNode(AgentAI agent)
    {
        object[] objs = new object[4];
        objs[0] = this.ID;
        objs[1] = this.type;
        objs[2] = GetTargetData(this.children["target"].AData, agent);
        objs[3] = Int32.Parse(this.children["time"].AData);
        return objs;
    }

    Transform GetTargetData(string target, AgentAI agent)
    {
        if (target == "icpu")
            return agent.agentSummoning.GetSummoner().transform;
        return null;
    }

    public override string Compile()
    {
        
        string compiled = "";
        compiled += this.ID + "," + this.type + "," + this.AData;
        
        for (int i = 0; i < this.ID_Children.Count; i++)
        {
            compiled += "," + this.ID_Children[i];
        }
        Debug.Log("Compiled Text: " + compiled);
        return compiled + "\n";
    }

    public override string ToString()
    {
        return this.ID + " , " + this.type + " , " + this.children["target"].type + " , " + this.children["time"].type + " , " + ID_Children.ToString();
    }
}
