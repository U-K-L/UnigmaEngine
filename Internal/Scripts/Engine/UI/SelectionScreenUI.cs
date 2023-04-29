using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionScreenUI : MonoBehaviour
{
    // Start is called before the first frame update
    List<EggStages> ListOfStages;
    List<SelectionNodes> stagesUI;
    bool glowOff = false;
    void Start()
    {
        ListOfStages = new List<EggStages>();
        EggStages[] stages = Resources.LoadAll<EggStages>("EggMaps/Stages");
        ListOfStages.AddRange(stages);
        stagesUI = new List<SelectionNodes>();
        DisplayStages();
        
        //DebugStageList();
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Camera.main.transform.rotation;
        if (!glowOff && Camera.main)
        {
            GlowComposite glow = Camera.main.transform.GetComponent<GlowComposite>();
            if (glow)
            {
                glow.enabled = false;
                glowOff = true;
            }
        }
    }

    void DisplayStages()
    {
        foreach (EggStages stage in ListOfStages)
        {
            GameObject obj = new GameObject(stage.stageName);
            obj.transform.parent = transform;
            obj.transform.position = Vector3.zero;
            SelectionNodes node = obj.AddComponent<SelectionNodes>();
            node.image = stage.stageIcon;
            stagesUI.Add(node);

        }
    }

    public void DebugStageList()
    {
        Debug.Log("Stages count: " + ListOfStages.Count);
        foreach (EggStages stage in ListOfStages)
        {
            Debug.Log(stage.stageName);
        }
    }
}
