using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSPHSim : MonoBehaviour
{
    // Start is called before the first frame update
    public int height;
    public int width;
    public int length;
    public WaterParticle prefab;
    List<WaterParticle> particles;
    float HSQ;
    public float mass = 65.5f;
    public float GAS_DENS = 2000f;
    public float REST_DENS = 1000f;
    public float SPIKY_GRAD = 0.5f;
    public float VISC = 0.5f;
    public float VISC_LAP = 0.5f;
    public float H = 0.5f;
    public float EPS = 0.2f;
    public float POLY6;

    void Start()
    {
        HSQ = height * height;
        //POLY6 = 315.0f / (65.0f * Mathf.PI * Mathf.Pow(H, 9f));
        //SPIKY_GRAD = -45.0f / (Mathf.PI * Mathf.Pow(H, 6));
        //VISC_LAP = 45.0f / (Mathf.PI * Mathf.Pow(H, 6));
        particles = new List<WaterParticle>();
        InitSPH();

    }

    // Update is called once per frame
    void Update()
    {
        ComputeDensityPressure();
        ComputeForces();
        IntegrateForces();
    }

    void InitSPH()
    {
        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                for(int z = 0; z < length; z++)
                {
                    particles.Add(Instantiate(prefab, new Vector3(x,y,z)+gameObject.transform.position, gameObject.transform.rotation, gameObject.transform));
                }
            }
        }
    }

    void ComputeDensityPressure()
    {
        foreach(WaterParticle pi in particles){
            pi.density = 0.0f;
            foreach(WaterParticle pj in particles)
            {
                Vector3 rij = pj.transform.position - pi.transform.position;
                float r2 = rij.sqrMagnitude;
                if(r2 < HSQ)
                {
                    pi.density += POLY6*mass * Mathf.Pow(HSQ - r2, 3.0f);
                }
            }
            pi.pressure = GAS_DENS*(pi.density-REST_DENS);
        }
    }

    void ComputeForces()
    {
        foreach(WaterParticle pi in particles)
        {
            Vector3 fpress = new Vector3(0, 0, 0);
            Vector3 fvisc = new Vector3(0, 0, 0);
            foreach (WaterParticle pj in particles)
            {
                if (pi == pj)
                    continue;
                Vector3 rij = pi.transform.position - pj.transform.position;
                float r = rij.sqrMagnitude;
                if(r < H)
                {
                    fpress += Vector3.Cross(-rij.normalized, mass * (pi.transform.position + pj.transform.position)) / (2 * pj.density) * SPIKY_GRAD * Mathf.Pow(H - r, 2);
                    fvisc += VISC * mass * (pj.velocity - pi.velocity) / pj.density * VISC_LAP * (H - r);
                }
            }
            Vector3 fgrav = Physics.gravity * pi.density;
            pi.force = fpress + fvisc + fgrav;
        }
    }

    void IntegrateForces()
    {
        foreach(WaterParticle particle in particles)
        {
            //Forward integreation.
            Rigidbody rb = particle.GetComponent<Rigidbody>();
            rb.velocity += Time.deltaTime * particle.force / particle.density;
            rb.AddForce(Time.deltaTime * rb.velocity);
        }
    }
}
