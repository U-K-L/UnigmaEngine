using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Summon_Skill : AgentSkill
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
    }

    public override void executeSkill(Player_Controller_Agents controller, Agent_Summoner summoner)
    {


        //This is the summoning which the player selected. 
        GameObject objPrefab = Resources.Load("Characters/Pedestrian/Person") as GameObject;
        GameObject obj = Instantiate(objPrefab) as GameObject;
        AgentSummoning summon = obj.GetComponentInChildren<AgentSummoning>();

        //Assign key
        summon.key = summoner.key+","+summon.type + ",{" + summoner.GetSummonings().Count + "}";

        //Creates summoning and places them at the position of the cursor.
        obj.transform.position = controller.GetCursorPosition() + Vector3.up*10f;
        Vector3 rotation = obj.transform.rotation.eulerAngles;
        rotation.y = summoner.transform.rotation.eulerAngles.y;
        obj.transform.rotation = Quaternion.Euler(rotation);
        summoner.setCurrentSkills(summoner.skills);

        //Add this agent to the current summoner:
        summoner.AddSummoning(summon.key, summon);

    }
}
