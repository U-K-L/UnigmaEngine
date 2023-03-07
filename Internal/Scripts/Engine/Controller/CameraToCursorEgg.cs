using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraToCursorEgg : Player_Controller_Agents
{
    // Start is called before the first frame update
    private PlayerCursorEggGame _cursor;
    private Stack<AgentPhysics> _agentStack;
    private CameraController _cameraController;

    void Start()
    {
        _cursor = GetComponent<PlayerCursorEggGame>();
        _agentStack = new Stack<AgentPhysics>();
        GameObject parentUI = new GameObject();
        parentUI = Instantiate(parentUI, Vector3.zero, Quaternion.identity, gameObject.transform);
        parentUI.name = "ParentUI";
        parentUI.transform.localScale *= 10f;
        UIOptions = Instantiate(UIOptionsPrefab, Vector3.zero, Quaternion.identity, parentUI.transform);
        uiAgentOptions = UIOptions.GetComponent<UIAgentOptions>();
        drawingState = DrawingState.Idle;
        _cam = Camera.main;
        _cameraController = GameObject.FindGameObjectWithTag("CameraController").GetComponent<CameraController>();
    }

    // Update is called once per frame
    void Update()
    {
        //UpdateInputs();
        updateMenuInputs();
    }

    public override Vector3 GetPivot()
    {
        Debug.Log(_cursor.GetRayCastedHit());
        return _cursor.GetRayCastedHit();
    }

    void UpdateInputs()
    {
        
        if (Input.GetMouseButtonDown(0))
        {

            if (_cursor.GetCursorPointsToObject() != null)
            {
                AgentPhysics agentPhysics = _cursor.GetCursorPointsToObject().GetComponent<AgentPhysics>();
                if (agentPhysics != null)
                {
                    if (agentPhysics != null)
                        SelectAgent(agentPhysics);
                }

            }

        }
    }

    void updateMenuInputs()
    {
        //Deactivates menu if pressed while menu already opened.
        if (Input.GetButtonDown("MenuKeyboard"))
        {
            if (drawingState == DrawingState.Menu)
            {
                deactivateMenu();
                drawingState = DrawingState.Selecting;
            }
            else if (drawingState != DrawingState.Menu)
            {
                activateMenu();
                drawingState = DrawingState.Menu;

            }
        }
    }

    
    void SelectAgent(AgentPhysics agent)
    {
        
        if (_cursor.getSelectedObjects().ContainsKey(agent.ToString()))
        {
            _cursor.removeObjectFromList(agent);

        }
        else
        {
            //Add object, add to stack.
            _cursor.addObjectToList(agent);
            _agentStack.Push(agent);
        }

    }

    private void RemoveAllObjectsFromList()
    {
        _cursor.removeAllObjectFromList();
    }

    void activateMenu()
    {
        UIOptions.SetActive(true);
        uiAgentOptions.setController(this);
        if (_agentStack.Count > 0)
        {
            Debug.Log(_agentStack.Peek().name);
            uiAgentOptions.setAgent(_agentStack.Peek());
            _cameraController.moveCamera(_agentStack.Peek().transform.position);
        }
        else
            uiAgentOptions.setAgent(summoner);
        uiAgentOptions.activateMenu();

    }

    void deactivateMenu()
    {
        uiAgentOptions.setController(null);
        uiAgentOptions.deactivateMenu();
    }

    public override Vector3 GetCursorPosition()
    {
        return _cursor.GetRayCastedHit();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_cursor.GetRayCastedHit(), 0.5f);
    }

}
