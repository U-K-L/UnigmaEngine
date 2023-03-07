using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackHoleAnimation : MonoBehaviour
{
    // Start is called before the first frame update
    Animator animator;
    public GameObject bomb;
    bool animStarted;
    Rigidbody rigibody;
    void Start()
    {
        animator = GetComponent<Animator>();
        animator.enabled = false;
        rigibody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        if (!animStarted)
            CheckFloor();
    }

    void CheckFloor()
    {
        RaycastHit hit;
        Vector3 tailVec = bomb.transform.position + Vector3.down*bomb.transform.localScale.y*0.5f;
        Vector3 headVec = Vector3.down;
        float distance = 0.15f;
        if (Physics.Raycast(tailVec, headVec, out hit, distance))
        {
            if(hit.transform.gameObject.tag != "Explodable")
            {
                rigibody.isKinematic = true;
                animator.enabled = true;
                Debug.Log(hit.transform.gameObject.name);
                animStarted = true;
                animator.Play("Trigger");
            }

        }
        Debug.DrawRay(tailVec, headVec* distance);

    }
}
