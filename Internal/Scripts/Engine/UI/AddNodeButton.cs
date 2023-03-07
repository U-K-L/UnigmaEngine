using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddNodeButton : MonoBehaviour
{
    public void OnButtonPress()
    {
        //MagikaPP_SyntaxNode synNode = new MagikaPP_SyntaxNode();
        //MagikaPP_Lexer._syntaxRoots.Add(synNode);
        MagikaPPUI.SwitchStateToSubMenu();
    }
}
