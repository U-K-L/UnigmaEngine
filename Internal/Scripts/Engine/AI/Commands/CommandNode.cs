using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandNode
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