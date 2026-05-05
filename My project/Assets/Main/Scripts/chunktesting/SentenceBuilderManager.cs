using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class SentenceBuilderManager : MonoBehaviour
{
    [Header("Event Channels")]
    public IntEventChannelSO slotSelectedChannel;
    public StringEventChannelSO foilSelectedChannel;

    [Header("UI References")]
    [Tooltip("Drag your single 'FinalSentence' TextMeshPro object here.")]
    public TMP_Text finalSentenceText;

    private int activeSlotIndex = -1;
    private string[] sentenceChunks = new string[6];
    private bool[] slotIsFilled = new bool[6];
    private int filledSlotsCount = 0;
    private ExperimentTrialData currentTrialData;
    private void Awake()
    {
        ResetSentence();
    }

    private void OnEnable()
    {
        if (slotSelectedChannel != null) slotSelectedChannel.OnEventRaised += HandleSlotSelected;
        if (foilSelectedChannel != null) foilSelectedChannel.OnEventRaised += HandleFoilSelected;
    }

    private void OnDisable()
    {
        if (slotSelectedChannel != null) slotSelectedChannel.OnEventRaised -= HandleSlotSelected;
        if (foilSelectedChannel != null) foilSelectedChannel.OnEventRaised -= HandleFoilSelected;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            for (int i = 0; i < 6; i++)
            {
                if (!slotIsFilled[i])
                {
                    activeSlotIndex = i;
                    HandleFoilSelected("RandomFoil_" + Random.Range(10, 99));
                }
            }
        }
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            submit_text();
        }
    }

    private void HandleSlotSelected(int slotIndex)
    {
        activeSlotIndex = slotIndex;
        UpdateSentenceDisplay();
    }

    private void HandleFoilSelected(string foilText)
    {
        if (activeSlotIndex >= 0 && activeSlotIndex < 6)
        {
            // Insert the chosen word and color it green
            sentenceChunks[activeSlotIndex] = $"<color=#00FF00>{foilText}</color>";

            if (!slotIsFilled[activeSlotIndex])
            {
                slotIsFilled[activeSlotIndex] = true;
                filledSlotsCount++;
            }

            // Deselect the slot once filled
            activeSlotIndex = -1; 
            UpdateSentenceDisplay();
        }
    }
    
public void submit_text()
    {
        if (filledSlotsCount >= 6 && currentTrialData != null)
        {
            Debug.Log("<color=green>Sentence Complete! Grading...</color>");
            int correctCount = 0;

            // 1. Grade each chunk and log the result
            for (int i = 0; i < 6; i++)
            {
                // Strip the green rich text tags to get the raw word
                string cleanChunk = sentenceChunks[i].Replace("<color=#00FF00>", "").Replace("</color>", "").Trim();
                string targetChunk = currentTrialData.Chunks[i].Trim();

                // Compare what they selected vs what the true target was
                bool isCorrect = string.Equals(cleanChunk, targetChunk, System.StringComparison.OrdinalIgnoreCase);
                if (isCorrect) correctCount++;

                string eventName = isCorrect ? "Chunk_Correct" : "Chunk_Incorrect";

                // Log this specific chunk's result to the JSON Lines file
                if (LoggingManager.Instance != null)
                {
                    LoggingManager.Instance.LogEvent(currentTrialData.TrialID, eventName, i, cleanChunk);
                }
            }

            // 2. Log the final complete sentence and the total score (e.g., 4 out of 6)
            string cleanSentence = string.Join(" ", sentenceChunks).Replace("<color=#00FF00>", "").Replace("</color>", "");
            
            if (LoggingManager.Instance != null)
            {
                // We embed the final score directly into the event name for easy data sorting later
                LoggingManager.Instance.LogEvent(currentTrialData.TrialID, $"Test_Complete_Score_{correctCount}_out_of_6", -1, cleanSentence);
            }

            Director.Instance.EndTestingPhase();
        }
    }

    private void UpdateSentenceDisplay()
    {
        if (finalSentenceText == null) return;

        string displayString = "";
        for (int i = 0; i < 6; i++)
        {
            if (i == activeSlotIndex)
            {
                // Highlight the currently selected blank in yellow so the user knows where the word will go
                displayString += $"<color=yellow>[ {sentenceChunks[i]} ]</color> ";
            }
            else
            {
                displayString += $"{sentenceChunks[i]} ";
            }
        }
        
        finalSentenceText.text = displayString.Trim();
    }

    // Called by the Director to clear the text for the next trial
    public void ResetSentence()
    {
        activeSlotIndex = -1;
        filledSlotsCount = 0;
        for (int i = 0; i < 6; i++)
        {
            sentenceChunks[i] = "_____";
            slotIsFilled[i] = false;
        }
        UpdateSentenceDisplay();
    }
    
    public void InitializeSentenceBuilder(ExperimentTrialData trialData)
    {
        currentTrialData = trialData;
        ResetSentence();
    }
}