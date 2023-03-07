using System.Collections;
using System.Collections.Generic;

public class MagikaPP_Interpreter
{
    MagikaPP_Parser _parser;
    MagikaPP_Node _current;
    public MagikaPP_Interpreter()
    {
        _parser = new MagikaPP_Parser();
        _current = _parser.nodes[0];//_parser.CreateAbstractSyntaxTree();
    }

    public void GetNextNode(AgentAI agent)
    {
        if (_current.GetNext() == null)
        {
            return;
        }
        _current = _current.GetNext();
    }

    public object[] InterpretNode(AgentAI agent)
    {
        object[] arguements = _current.InterpretNode(agent);
        return arguements;
    }

    public string Peek()
    {
        return _current.type;
    }

    public MagikaPP_Node GetCurrentNode()
    {
        return _current;
    }
}
