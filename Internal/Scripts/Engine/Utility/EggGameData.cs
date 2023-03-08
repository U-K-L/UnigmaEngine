using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EggGameData
{
    public class PLAYABLECHARACTERS
    {

        public static Dictionary<string, (string, int)> CHARACTERSNAMES = new Dictionary<string, (string, int)>()
        {
           { "Kanaloa", ("Kanaloa", 0)}
        };

        public static Dictionary<int, (string, int)> CHARACTERSINDEX = new Dictionary<int, (string, int)>()
        {
           {0, ("Kanaloa", 0)}
        };
    }

    public static GameObject LoadCharacterFromIndex(int index)
    {
        return Resources.Load<GameObject>("Characters/" + PLAYABLECHARACTERS.CHARACTERSINDEX[index].Item1 + "/EggPrefabs/" + PLAYABLECHARACTERS.CHARACTERSINDEX[index].Item1 + "EggObject");
    }

    public static GameObject LoadCharacterFromName(string name)
    {
        return Resources.Load<GameObject>("Characters/" + PLAYABLECHARACTERS.CHARACTERSNAMES[name].Item1 + "/EggPrefabs/" + PLAYABLECHARACTERS.CHARACTERSNAMES[name].Item1 + "EggObject");
    }
}
