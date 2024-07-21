using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnigmaGravityAttractor : MonoBehaviour
{
    public float strength;
    public Vector3 direction;

    public enum GravityType {Uniform, Attract, Repulse}

    public GravityType gravityType = GravityType.Uniform;

    //Make this a singleton
    UnigmaSpaceTime SpaceTimeVectorField;
    private void FixedUpdate()
    {
        if (SpaceTimeVectorField == null)
            SpaceTimeVectorField = GameObject.FindGameObjectWithTag("GameManager").GetComponent<UnigmaSpaceTime>();

        //UpdateVectorField();
    }

    void UpdateVectorField()
    {
        if (gravityType == GravityType.Uniform)
            UpdateUniformly();
        if (gravityType == GravityType.Attract)
            UpdateAttract();
        if (gravityType == GravityType.Repulse)
            UpdateRepulse();
    }

    void UpdateUniformly()
    {
        for (int i = 0; i < SpaceTimeVectorField.VectorField.Length; i++)
        {
            SpaceTimeVectorField.VectorField[i].force += Vector3.Normalize(direction) * strength;
        }
    }

    void UpdateAttract()
    {
        for (int i = 0; i < SpaceTimeVectorField.VectorField.Length; i++)
        {

            float distance = 1.0f / Mathf.Pow(Vector3.Distance(SpaceTimeVectorField.VectorField[i].position, transform.position), 2);
            Vector3 toObjDir = Vector3.Normalize(transform.position - SpaceTimeVectorField.VectorField[i].position);

            SpaceTimeVectorField.VectorField[i].force += toObjDir * strength * Mathf.Min(distance, strength*20);

        }
    }

    void UpdateRepulse()
    {

    }
}
