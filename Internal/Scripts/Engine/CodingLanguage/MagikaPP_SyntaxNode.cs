using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagikaPP_SyntaxNode : ImmediateModeShapeDrawer
{
    public MagikaPP_Node MagikaNode;
    public MagikaPP_Edge[] _edges;
    protected Vector2 position;
    protected Vector2 _cellPosition; //for menu.
    protected bool selected;
    protected Vector3 _position3D;
    protected bool _instantiated = false;
    protected bool _active = false;
    public string type = "null";
    public int currentConnection = 0;
    public MagikaPP_SyntaxNode()
    {
        MagikaNode = new MagikaPP_Node("","");
        _edges = new MagikaPP_Edge[0];
    }

    public void UpdateNode()
    {
        foreach (MagikaPP_Edge edge in _edges)
        {
            if (edge != null)
            {
                if (GetActive())
                    edge.edge.target.SetActive(true);
            }
        }
    }
    
    public virtual void DrawShape(bool reset, Camera cam, float animating, Transform transform)
    {
        
    }

    public virtual void DrawCell(bool reset, Camera cam, float animating, Vector2 pos, Vector4 offsetWidthHeight, float scale)
    {
        
    }

    public virtual void DrawConnections(Camera cam, Transform transform, MagikaPP_Edge edge)
    {

    }

    public void Set3DPosition(Vector3 pos)
    {
        _position3D = pos;
    }

    public Vector3 Get3DPosition()
    {
        return _position3D;
    }

    public virtual Vector2 GetTargetPosition()
    {
        return GetPosition();
    }        
    public MagikaPP_Edge[] GetConnections()
    {
        return _edges;
    }

    public virtual MagikaPP_Edge GetConnection()
    {
        return _edges[currentConnection];
    }

    public void SetInstantiation(bool instantiated)
    {
        _instantiated = instantiated;
    }
    public bool GetInstantiation()
    {
        return _instantiated;
    }

    public void SetActive(bool active)
    {
        _active = active;
    }

    public bool GetActive()
    {
        return _active;
    }

    /*
    public void AddConnection(string name, MagikaPP_Edge node)
    {
        _edges.Add(name, node);
    }



    public bool HasConnection(string name)
    {
        return _edges.ContainsKey(name);
    }

    public void RemoveConnection(string name)
    {
        _edges.Remove(name);
    }

    public void RemoveAllConnections()
    {
        _edges.Clear();
    }
    */

    public Vector2 GetPosition()
    {
        return position;
    }

    public void SetPosition(Vector2 position)
    {
        this.position = position;
    }
    
    public Vector2 GetCellPosition()
    {
        return _cellPosition;
    }

    public void SetCellPosition(Vector2 pos)
    {
        _cellPosition = pos;
    }
    
    public bool IsSelected()
    {
        return selected;
    }

    public void SetSelected(bool selected)
    {
        this.selected = selected;
    }

    public virtual void Compile()
    {
        
    }
}
