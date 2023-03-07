using Shapes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Start_UIMagika : MagikaPP_SyntaxNode{
    private float[] weights; //used for tracks.
    public PolygonPath path;
    private int _currentAmountOfTracks = 0;
    public Start_UIMagika()
    {
        type = "start";
        _active = true;        
        path = new PolygonPath();
        Vector2 v1 = new Vector2(0.7f, 1f);
        Vector2 v2 = new Vector2(0f, 1f);
        Vector2 v3 = new Vector2(-1.25f, 0f);
        Vector2 v4 = new Vector2(0f, -1f);
        Vector2 v5 = new Vector2(0.7f, -1f);
        Vector2 v6 = new Vector2(-0.3f, 0f);
        path.AddPoints(v1, v2, v3, v4, v5, v6);
        
        weights = new float[50];
        _edges = new MagikaPP_Edge[1];
        _edges[0] = new MagikaPP_Edge();
    }
    public override void DrawShape(bool reset, Camera cam, float animating, Transform transform)
    {
        float scale = 0.7f;
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
            // inner / outer
            float angleOffset = 0;
            float radius = 0.695f* scale;
            float thickness = 0.015f;
            Vector2 origin = Vector2.zero;
            float outlineThickness = 0.025f;
            float angStart = (0 + animating);
            float angEnd = (Mathf.PI / 2.45f + animating);
            UIAgentOptions.DrawRoundedArcOutline(origin, radius, thickness, outlineThickness, angStart, angEnd);

            //Another ring
            angleOffset = Mathf.PI / 2.45f;
            radius = 0.615f* scale;
            thickness = 0.015f;
            origin = Vector2.zero;
            outlineThickness = 0.025f;
            angStart = (0 - animating) + angleOffset;
            angEnd = (Mathf.PI / 4f - animating) + angleOffset;
            UIAgentOptions.DrawRoundedArcOutline(origin, radius, thickness, outlineThickness, angStart, angEnd);


            //Another ring
            angleOffset = Mathf.PI;
            radius = 0.615f* scale;
            thickness = 0.015f;
            origin = Vector2.zero;
            outlineThickness = 0.025f;
            angStart = (0 - (animating * 1.5f)) + angleOffset;
            angEnd = (Mathf.PI / 3 - (animating * 1.5f)) + angleOffset;
            UIAgentOptions.DrawRoundedArcOutline(origin, radius, thickness, outlineThickness, angStart, angEnd);


            //Another ring
            angleOffset = Mathf.PI;
            radius = 0.775f* scale;
            thickness = 0.0125f;
            origin = Vector2.zero;
            outlineThickness = 0.02f;
            angStart = (0 - (animating * 1.9f)) + angleOffset;
            angEnd = (Mathf.PI * 1.25f - (animating * 1.9f)) + angleOffset;
            UIAgentOptions.DrawRoundedArcOutline(origin, radius, thickness, outlineThickness, angStart, angEnd);

            //Outer layers.
            Draw.Radius = 0.715f* scale;
            Draw.Disc(Vector2.zero, DiscColors.Radial(new Color(40f / 255f, 207f / 255f, 249f / 255f, 1f), new Color(40f / 255f, 207f / 255f, 249f / 255f, 0f)));

            Draw.Radius = 0.535f* scale;
            Draw.Color = new Color(160f / 255f, 255f / 255f, 255f / 255f, 1f);
            Draw.Disc();

            //The core.
            Draw.Radius = 0.5f* scale;
            Draw.Color = new Color(22f / 255f, 219f / 255f, 154f / 255f, 1f);
            Draw.Disc();

            //Draw the buttom arc.
            angleOffset = Mathf.PI * 0.6195f;
            radius = 0.265f* scale;
            thickness = 0.0185f;
            origin = Vector2.zero;
            outlineThickness = 0.02f;
            angStart = 0 + angleOffset;
            angEnd = Mathf.PI * 1.75f + angleOffset;
            Draw.Color = new Color(240f / 255f, 247f / 255f, 249f / 255f, 1f);
            UIAgentOptions.DrawRoundedArcOutline(origin, radius, thickness, outlineThickness, angStart, angEnd);

            //Draw Line in center.
            Draw.Line(new Vector2(0, 0.01f), new Vector2(0, 0.31f));
            Set3DPosition(Draw.Position);
        }
    }

    public override void DrawCell(bool reset, Camera cam, float animating, Vector2 pos, Vector4 offsetWidthHeight, float scale)
    {


        using (Draw.Command(cam))
        {

            //Set the settings for this node.
            Draw.Translate(pos + new Vector2(offsetWidthHeight.x, offsetWidthHeight.y));

            Draw.Scale(scale);
            //Draw rings
            Draw.Radius = 0.595f;
            Draw.Color = new Color(140f / 255f, 227f / 255f, 255f / 255f, 1f);
            Draw.DashStyle = DashStyle.defaultDashStyle;
            Draw.DashSpacing = 0.1f;
            Draw.BlendMode = ShapesBlendMode.Transparent;

            // inner / outer
            float angleOffset = 0;
            float radius = 0.655f;
            float thickness = 0.015f;
            Vector2 origin = Vector2.zero;
            float outlineThickness = 0.025f;
            float angStart = (0 + animating * 2.5f);
            float angEnd = (Mathf.PI / 2.45f + animating * 2.5f);
            UIAgentOptions.DrawRoundedArcOutline(origin, radius, thickness, outlineThickness, angStart, angEnd);

            //Outer layers.
            Draw.Radius = 0.715f;
            Draw.Disc(Vector2.zero, DiscColors.Radial(new Color(40f / 255f, 207f / 255f, 249f / 255f, 1f), new Color(40f / 255f, 207f / 255f, 249f / 255f, 0f)));

            Draw.Radius = 0.535f;
            Draw.Color = new Color(160f / 255f, 255f / 255f, 255f / 255f, 1f);
            Draw.Disc();

            //The core.
            Draw.Radius = 0.5f;
            Draw.Color = new Color(22f / 255f, 219f / 255f, 154f / 255f, 1f);
            Draw.Disc();

            //Draw the buttom arc.
            angleOffset = Mathf.PI * 0.6195f;
            radius = 0.265f;
            thickness = 0.0185f;
            origin = Vector2.zero;
            outlineThickness = 0.02f;
            angStart = 0 + angleOffset;
            angEnd = Mathf.PI * 1.75f + angleOffset;
            Draw.Color = new Color(240f / 255f, 247f / 255f, 249f / 255f, 1f);
            UIAgentOptions.DrawRoundedArcOutline(origin, radius, thickness, outlineThickness, angStart, angEnd);

            //Draw Line in center.
            Draw.Line(new Vector2(0, 0.01f), new Vector2(0, 0.31f));

            SetCellPosition(pos + new Vector2(offsetWidthHeight.x, offsetWidthHeight.y));
            Set3DPosition(Draw.Position);
        }
    }

    public override void DrawConnections(Camera cam, Transform transform, MagikaPP_Edge edge)
    {
        base.DrawConnections(cam, transform, edge);
        using (Draw.Command(cam))
        {
            Draw.ResetAllDrawStates();
            Draw.Position = Vector3.zero;
            Draw.ZTest = CompareFunction.Always;
            Draw.Matrix = transform.localToWorldMatrix;
            Draw.Position2D += GetPosition();
            Draw.BlendMode = ShapesBlendMode.Transparent;

            //Draw line connecting the connector.
            //Draw the track

            //Track slices
            Vector2 finalTargetPos = edge.GetPosition();

            if (edge.edge.target.GetInstantiation())
                finalTargetPos = edge.edge.target.GetTargetPosition()-GetPosition();

            float distance = Vector2.Distance(Vector2.zero, finalTargetPos);
            float increments = 0.35f; //Distance between each slice.
            int slicesCount = (int)(distance / increments);

            float timeForTrack = (int)(Time.time*7f) % (slicesCount+1); //Time.time is in seconds.

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

                weights[i] += Time.deltaTime*0.25f;

                DrawTrack(cam, edge, weights[i], finalTargetPos);
            }

        }
    }

    public void DrawTrack(Camera cam, MagikaPP_Edge edge, float weight, Vector2 pos)
    {
        using (Draw.Command(cam))
        {
            //rgba(0, 1/255f, 1/124f, 1);
            Color colorS = new Vector4(0f, 255f / 255f, 255f / 124f, 1f);
            Color colorE = new Vector4(0.65f, 0.25f, 0.55f, 0.55f);            

            //Get the line and rotate accordingly.
            Vector2 p1 = Vector2.zero; //The position of the node.
            Vector2 p2 = p1 + pos; //The position of the edge header.
            
            //Get the angle between the two points.
            float angle = Mathf.Atan2(p1.y - p2.y, p1.x - p2.x);

            //initial rotation to point down.
            Draw.Rotate(angle);
            
            Vector2 scaleTrack = new Vector2(0.2f, 0.3f);
            Vector2 scaleTrackInverse = new Vector2(1/ scaleTrack.x, 1/ scaleTrack.y);
            Draw.Scale(scaleTrack);

            Draw.GradientFill = GradientFill.defaultFill;

            Color colorInterp = Color.Lerp(colorS, colorE, weight); 


            Draw.Polygon(path, colorInterp);
            
            Draw.Scale(scaleTrackInverse);
        }
    }

    public override Vector2 GetTargetPosition()
    {
        return GetPosition();
    }
}
