using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagikaPP_Edge
{
    private Vector2 _position;
    public struct Edge
    {
        public MagikaPP_SyntaxNode source;
        public MagikaPP_SyntaxNode target;
    }

    public Edge edge;
    public bool targetConnected = false;

    public Vector2 GetPosition()
    {
        return _position;
    }

    public void SetPosition(Vector2 position)
    {
        _position = position;
    }

    public MagikaPP_Edge()
    {
        edge = new Edge();
        edge.source = new MagikaPP_SyntaxNode();
        edge.target = new MagikaPP_SyntaxNode();
    }
}
