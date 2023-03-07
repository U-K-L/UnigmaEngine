using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EggGameData
{
    public class PLAYABLECHARACTERS
    {
        public static (string, int) KANALOA = ("Kanaloa", 0);
    }

    public static GameObject LoadCharacterFromIndex(int index)
    {
        return Resources.Load<GameObject>("Characters/" + PLAYABLECHARACTERS.KANALOA.Item1 + "/EggPrefabs/" + PLAYABLECHARACTERS.KANALOA.Item1 + "EggObject");
    }
}
