using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stackable : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform _baseObj;
    [Range(0, 1)]
    private Rigidbody rb;
    private Vector3 v = Vector3.zero;
    private Collider _collider;
    public Vector3 centerOfMass;
    [Range(0.00001f, 10)]
    public float frequency = 0.05f;
    [Range(0.000001f, 100)]
    public float half_life = 3f;
    private Vector4 forceFeedback = Vector4.zero;
    public float elasticity = 1.0f;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        ConstraintAtPoint(_baseObj.position);
        FaceTowardsParent();
    }

    void ConstraintAtPoint(Vector3 constraint_point)
    {
        //Positional constraint.
        float dt = Time.deltaTime;
        float omega = 2 * Mathf.PI * frequency;
        float zetta = -Mathf.Log(0.5f) / (omega * half_life * 10);
        float damping = 2 * rb.mass * zetta * omega;
        float spring = rb.mass * omega * omega;
        float beta = (dt * spring) / (damping + dt * spring);
        float gamma = 1 / (damping + dt * spring);

        //Rotation constraint.
        Vector3 r = transform.rotation * (centerOfMass);
        Vector3 cPos = (transform.position + r) - constraint_point;
        Vector3 cVel = v + Vector3.Cross(rb.angularVelocity, r);

        float massInv = 1.0f / rb.mass;
        Matrix4x4 inertia = SolidBox(rb.mass, transform.localScale);


        Matrix4x4 s = Skew(r);
        Matrix4x4 k = add(mul(massInv, Matrix4x4.identity), s * inertia * s.transpose); //add inertia inverse.
        Matrix4x4 effectiveMass = k.inverse;
        Vector4 lambda = effectiveMass * (-(cVel + (beta / dt) * cPos)) + forceFeedback * gamma; //Baumgarte Stabilization.

        //Velocity correction.
        v += massInv * new Vector3(lambda.x, lambda.y, lambda.z);

        Vector4 t = (inertia * s.transpose) * new Vector4(lambda.x, lambda.y, lambda.z, 0.0f);
        Vector3 rotationalForce = new Vector3(t.x, t.y, t.z);
        Quaternion qatDiff = _baseObj.transform.rotation * Quaternion.Inverse(transform.rotation);
        Quaternion shortestPath = ShortestRotation(transform.rotation, _baseObj.transform.rotation);

        Vector3 rotDiff = new Vector3(qatDiff.x, qatDiff.y, qatDiff.z);

        Vector3 correctionalForce = -(new Vector3(shortestPath.x, shortestPath.y, shortestPath.z) * elasticity);
        rb.angularVelocity = rotationalForce + correctionalForce;

        v *= damping; //Damping factor
        rb.angularVelocity *= damping;

        //Integration
        transform.position += v * dt;
        Quaternion q = Quaternion.AxisAngle(Vector3.Normalize(rb.angularVelocity), rb.angularVelocity.magnitude * dt);
        Quaternion qI = Quaternion.Inverse(q);
        q *= transform.rotation;
        transform.rotation = q;
        forceFeedback = new Vector4(v.x, v.y, v.z, 0.0f);

    }

    void FaceTowardsParent()
    {
        Vector3 rotation = transform.rotation.eulerAngles;
        rotation.y = _baseObj.rotation.eulerAngles.y;
        transform.rotation = Quaternion.Euler(rotation);
    }

    Matrix4x4 Skew(Vector3 v)
    {
        Matrix4x4 s = new Matrix4x4();
        s.SetRow(0, new Vector4(0.0f, -v.z, v.y, 0.0f));
        s.SetRow(1, new Vector4(v.z, 0.0f, -v.x, 0.0f));
        s.SetRow(2, new Vector4(-v.y, v.x, 0.0f, 0.0f));
        s.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
        return s;
    }

    Matrix4x4 mul(float s, Matrix4x4 m)
    {
        Matrix4x4 result = new Matrix4x4();
        int index = 0;
        result.SetRow(index, new Vector4(s * m.GetRow(index).x, s * m.GetRow(index).y, s * m.GetRow(index).z, s * m.GetRow(index).w));
        index = 1;
        result.SetRow(index, new Vector4(s * m.GetRow(index).x, s * m.GetRow(index).y, s * m.GetRow(index).z, s * m.GetRow(index).w));
        index = 2;
        result.SetRow(index, new Vector4(s * m.GetRow(index).x, s * m.GetRow(index).y, s * m.GetRow(index).z, s * m.GetRow(index).w));
        index = 3;
        result.SetRow(index, new Vector4(s * m.GetRow(index).x, s * m.GetRow(index).y, s * m.GetRow(index).z, s * m.GetRow(index).w));

        return result;
    }

    Matrix4x4 add(Matrix4x4 a, Matrix4x4 m)
    {
        Matrix4x4 result = new Matrix4x4();
        int index = 0;
        result.SetRow(index, new Vector4(a.GetRow(index).x + m.GetRow(index).x, a.GetRow(index).y + m.GetRow(index).y, a.GetRow(index).z + m.GetRow(index).z, a.GetRow(index).w + m.GetRow(index).w));
        index = 1;
        result.SetRow(index, new Vector4(a.GetRow(index).x + m.GetRow(index).x, a.GetRow(index).y + m.GetRow(index).y, a.GetRow(index).z + m.GetRow(index).z, a.GetRow(index).w + m.GetRow(index).w));
        index = 2;
        result.SetRow(index, new Vector4(a.GetRow(index).x + m.GetRow(index).x, a.GetRow(index).y + m.GetRow(index).y, a.GetRow(index).z + m.GetRow(index).z, a.GetRow(index).w + m.GetRow(index).w));
        index = 3;
        result.SetRow(index, new Vector4(a.GetRow(index).x + m.GetRow(index).x, a.GetRow(index).y + m.GetRow(index).y, a.GetRow(index).z + m.GetRow(index).z, a.GetRow(index).w + m.GetRow(index).w));

        return result;
    }

    // https://en.wikipedia.org/wiki/List_of_moments_of_inertia
    // https://github.com/TheAllenChou/unity-physics-constraints/blob/master/src/Physics%20Constraints/Assets/Physics%20Constraints/Core/Inertia.cs
    public static Matrix4x4 SolidBox(float mass, Vector3 dimensions)
    {
        float oneTwelfth = 1.0f / 12.0f;
        float xx = dimensions.x * dimensions.x;
        float yy = dimensions.y * dimensions.y;
        float zz = dimensions.z * dimensions.z;
        Matrix4x4 m = new Matrix4x4();
        m.SetRow(0, new Vector4(oneTwelfth * mass * (yy + zz), 0.0f, 0.0f, 0.0f));
        m.SetRow(1, new Vector4(0.0f, oneTwelfth * mass * (xx + zz), 0.0f, 0.0f));
        m.SetRow(2, new Vector4(0.0f, 0.0f, oneTwelfth * mass * (xx + yy), 0.0f));
        m.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
        return m;
    }

    public Quaternion ShortestRotation(Quaternion a, Quaternion b)
    {
        if (Quaternion.Dot(a, b) < 0)
        {
            return a * Quaternion.Inverse(Multiply(b, -1));
        }
        else return a * Quaternion.Inverse(b);
    }



    public Quaternion Multiply(Quaternion input, float scalar)
    {
        return new Quaternion(input.x * scalar, input.y * scalar, input.z * scalar, input.w * scalar);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centerOfMass+transform.position, 0.1f);
    }
}
