using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crouching : StateMachineBehaviour
{
    AgentPhysics agent;
    private AnimationControllerSprites animationControllerSprites;
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.gameObject.GetComponent<AgentPhysics>();
        animationControllerSprites = animator.gameObject.GetComponent<AnimationControllerSprites>();
        animationControllerSprites.SetAnimation("Crouching");        
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log("Is Crouching");
        if (agent)
        {
            CheckAgentStates(agent, animator, stateInfo);
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}

    private void CheckAgentStates(AgentPhysics agent, Animator animator, AnimatorStateInfo stateInfo)
    {
        if (agent.getState() == AgentPhysics.StateMachine.jumping)
        {
            animator.Play("Jumping");
        }
        if (agent.getState() == AgentPhysics.StateMachine.idle)
        {
            animator.GetComponent<AnimationControllerSprites>().SetAnimation("IdleToCrouch_reverse");

            animator.Play("Idle");
        }
    }    
}
