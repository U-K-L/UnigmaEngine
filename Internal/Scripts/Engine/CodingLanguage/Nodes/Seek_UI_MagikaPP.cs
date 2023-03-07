using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Seek_UI_MagikaPP : MagikaPP_SyntaxNode
{
    private float[] weights; //used for tracks.
    public PolygonPath path;
    private int _currentAmountOfTracks = 0;
    private Seek_Settings_MagikaPP _settings = null;
    public Seek_UI_MagikaPP()
    {
        this.type = "seek";
        //Set the initial values of this node.
        this.MagikaNode = new Seek_MagikaPP("seek", "");
        this.MagikaNode.type = "seek";
        this.MagikaNode.AData = "icpu, -1";
        
        path = new PolygonPath();
        Vector2 v1 = new Vector2(0.7f, 1f);
        Vector2 v2 = new Vector2(0f, 1f);
        Vector2 v3 = new Vector2(-1.25f, 0f);
        Vector2 v4 = new Vector2(0f, -1f);
        Vector2 v5 = new Vector2(0.7f, -1f);
        Vector2 v6 = new Vector2(-0.3f, 0f);
        path.AddPoints(v1, v2, v3, v4, v5, v6);

        weights = new float[50];
        _edges = new MagikaPP_Edge[2];
        _edges[0] = new MagikaPP_Edge();
        _edges[1] = new MagikaPP_Edge();
    }

    void Start()
    {
        
    }

    public override void DrawShape(bool reset, Camera cam, float animating, Transform transform)
    {
        if (_settings == null)
            _settings = Seek_Settings_MagikaPP.GetInstance("UI/");
        using (Draw.Command(cam))
        {
            if (reset)
            {
                //Set the settings for this node.
                Draw.ResetAllDrawStates();
                Draw.Position = Vector3.zero;
                Draw.ZTest = CompareFunction.Always;
                Draw.Matrix = transform.localToWorldMatrix;
                Draw.Position2D += GetPosition();

                //Draw rings
                Draw.Radius = 0.595f;
                Draw.Color = new Color(140f / 255f, 227f / 255f, 255f / 255f, 1f);
                Draw.DashStyle = DashStyle.defaultDashStyle;
                Draw.DashSpacing = 0.1f;
                Draw.BlendMode = ShapesBlendMode.Transparent;
            }
            Draw.Scale(_settings.scale);
            //Draws the background circuit board shape many commands share.
            DrawBackgroundShape(animating);

            Draw.Scale(_settings.MagnifyingGlassMaster.z, _settings.MagnifyingGlassMaster.w);
            Draw.Position2D += new Vector2(_settings.MagnifyingGlassMaster.x, _settings.MagnifyingGlassMaster.y);
            //Draws the search icon.
            DrawSearchIcon(animating);

            Set3DPosition(Draw.Position);
        }
    }

    public override void DrawCell(bool reset, Camera cam, float animating, Vector2 pos, Vector4 offsetWidthHeight, float scale)
    {
        if(_settings == null)
            _settings = Seek_Settings_MagikaPP.GetInstance("UI/");

        using (Draw.Command(cam))
        {

            //Set the settings for this node.
            Draw.Translate(pos + new Vector2(offsetWidthHeight.x-0.115f, offsetWidthHeight.y - 0.1f));

            Draw.Scale(scale);
            Draw.Scale(_settings.scale);
            //Draws the background circuit board shape many commands share.
            DrawBackgroundShape(animating, true);

            Draw.Scale(_settings.MagnifyingGlassMaster.z, _settings.MagnifyingGlassMaster.w);
            Draw.Translate(new Vector2(1.1f, 2f));            

            //Draws the search icon.
            DrawSearchIcon(animating, true);
            SetCellPosition(pos + new Vector2(offsetWidthHeight.x, offsetWidthHeight.y));
            Set3DPosition(Draw.Position);
        }
    }

    public void DrawSearchIcon(float animating, bool ignoreActive = false)
    {
        float activeGlow = _settings.inactiveBrightness;
        if (_active || ignoreActive)
        {
            activeGlow = 1.0f;
        }
        
        //Draw Connector to handle.
        Draw.Color = _settings.HandleOutline;
        Draw.LineEndCaps = LineEndCap.Round;
        Draw.Line(_settings.HandleInnerStartEnd, new Vector2(_settings.HandleInnerStartEnd.z, _settings.HandleInnerStartEnd.w), 0.25f * _settings.HandleThickness);
        
        Draw.UseGradientFill = false;
        Draw.Radius = 1.0f * _settings.OutlineRadius;
        Draw.Color = _settings.HandleOutline * activeGlow;
        //Draw the outer most ring.
        Draw.Disc();

        //Draw the white ring.
        Draw.Color = _settings.HandleInner * activeGlow;
        Draw.Radius = 0.75f * _settings.WhiteOutlineRadius;
        Draw.Disc();

        //Outline for glass
        Draw.Color = _settings.HandleOutline* activeGlow;
        Draw.Radius = 0.65f * _settings.GlassOutlineRadius;
        Draw.Disc();

        //Draw the inner most magnifying glass.
        Draw.Color = _settings.Glass* activeGlow;
        Draw.Radius = 0.55f * _settings.GlassRadius;
        Draw.Disc();


        //Draw the handle
        Draw.Color = _settings.HandleOutline* activeGlow;
        Draw.Line(_settings.HandleStartEnd, new Vector2(_settings.HandleStartEnd.z, _settings.HandleStartEnd.w), 1.0f* _settings.HandleThickness);
        Draw.Line(_settings.HandleStartEnd, new Vector2(_settings.HandleStartEnd.z, _settings.HandleStartEnd.w), 1.0f* _settings.HandleThickness);
        Draw.Color = _settings.HandleInner* activeGlow;
        Draw.Line(_settings.HandleStartEnd, new Vector2(_settings.HandleStartEnd.z, _settings.HandleStartEnd.w), 0.55f* _settings.HandleThickness);
        
    }
    
    public void DrawBackgroundShape(float animating, bool ignoreActive = false)
    {
        float activeGlow = _settings.inactiveBrightness;
        GradientFill gradient = _settings.inactiveColorGradientSubMenu;
        if (_active || ignoreActive)
        {
            gradient = _settings.colorGradientSubMenu;
            activeGlow = 1.0f;
        }
        Draw.Color = _settings.ConnectorsColor * activeGlow;
        //Draw the Bars Connectors.
        float spacing = 0.25f;
        Draw.LineEndCaps = LineEndCap.Round;
        Draw.Line(new Vector2(-0.375f * _settings.HorizontalLinesLength, 0.5f + spacing), new Vector2(1.35f * _settings.HorizontalLinesLength, 0.5f + spacing), _settings.LineThickness);
        Draw.Line(new Vector2(-0.375f * _settings.HorizontalLinesLength, 0.5f), new Vector2(1.35f * _settings.HorizontalLinesLength, 0.5f), _settings.LineThickness);
        Draw.Line(new Vector2(-0.375f * _settings.HorizontalLinesLength, 0.5f - spacing), new Vector2(1.35f * _settings.HorizontalLinesLength, 0.5f - spacing), _settings.LineThickness);

        //Vertical
        Draw.Line(new Vector2(0.5f + spacing, -0.35f * _settings.HorizontalLinesLength), new Vector2(0.5f + spacing, 1.35f * _settings.HorizontalLinesLength), _settings.LineThickness);
        Draw.Line(new Vector2(0.5f - spacing, -0.35f * _settings.HorizontalLinesLength), new Vector2(0.5f - spacing, 1.35f * _settings.HorizontalLinesLength), _settings.LineThickness);
        Draw.Line(new Vector2(0.5f, -0.35f * _settings.HorizontalLinesLength), new Vector2(0.5f, 1.35f * _settings.HorizontalLinesLength), _settings.LineThickness);

        //Draw Borders
        Draw.UseGradientFill = true;
        Draw.GradientFill = gradient;
        Draw.Rectangle(_settings.BorderBackgroundOffsets, new Rect(Vector2.zero, Vector2.one  * 1.35f), 0.25f);

        //Draw White rounded square.
        Draw.UseGradientFill = false;
        Draw.Color = new Color(1, 1, 1, 1) * activeGlow;
        Draw.Rectangle(Vector2.zero, new Rect(_settings.WhiteBackgroundOffsets, Vector2.one * 1.15f), 0.15f);
        //Draw inner most square.
        Draw.UseGradientFill = true;
        Draw.GradientFill = gradient;
        Draw.Rectangle(Vector2.zero, new Rect(_settings.InnerOffsets, Vector2.one  * 1.08f), 0.15f);
        //Draw.Disc();
    }



    public override void DrawConnections(Camera cam, Transform transform, MagikaPP_Edge edge)
    {
        base.DrawConnections(cam, transform, edge);
        using (Draw.Command(cam))
        {
            for (int i = 0; i < _edges.Length; i++)
            {
                DrawConnection(cam, transform, _edges[i], i);
            }
        }
    }

    public void DrawConnection(Camera cam, Transform transform, MagikaPP_Edge edge, int index)
    {
        Draw.ResetAllDrawStates();
        Draw.Position = Vector3.zero;
        Draw.ZTest = CompareFunction.Always;
        Draw.Matrix = transform.localToWorldMatrix;
        Draw.Position2D += GetTargetPosition();
        Draw.BlendMode = ShapesBlendMode.Transparent;

        //Draw line connecting the connector.
        //Draw the track

        //Track slices
        Vector2 finalTargetPos = edge.GetPosition();

        if (edge.edge.target.GetInstantiation())
            finalTargetPos = edge.edge.target.GetPosition() - GetPosition();

        float distance = Vector2.Distance(Vector2.zero, finalTargetPos);
        float increments = 0.35f; //Distance between each slice.
        int slicesCount = (int)(distance / increments);

        float timeForTrack = (int)(Time.time * 7f) % (slicesCount + 1); //Time.time is in seconds.

        if (_currentAmountOfTracks != slicesCount)
        {
            _currentAmountOfTracks = slicesCount;

            //fill weights
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = 1.0f;
            }
        }

        //Draw Tracks
        for (int i = 0; i < slicesCount; i++)
        {
            Draw.Position2D += finalTargetPos * (1.0f / slicesCount);

            if (i == timeForTrack)
                weights[i] = 0.0f;

            weights[i] += Time.deltaTime * 0.25f;

            DrawTrack(cam, edge, weights[i], finalTargetPos, index);
        }

    }

    public void DrawTrack(Camera cam, MagikaPP_Edge edge, float weight, Vector2 pos, int index)
    {
        using (Draw.Command(cam))
        {
            //rgba(0, 1/255f, 1/124f, 1);
            Color colorS = new Vector4(0f, 255f / 255f, 255f / 124f, 1f);
            Color colorE = new Vector4(0.65f, 0.25f, 0.55f, 0.55f);

            //If it is the second track
            if (index > 0)
            {
                colorE = new Vector4(0.67f, 0.35f, 0.05f, 0.55f);
                colorS = new Vector4(255f / 255f, 
                                    46f / 255f, 
                                    79f / 255f, 1f);
            }
            //Get the line and rotate accordingly.
            Vector2 p1 = Vector2.zero; //+ new Vector2(0.5f, 0.5f); //The position of the node.
            Vector2 p2 = p1 + pos; //The position of the edge header.

            //Get the angle between the two points.
            float angle = Mathf.Atan2(p1.y - p2.y, p1.x - p2.x);

            //initial rotation to point down.
            Draw.Rotate(angle);

            Vector2 scaleTrack = new Vector2(0.2f, 0.3f);
            Vector2 scaleTrackInverse = new Vector2(1 / scaleTrack.x, 1 / scaleTrack.y);
            Draw.Scale(scaleTrack);

            Draw.GradientFill = GradientFill.defaultFill;

            Color colorInterp = Color.Lerp(colorS, colorE, weight);


            Draw.Polygon(path, colorInterp);

            Draw.Scale(scaleTrackInverse);
        }
    }

    public override MagikaPP_Edge GetConnection()
    {
        Debug.Log("Current Connection: " + currentConnection);
        MagikaPP_Edge edge = _edges[currentConnection];
        return edge;
    }

    public override Vector2 GetTargetPosition()
    {
        return GetPosition() + _settings.TrackConnectorOffsets;
    }
}
