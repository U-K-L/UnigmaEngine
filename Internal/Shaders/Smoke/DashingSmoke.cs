using MudBun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashingSmoke : MonoBehaviour
{
    public MudNoiseVolume noise;
    public bool on = false;
    void Start()
    {
        noise = GetComponentInChildren<MudNoiseVolume>();
        noise.enabled = false;
    }

    void Update()
    {
        
    }

    public void setRotation(Vector3 vel)
    {
        Vector3 direction = vel.normalized;
        float strength = vel.magnitude;

        Quaternion velRot = Quaternion.LookRotation(-direction);
        this.gameObject.transform.parent.rotation = Quaternion.Slerp(this.gameObject.transform.parent.rotation,velRot, Time.deltaTime*10f);
    }

    public void TurnOn()
    {
        StartCoroutine(killSmoke());
        on = true;
        noise.enabled = true;
    }

    public void TurnOff()
    {
        on = false;
        noise.Threshold = 0;
        noise.enabled = false;
    }

    IEnumerator killSmoke()
    {
        while (noise.Threshold < 1)
        {
            noise.Threshold += Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }
        TurnOff();
    }
}
