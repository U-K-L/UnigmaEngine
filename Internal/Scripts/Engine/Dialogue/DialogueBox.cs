using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueBox : MonoBehaviour
{

    UnigmaAgent agent;
    public TextMeshPro text;
    int visibleCharacters = 0;
    public float textSpeed = 3;
    public GameObject target;
    public GameObject parent;
    public Vector3 originalPos;
    //Testing parameters
    public bool loopDialogue = true;
    // Start is called before the first frame update
    Queue<string> messages;
    bool displayingCharacters = false;
    void Start()
    {
        //text = Helper.FindComponentInChildWithTag<TextMeshPro>(gameObject, "ParentDialogue");
        messages = new Queue<string>();
        originalPos = transform.localPosition;
        agent = GetComponentInParent<UnigmaAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        //ChangeText(text.text);
        //InputManage();
        if (messages.Count > 0)
        {
            if (displayingCharacters)
                ReadText();
        }
        else
        {
            if(enabled && !displayingCharacters)
                Deactivate();
        }
        UpdateRotation();
    }

    private void OnEnable()
    {
        if (messages != null)
        {
            if (messages.Count > 0)
            {
                StopCoroutine(Expand());
                StopCoroutine(Shrink());
                StartCoroutine(Expand());
            }
        }


    }

    public void Deactivate()
    {
        StopCoroutine(Shrink());
        StopCoroutine(Expand());
        StartCoroutine(Shrink());
    }

    void UpdateRotation()
    {
        //transform.LookAt(Camera.main.transform.position, Vector3.up);
        Vector3 Ceuler = Camera.main.transform.rotation.eulerAngles;
        Vector3 Teuler = gameObject.transform.rotation.eulerAngles;
        Vector3 swizzle = new Vector3(Teuler.x, Ceuler.y, Ceuler.z);
        gameObject.transform.rotation = Quaternion.Euler(swizzle);
        transform.localPosition = originalPos + target.transform.localPosition;
        //float angle = Quaternion.Angle(Camera.main.transform.rotation, transform.rotation);
        //transform.RotateAround(target.transform.position, Vector3.up, angle);
    }

    /*
    private void LateUpdate()
    {
        //transform.LookAt(Camera.main.transform.position);
        //transform.forward = Camera.main.transform.forward;
        transform.rotation = Quaternion.LookRotation( agent.agentTrans.transform.position - Camera.main.transform.position );
    }
    */
    //Changes text
    public void ChangeText(string words)
    {
        if (text != null)
        {
            text.text = words;
        }
        else
            text = Helper.FindComponentInChildWithTag<TextMeshPro>(gameObject, "DialogueBox");

    }

    //0 stop, .. , 3 normal, .., 5 max.
    public void ReadText(float speed = 3f)
    {
        textSpeed = speed;
        StopCoroutine("displayCharacters");
        visibleCharacters = 0;
        text.maxVisibleCharacters = visibleCharacters;
        StartCoroutine("displayCharacters");
    }

    public void AddText(string stringtext)
    {
        if (!messages.Contains(stringtext))
        {
            messages.Enqueue(stringtext);
            enabled = true;
            gameObject.SetActive(true);
            displayingCharacters = true;

        }
    }

    IEnumerator displayCharacters()
    {
        displayingCharacters = true;
        string currentMessage = messages.Dequeue();
        text.text = currentMessage;
        while (visibleCharacters < currentMessage.Length)
        {
            yield return new WaitForSeconds(2 / (7 * (textSpeed + 0.5f)));
            visibleCharacters += 1;
            text.maxVisibleCharacters = visibleCharacters;

            if (loopDialogue && visibleCharacters == currentMessage.Length)
                visibleCharacters = 0;
        }
        yield return new WaitForSeconds(2);
        displayingCharacters = false;
        text.text = "";
        StopCoroutine("displayCharacters");

    }

    void InputManage()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            ReadText(textSpeed);
        }
    }

    IEnumerator Expand()
    {
        while (Vector3.Distance(gameObject.transform.localScale, Vector3.one) > 0.01f)
        {
            Debug.Log("expanding");
            gameObject.transform.localScale = Vector3.Lerp(gameObject.transform.localScale, Vector3.one, Time.deltaTime * 6f);
            yield return new WaitForSeconds(0.055f * Time.deltaTime);
        }
    }

    IEnumerator Shrink()
    {
        enabled = false;
        while (Vector3.Distance(gameObject.transform.localScale, Vector3.zero) > 0.01f)
        {
            Debug.Log("shrinking");
            gameObject.transform.localScale = Vector3.Lerp(gameObject.transform.localScale, Vector3.zero, Time.deltaTime * 9.25f);
            yield return new WaitForSeconds(0.055f * Time.deltaTime);
        }

        gameObject.SetActive(false);
        

    }
}
