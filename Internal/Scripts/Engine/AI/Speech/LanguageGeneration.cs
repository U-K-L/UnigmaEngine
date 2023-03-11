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
    // Start is called before the first frame update
    void Start()
    {
        _dialogueBox = this.transform.parent.GetComponentInChildren<DialogueBox>();
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }

    async void StartChatting(string message)
    {
        var api = new OpenAIClient();
        var chatPrompts = new List<ChatPrompt>
{
        new ChatPrompt("system", GPTSystem),
        new ChatPrompt("user", message)
        //new ChatPrompt("user", "Who won the world series in 2020?"),
        //new ChatPrompt("assistant", "The Los Angeles Dodgers won the World Series in 2020."),
        //new ChatPrompt("user", "Where was it played?"),
};
        var chatRequest = new ChatRequest(chatPrompts, Model.GPT3_5_Turbo);

        string words = "";
        await api.ChatEndpoint.StreamCompletionAsync(chatRequest, result =>
        {
            words += result.FirstChoice;
            //Debug.Log(result.FirstChoice);
        });

        Debug.Log(words);

        _dialogueBox.AddText(words);
    }
    
    public void GenerateDialogue(string dialogue)
    {
        if (previousMessage == dialogue)
        {
            return;
        }
        Debug.Log("Generating");
        previousMessage = dialogue;
        StartChatting(dialogue);
        //_dialogueBox.AddText(dialogue);
    }
}
