using MudBun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandingSmoke : MonoBehaviour
{
    private MudTorus Puff;

    public float Period = 1.0f;
    public float speed = 1f;

    public float MaxPuffRadius = 3.0f;
    public float MaxPuffSize = 1.0f;

    private float m_timer = 0.0f;
    public bool looping = false;

    void Start()
    {
        Puff = gameObject.GetComponentInChildren<MudTorus>();
        //originalScale = Puff.transform.localScale;
    }

    private void OnEnable()
    {

        if (Puff == null)
            Puff = gameObject.GetComponentInChildren<MudTorus>();
        m_timer = 0f;
    }
    private void OnDisable()
    {
        m_timer = 0f;
    }
    public void Update()
    {
        m_timer += Time.deltaTime*speed;

        float t = m_timer / Period;


        Puff.transform.localScale = new Vector3(MaxPuffRadius * (t + 0.2f) / 1.2f, 1.0f, MaxPuffRadius * (t + 0.2f) / 1.2f);

        if (t < 0.5f)
        {
            Puff.Radius = MaxPuffSize * ((t + 0.4f) / 0.9f);
        }
        else
        {
            Puff.Radius = MaxPuffSize * (1.0f - t) * 2.0f;
        }

        if (looping)
            m_timer = Mathf.Repeat(m_timer, Period);
        else if (m_timer > 1.01f)
        {
            gameObject.SetActive(false);
            this.enabled = false;
        }
    }
    /*
    // Start is called before the first frame update
    private MudTorus Puff;
    public float speed;
    public float maxSize;
    public float maxRadius;
    public float threshold = 0.75f;
    Vector3 originalScale;

    private float delta = 0.0f;
    void Start()
    {
        Puff = gameObject.GetComponentInChildren<MudTorus>();
        originalScale = Puff.transform.localScale;
    }

    private void OnEnable()
    {
        
        if (Puff == null)
            Puff = gameObject.GetComponentInChildren<MudTorus>();
        Puff.Radius = maxRadius*0.5f;

    }

    private void OnDisable()
    {
        Puff.Radius = 0f;
        Puff.transform.localScale = Vector3.one;
        delta = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(delta);
        //Puff.Radius = Mathf.Lerp(Puff.Radius, maxRadius, Time.deltaTime * speed);

        delta += Time.deltaTime * speed;

        if (delta < threshold)
        {
            Puff.Radius = Mathf.Lerp(Puff.Radius, maxRadius, Time.deltaTime * speed);
            Puff.transform.localScale = Vector3.Lerp(Puff.transform.localScale,
                new Vector3(originalScale.x * maxSize, originalScale.y, originalScale.z * maxSize), Time.deltaTime * speed);
        }
        else
        {
            Puff.Radius = Mathf.Lerp(Puff.Radius, 0, Time.deltaTime * speed);
            Puff.transform.localScale = Vector3.Lerp(Puff.transform.localScale,
                new Vector3(originalScale.x * maxSize, originalScale.y, originalScale.z * maxSize), Time.deltaTime * speed*speed);
        }

        if (Puff.Radius < 0.2f)
            Puff.Radius = 0f;

    }
    */
}
