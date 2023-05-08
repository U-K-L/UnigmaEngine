using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleScreenUI : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject buttonPrefab;
    public int buttons = 0;

    private string currentLevel = "Battle_Scene";
    void Start()
    {
        CreateButtons();
        this.transform.localPosition = new Vector3(13.835f, 2.439f, -2.44f);
        this.transform.localRotation = new Quaternion(-0.18834f, 0.44261f, -0.09565f, -0.87148f);
        Debug.Log(this.transform.localRotation);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(this.transform.localRotation);
    }

    void CreateButtons()
    {
        for (int i = 0; i < buttons; i++)
        {
            GameObject obj = GameObject.Instantiate(buttonPrefab, transform);
            obj.transform.position = new Vector3(0, obj.transform.position.y - (i * 1.15f) - 1, obj.transform.position.z);
            switch (i)
            {
                case 0:
                    obj.GetComponent<MenuButtons>().buttonText = "Single-player";
                    obj.GetComponent<MenuButtons>().type = "Singleplayer";
                    break;
                case 1:
                    obj.GetComponent<MenuButtons>().buttonText = "Multiplayer";
                    obj.GetComponent<MenuButtons>().type = "Multiplayer";
                    break;
            }


        }
    }

    public string GetCurrentLevel()
    {
        return currentLevel;
    }
}
