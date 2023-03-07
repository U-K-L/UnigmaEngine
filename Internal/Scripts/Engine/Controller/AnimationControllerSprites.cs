using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;


public class AnimationFileJson
{
    //the constructor
    [Preserve]
    public AnimationFileJson()
    {
    }
    public List<AnimationData> animationData;
}

public class AnimationData
{
    //the constructor
    [Preserve]
    public AnimationData()
    {
    }
    public string name { get; set; }
    public string numFrames { get; set; }
    public List<string> animationSpeeds { get; set; }
}

public class AnimationControllerSprites : MonoBehaviour
{
    
    public string Name;

    [HideInInspector]
    public int currentAnim = 0;
    [HideInInspector]
    public bool loop = true;
    [HideInInspector]
    public bool ForceAnimationPlay = false;
    [HideInInspector]
    public string animationName = "none";
    [HideInInspector]
    public float _animationSpeed = 0.02f;
    [HideInInspector]
    public Dictionary<string, AnimationData> animationData;

    private string _currentKey;
    private Queue<string> _animationStack = new Queue<string>();
    private List<string> States;
    Dictionary<string, Texture2D[]> textures;
    Renderer m_Renderer;
    private AgentPhysics _agent;
    private string currnetAnimationKey = "none";
    public TextAsset JsonFile;

    private bool _playInReverse = false;



    void Start()
    {
        animationData = new Dictionary<string, AnimationData>();
        _agent = GetComponent<AgentPhysics>();
        m_Renderer = GetComponent<Renderer>();
        
        textures = new Dictionary<string, Texture2D[]>();
        
        CreateAnimationData();
        StoreTextures();
        StartCoroutine(animate());
    }

    private void Update()
    {

    }

    void CreateAnimationData()
    {
        string prependedPath = "Characters/";
        string appendedPath = "Animation/";
        string path = prependedPath + Name + "/" + appendedPath + Name + "AnimationData";
        AnimationFileJson JsonAnimation = JsonConvert.DeserializeObject<AnimationFileJson>(JsonFile.text);
        foreach (AnimationData data in JsonAnimation.animationData)
        {
            animationData.Add(data.name, data);
            Debug.Log(data.name + " " + data.numFrames);
        }
        //AnimationFileJson JsonAnimation = Json.ToObject<AnimationFileJson>(JsonFile.text, new JsonParameters());
        //animationData = JsonAnimation.animationData.ToDictionary(x => x.name);
    }

    void StoreTextures()
    {
        string prependedPath = "Characters/";
        string appendedPath = "Animation/";
        foreach(KeyValuePair<string, AnimationData> entry in animationData)
        {
            string key = entry.Key;
            string path = prependedPath + Name + "/" + appendedPath + key + "/";
            textures.Add(key, Resources.LoadAll<Texture2D>(path));
        }
    }
    
    IEnumerator animate()
    {
        EndOfAnimationCallBack();
        currentAnim = 0;
        while (this.enabled)
        {
            if (animationData.ContainsKey(_currentKey))
            {
                if (textures.ContainsKey(_currentKey))
                {
                    if (ForceAnimationPlay == false)
                    {
                        //string key = Enum.GetName(typeof(AgentPhysics.StateMachine), (int)_agent.getState());
                        
                        string key = _currentKey;
                        int numOfFrames = int.Parse(animationData[key].numFrames);
                        int currentFrame = currentAnim % numOfFrames;
                        float animationSpeed = 0;
                        Texture2D texture = textures[key][currentFrame];
                        animationSpeed = float.Parse(animationData[key].animationSpeeds[currentFrame]);

                        if (_playInReverse)
                        {
  
                            currentFrame = (numOfFrames - currentAnim - 1) % numOfFrames;
                            texture = textures[key][currentFrame];
                            animationSpeed = float.Parse(animationData[key].animationSpeeds[currentFrame]);
                        }

                        SetTexture(texture);
                        yield return new WaitForSeconds(animationSpeed);

                    }
                }
                else
                    EndOfAnimationCallBack();
            }
            else
                EndOfAnimationCallBack();
            yield return new WaitForSeconds(0f);
        }
    }

    void SetTexture(Texture2D texture)
    {
        int numOfFrames = int.Parse(animationData[_currentKey].numFrames);
        m_Renderer.material.SetTexture("_MainTex", texture);
        m_Renderer.material.SetTexture("_SpriteTex", texture);

        if (currentAnim == numOfFrames - 1)
            EndOfAnimationCallBack();
        currentAnim = (currentAnim + 1) % numOfFrames;
    }

    void EndOfAnimationCallBack()
    {
        currentAnim = 0;
        _currentKey = GetAnimation();
    }

    public void SetAnimation(string key)
    {
        if (_animationStack.Count > 0)
        {
            if (_animationStack.Last() != key)
                _animationStack.Enqueue(key);
        }
        else
        {
            _animationStack.Enqueue(key);
        }
    }

    private string GetAnimation()
    {
        _playInReverse = false;
        if (_animationStack.Count > 0)
        {
            currnetAnimationKey = _animationStack.Dequeue();

        }
        string[] tokens = currnetAnimationKey.Split('_');
        if (tokens.Length > 1)
        {
            if (tokens[1] == "reverse")
            {
                _playInReverse = true;
            }
        }
        currnetAnimationKey = tokens[0];

        return currnetAnimationKey;
    }
}
