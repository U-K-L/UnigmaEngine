using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class MagikaPP_Lexer
{
    public static List<MagikaPP_SyntaxNode> _syntaxRoots = new List<MagikaPP_SyntaxNode>();

    public static void SaveCode(List<MagikaPP_SyntaxNode> AST)
    {
        // Save code to file
        string path = Application.streamingAssetsPath + "/MagikaCode/" + "SeekCodeC.txt";
        string fullCode = "";
        foreach (MagikaPP_SyntaxNode node in AST)
        {
            if (node.type == "start")
            {
                fullCode += WriteASTFile(path, node);
                fullCode += "-2,end,-2\n";
            }                
        }
        
        try
        {
            File.WriteAllText(path, fullCode);
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
        }
        
    }

    public static string WriteASTFile(string path, MagikaPP_SyntaxNode node)
    {
        HashSet<int> visited = new HashSet<int>();
        int id = 0;
        node.MagikaNode.ID = id;
        Stack<MagikaPP_SyntaxNode> stack = new Stack<MagikaPP_SyntaxNode>();
        stack.Push(node);

        string code = "0,start,1\n";
        //Depth First Search
        while (stack.Count > 0)
        {
            MagikaPP_SyntaxNode curr = stack.Pop();
            curr.MagikaNode.ID_Children.Clear();
            Debug.Log(curr.MagikaNode.ID);
            if (visited.Contains(curr.MagikaNode.ID))
            {
                continue;
            }
            else
                visited.Add(curr.MagikaNode.ID);
            

            foreach (MagikaPP_Edge edge in curr._edges)
            {
                id++;
                stack.Push(edge.edge.target);
                
                if(edge.edge.target.MagikaNode.ID == -2)
                    edge.edge.target.MagikaNode.ID = id;
                if(curr.MagikaNode.ID_Children != null)
                    curr.MagikaNode.ID_Children.Add(edge.edge.target.MagikaNode.ID);
            }
            code += curr.MagikaNode.Compile();
        }

        //Write to file
        return code;
    }
}
