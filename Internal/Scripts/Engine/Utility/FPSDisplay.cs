using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnigmaEngine;

public class FPSDisplay : MonoBehaviour
{
    public TextMeshProUGUI FPS;

    private float pollingTime = 2f;
    private float time;
    private int frameCount;
        
    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        frameCount++;
        
        if(time >= pollingTime)
        {
            int frameRate = Mathf.RoundToInt(frameCount / time);
            FPS.text = frameRate.ToString() + " FPS<br>" + UnigmaSpaceTime.Instance.temperatureSample.ToString("F1") + "F";
            time -= pollingTime;
            frameCount = 0;
        }
    }
}
