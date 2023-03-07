using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Seek_Command", menuName = "Magika")]
public class Seek_Settings_MagikaPP : SingletonScriptableObject<Seek_Settings_MagikaPP>
{
    
    public string name;
    public string description;

    //For the graphics of the nodes.
    [Header("Graphics")]
    public float inactiveBrightness = 1.0f;
    public GradientFill inactiveColorGradientSubMenu;
    public GradientFill colorGradientSubMenu;   
    public Color ConnectorsColor;
    public float scale = 1.0f;
    public Vector2 WhiteBackgroundOffsets;
    public Vector2 BorderBackgroundOffsets;
    public Vector2 InnerOffsets;

    public float HorizontalLinesLength = 1.0f;
    public float LineThickness = 1.0f;

    public Color HandleOutline;
    public Color HandleInner = new Vector4(1, 1, 1, 1);
    public Color Glass;
    public Vector4 HandleStartEnd;
    public Vector4 HandleInnerStartEnd;

    public float OutlineRadius = 1.0f;
    public float WhiteOutlineRadius = 1.0f;
    public float GlassOutlineRadius = 1.0f;
    public float GlassRadius = 1.0f;

    public float HandleThickness = 1.0f;

    public Vector4 MagnifyingGlassMaster;

    public Vector2 TrackConnectorOffsets;
}
