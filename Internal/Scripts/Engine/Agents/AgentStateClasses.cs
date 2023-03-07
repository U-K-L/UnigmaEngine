using System.Collections;
using UnityEngine;

public class AgentStateClasses
{
    public int state = 0;

    public virtual void Debug()
    {
        UnityEngine.Debug.Log("MainState");
    }

    public virtual string GetCurrentStateAsString()
    {
        return "none";
    }
}

public class AgentStateClassesIdle : AgentStateClasses
{
    public void Debug()
    {
        UnityEngine.Debug.Log("Idle");
    }
}

public class AgentStateClassesPause : AgentStateClasses
{
    public void Debug()
    {
        UnityEngine.Debug.Log("Idle");
    }
}

public class AgentStateClassesJumping : AgentStateClasses
{
    public void Debug()
    {
        UnityEngine.Debug.Log("Jumping");
    }
}

public class AgentStateClassesMoving : AgentStateClasses
{
    public void Debug()
    {
        UnityEngine.Debug.Log("Moving");
    }
}

public class AgentStateClassesAirborne : AgentStateClasses
{
    public override void Debug()
    {
        UnityEngine.Debug.Log("Airborne");
    }

    public override string GetCurrentStateAsString()
    {
        if (state == 0)
            return "rising";
        if (state == 1)
            return "falling";
        return "none";
    }
}

public class AgentStateClassesCrouching : AgentStateClasses
{
    public void Debug()
    {
        UnityEngine.Debug.Log("Crouching");
    }
}