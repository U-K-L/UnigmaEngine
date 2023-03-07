using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;

public class TrajectoryVisual : ImmediateModeShapeDrawer
{
    public struct TrajectoryPoint
    {
        public Vector3 position;
        public int hit;
        public Vector3 normal;
        public Vector3 hitPos;
    }
    public GameObject linePrefab;
    private LineRenderer _lineRenderer;
    public int lineSegments = 10;
    private List<TrajectoryPoint> points;
    private AgentPhysics _agent;
    private PlayerCursor _cursor;
    private float[] weights;
    bool updateTrajectory = false;
    // Start is called before the first frame update
    void Start()
    {
        weights = new float[lineSegments];
        _lineRenderer = Instantiate(linePrefab, gameObject.transform).GetComponent<LineRenderer>();
        points = new List<TrajectoryPoint>();
        _agent = GetComponent<AgentPhysics>();
        _cursor = GameObject.FindGameObjectWithTag("GameManager").GetComponent<PlayerCursor>();

        //fill weights
        for (int j = 0; j < weights.Length; j++)
        {
            weights[j] = 1.0f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (AgentPhysics.StateMachine.crouching == _agent.getState())
        {
            UpdateTrajectory();
            updateTrajectory = true;
        }
        else if(AgentPhysics.StateMachine.airborne != _agent.getState() && _agent.getState() != AgentPhysics.StateMachine.jumping)
            updateTrajectory = false;
    }

    void UpdateTrajectory()
    {
        points.Clear();
        
        Vector3 pos = gameObject.transform.position;
        Vector3 target = _cursor.GetRayCastedHit();

        bool collided = false;

        Vector3 dir = (target - pos).normalized;

        float distance = Vector3.Distance(pos, target);
        distance = Mathf.Clamp(distance, 0.0f, _agent.jumpRadius);

        Vector3 targetPositionConstrained = pos + dir * distance;

        Vector3 vel = JumpVelocity(targetPositionConstrained);
        //Vector3 constrainedPosition = 

        float dist = Vector3.Distance(pos, targetPositionConstrained) * 2f;
        for (int i = 0; i < lineSegments; i++)
        {
            TrajectoryPoint point = new TrajectoryPoint();

            float drag = 1.0f - (_agent._mbody.drag * Time.fixedDeltaTime);
            vel *= drag;
            float timestep = Time.fixedDeltaTime * i; //* ((dist * i + 2) / lineSegments);
            if (vel.y < 0.0f)
            {
                vel += 0.85f * Physics.gravity * Time.fixedDeltaTime;
            }
            pos = JumpTimeStamp(timestep, vel, gameObject.transform.position, i);



            point.position = pos;
            if (collided == false)
            {
                if (DidTrajectoryHitObject(ref point) == true)
                {
                    //gameObject.transform.position;
                    collided = true;
                    points.Add(point);
                }
                else
                {
                    points.Add(point);
                }
            }
            else
            {
                point.position = Vector3.zero;
                points.Add(point);
            }

        }
    }

    bool DidTrajectoryHitObject(ref TrajectoryPoint point)
    {
        Vector3 pos = point.position;
        Collider[] hitColliders = Physics.OverlapSphere(pos, 0.1f);
        //Debug all the colliders
        foreach (var hitCollider in hitColliders)
        {
        }
        if (hitColliders.Length > 0)
        {
            
            if (Vector3.Distance(hitColliders[0].gameObject.transform.position, gameObject.transform.position) > 1f)
            {
                point.hit = 1;
                point.hitPos = pos;

                Vector3 direction = (pos - hitColliders[0].gameObject.transform.position).normalized;

                RaycastHit hit;

                if (Physics.Raycast(pos, direction, out hit, 5f))
                {
                    point.normal = hit.normal;
                }                    
                
                return true;
            }
        }
        return false;
    }

    Vector3 JumpVelocity(Vector3 target)
    {

        float g = Physics.gravity.y;
        float epsilon = 1f; //+ (_agent.fallMultiplier * 2f);
        Vector3 currentPos = _agent._collider.transform.position;
        Vector3 deltaPos = target - currentPos;
        


        //Ensures height is large enough to reach target.

        float deltaHeight = Vector3.Distance(currentPos, target) + 8f;

        float drag = 1.0f - (_agent._mbody.drag * Time.fixedDeltaTime);
        
        float time = Mathf.Sqrt(2 * deltaHeight / Mathf.Abs(g));

        
        float jumpVel = Mathf.Sqrt(-2f * g * deltaHeight);
        
        Vector3 vel = ((deltaPos + 0.5f * Physics.gravity * time * time + (_agent._mbody.drag * drag * deltaPos * time) + (0.125f * Physics.gravity * Time.fixedDeltaTime * time * time) + (deltaPos * time * 0.125f)) / time) * epsilon + _agent.contactNormal * jumpVel;

        Vector3 finalPos = new Vector3(vel.x, vel.y, vel.z);

        

        return finalPos;
    }
    
    Vector3 JumpTimeStamp(float dt, Vector3 vel, Vector3 pos, int iteration)
    {
        //vel = vel * Mathf.Pow(1.0f - _agent._mbody.drag * (float)(dt)/(float)iteration, iteration);

        pos += (vel * dt) + (0.5f * Physics.gravity *dt *dt);

        //float sY = (vel.y * dt) + (-0.5f * Mathf.Abs(Physics.gravity.y) * dt * dt) + pos.y;
        //pos.y = sY;
        return pos;
    }

    public override void DrawShapes(Camera cam)
    {
        if (updateTrajectory == false)
            return;
        for ( int i = 0; i < points.Count; i++) 
        {
            //float distance = Vector2.Distance(Vector2.zero, finalTargetPos);
            int slicesCount = lineSegments;

            float timeForTrack = (int)(Time.time * 15f) % (slicesCount + 1); //Time.time is in seconds.
            //Draw Spheres
            
            if (i == timeForTrack)
                weights[i] = 0.0f;
            
            weights[i] += Time.deltaTime * 0.25f;
            //DrawMenuDot(cam, point);
            if(i % 2 == 0)
                DrawSphere(cam, weights[i], points[i]);
        }

    }

    public void DrawSphere(Camera cam, float weight, TrajectoryPoint point)
    {
        using (Draw.Command(cam))
        {
            //rgba(0, 1/255f, 1/124f, 1);
            Color colorS = new Vector4(0f, 255f / 255f, 255f / 124f, 1f);
            Color colorE = new Vector4(0.65f, 0.25f, 0.55f, 0.55f);

            Draw.GradientFill = GradientFill.defaultFill;

            Color colorInterp = Color.Lerp(colorS, colorE, weight);
            
            Draw.Sphere(point.position, 0.1f, colorInterp);
            
        }
    }

    void DrawMenuDot(Camera cam, TrajectoryPoint point)
    {
        using (Draw.Command(cam))
        {
            Draw.ZTest = UnityEngine.Rendering.CompareFunction.Always;
            Draw.Radius = 0.2f;
            //Draw.BlendMode = ShapesBlendMode.Opaque;
            if (point.hit == 1)
            {
                Draw.Color = new Color(1f, 1f, 0.85f, 0.65f);
                Draw.Disc(point.hitPos, Vector3.Cross(point.normal, new Vector3(point.normal.z, point.normal.y, point.normal.x) ), 0.5f);
            }
            else
            {
                Draw.Color = new Color(0.75f, 0.42f, 0.55f, 0.65f);
                Draw.Sphere(point.position);
            }
            
        }
    }

    private void OnDrawGizmos()
    {
        /*
        Gizmos.color = Color.red;
        //Draw a sphere for each point
        foreach (TrajectoryPoint point in points)
        {
            Gizmos.DrawSphere(point.position, 0.1f);
        }
        */
    }
}
