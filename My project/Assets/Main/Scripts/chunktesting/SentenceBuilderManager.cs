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
        // Check if all 6 slots are filled
        Debug.Log(filledSlotsCount);
        if (filledSlotsCount >= 6)
        {
            Debug.Log("<color=green>Sentence Complete!</color>");
            
            // Clean the rich text tags off for the raw data log
            string cleanSentence = string.Join(" ", sentenceChunks).Replace("<color=#00FF00>", "").Replace("</color>", "");
            
            // Note: Ensure LoggingManager is in your scene to catch this!
            if (LoggingManager.Instance != null)
            {
                LoggingManager.Instance.LogEvent("Test_Completed", cleanSentence);
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
}