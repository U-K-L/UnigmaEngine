using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LanguageGeneration : MonoBehaviour
{
    private DialogueBox _dialogueBox;
    public string GPTSystem;

    private string previousMessage;

    private Dictionary<string, List<ChatPrompt>> _conversations = new Dictionary<string, List<ChatPrompt>>();
    private string currentConversation = "default";

    // Start is called before the first frame update
    void Start()
    {
        _dialogueBox = this.transform.parent.GetComponentInChildren<DialogueBox>();
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }

    async void StartChatting(string conversation, string message)
    {
        var api = new OpenAIClient();

        if (!_conversations.ContainsKey(conversation))
        {
            CreateNewConversation(conversation, message);
        }
        else
        {
            _conversations[conversation].Add(new ChatPrompt("user", message));
        }

        var chatRequest = new ChatRequest(_conversations[conversation], Model.GPT3_5_Turbo);

        string words = "";
        await api.ChatEndpoint.StreamCompletionAsync(chatRequest, result =>
        {
            words += result.FirstChoice;
        });

        _dialogueBox.AddText(words);

        _conversations[conversation].Add(new ChatPrompt("assistant", words));
        DebugConversations();
    }

    void CreateNewConversation(string conversation, string message)
    {
            
        string system = GPTSystem;
        _conversations.Add(conversation, new List<ChatPrompt>()
        {
            new ChatPrompt("system", system),
            new ChatPrompt("user", message)
        });
    }

    public void GenerateDialogue(string user, string dialogue)
    {
        if (previousMessage == dialogue)
        {
            return;
        }
        previousMessage = dialogue;
        StartChatting(GetConversation(user), dialogue);
    }

    //Idea here is for a user to set the conversation say based on location, date, time.
    public void SetConversation(string conversation)
    {
        currentConversation = conversation;
    }

    //Who they are or what they were talking to, try to recall previous conversation
    public string GetConversation(string user)
    {
        return this.name + "," + user + "," + currentConversation;
        
    }

    public void DebugConversations()
    {
        foreach (KeyValuePair<string, List<ChatPrompt>> conversation in _conversations)
        {
            Debug.Log(conversation.Key);
            foreach (ChatPrompt prompt in conversation.Value)
            {
                Debug.Log(prompt.Content);
            }
        }
    }
}
