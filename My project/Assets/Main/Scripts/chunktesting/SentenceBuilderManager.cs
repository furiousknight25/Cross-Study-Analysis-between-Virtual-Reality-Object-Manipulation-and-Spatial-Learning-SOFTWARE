using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class SentenceBuilderManager : MonoBehaviour
{
    [Header("Event Channels")]
    public IntEventChannelSO slotSelectedChannel;
    public StringEventChannelSO foilSelectedChannel;

    [Header("UI References")]
    [Tooltip("Drag your single 'FinalSentence' TextMeshPro object here.")]
    public TMP_Text finalSentenceText;
    
    [Tooltip("Reference to the Environment Manager so we can tell it to slide the words away.")]
    public TestingEnvironmentManager testingEnvironment;

    [Header("Phase 2: 3D Sorting UI")]
    [Tooltip("The empty GameObject holding your Receptacle Bubbles.")]
    public Transform bubbleUIParent;
    [Tooltip("The empty GameObject holding your words and column buttons.")]
    public Transform wordsAndButtons;
    
    public Vector3 bubbleUIUpOffset = new Vector3(0, 1.5f, 0);
    public Vector3 wordsAndButtonsDownOffset = new Vector3(0, -1.5f, 0);
    public float transitionTweenDuration = 1.0f;

    [Header("Phase 2: 3D Item Spawning")]
    [Tooltip("Empty GameObjects on the right table where items should spawn.")]
    public List<Transform> itemSpawnPoints = new List<Transform>();
    public List<PhysicsBubbleReceptacle> Bubbles = new List<PhysicsBubbleReceptacle>();

    // Internal State
    private int activeSlotIndex = -1;
    private string[] sentenceChunks = new string[6];
    private bool[] slotIsFilled = new bool[6];
    private int filledSlotsCount = 0;
    
    private ExperimentTrialData currentTrialData;
    private TrialScene currentTrialScene; 

    private bool is3DPhaseActive = false;
    private Vector3 initialBubbleUIPos;
    private Vector3 initialWordsAndButtonsPos;
    private List<GameObject> activeSpawnedItems = new List<GameObject>();

    private void Awake()
    {
        if (bubbleUIParent != null) initialBubbleUIPos = bubbleUIParent.localPosition;
        if (wordsAndButtons != null) initialWordsAndButtonsPos = wordsAndButtons.localPosition;
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
        if (Keyboard.current == null) return;

        // --- DEBUG CONTROL: Auto-Fill the sentence ---
        if (Keyboard.current.bKey.wasPressedThisFrame && !is3DPhaseActive)
        {
            Debug.Log("<color=yellow>[DEBUG] 'B' Key Pressed: Auto-filling sentence slots...</color>");
            for (int i = 0; i < 6; i++)
            {
                if (!slotIsFilled[i])
                {
                    string randomFoil = (currentTrialData != null && currentTrialData.Foils != null && currentTrialData.Foils.Length > 0) 
                        ? currentTrialData.Foils[UnityEngine.Random.Range(0, currentTrialData.Foils.Length)] 
                        : "Auto_Word_" + i;

                    sentenceChunks[i] = $"<color=#00FF00>{randomFoil}</color>";
                    slotIsFilled[i] = true;
                    filledSlotsCount++;
                }
            }
            
            activeSlotIndex = -1; 
            UpdateSentenceDisplay(); 
        }

        // --- Master Submit Button (Laptop Control) ---
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (!is3DPhaseActive) submit_text(); 
            else Submit3DOrder(); 
        }
    }

    // --- PHASE 1: TEXT BUILDING ---
    private void HandleSlotSelected(int slotIndex)
    {
        if (is3DPhaseActive) return; 
        activeSlotIndex = slotIndex;
        UpdateSentenceDisplay();
    }

    private void HandleFoilSelected(string foilText)
    {
        if (is3DPhaseActive) return; 

        if (activeSlotIndex >= 0 && activeSlotIndex < 6)
        {
            sentenceChunks[activeSlotIndex] = $"<color=#00FF00>{foilText}</color>";

            if (!slotIsFilled[activeSlotIndex])
            {
                slotIsFilled[activeSlotIndex] = true;
                filledSlotsCount++;
            }

            activeSlotIndex = -1; 
            UpdateSentenceDisplay();
        }
    }
    
    public void submit_text()
    {
        if (filledSlotsCount >= 6 && currentTrialData != null && !is3DPhaseActive)
        {
            Debug.Log("<color=green>[Phase 1] Text Sentence Complete! Grading...</color>");
            int correctCount = 0;

            for (int i = 0; i < 6; i++)
            {
                string cleanChunk = sentenceChunks[i].Replace("<color=#00FF00>", "").Replace("</color>", "").Trim();
                string targetChunk = currentTrialData.Chunks[i].Trim();

                bool isCorrect = string.Equals(cleanChunk, targetChunk, System.StringComparison.OrdinalIgnoreCase);
                if (isCorrect) correctCount++;

                string eventName = isCorrect ? "Chunk_Correct" : "Chunk_Incorrect";

                if (LoggingManager.Instance != null)
                {
                    LoggingManager.Instance.LogEvent(currentTrialData.TrialID, eventName, i, cleanChunk);
                }
            }

            string cleanSentence = string.Join(" ", sentenceChunks).Replace("<color=#00FF00>", "").Replace("</color>", "");
            if (LoggingManager.Instance != null)
            {
                LoggingManager.Instance.LogEvent(currentTrialData.TrialID, $"Text_Complete_Score_{correctCount}_out_of_6", -1, cleanSentence);
            }

            StartCoroutine(TransitionTo3DPhaseRoutine());
        }
    }

    private void UpdateSentenceDisplay()
    {
        if (finalSentenceText == null) return;

        string displayString = "";
        for (int i = 0; i < 6; i++)
        {
            if (i == activeSlotIndex) displayString += $"<color=yellow>[ {sentenceChunks[i]} ]</color> ";
            else displayString += $"{sentenceChunks[i]} ";
        }
        finalSentenceText.text = displayString.Trim();
    }

    // --- PHASE 2: 3D SPATIAL SORTING ---

    private IEnumerator TransitionTo3DPhaseRoutine()
    {
        is3DPhaseActive = true;
        Debug.Log("<color=cyan>[Phase 2] Transitioning UI and Moving 3D Items...</color>");

        float elapsed = 0f;
        Vector3 bubbleTargetPos = initialBubbleUIPos + bubbleUIUpOffset;
        Vector3 wordsTargetPos = initialWordsAndButtonsPos + wordsAndButtonsDownOffset;

        while (elapsed < transitionTweenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / transitionTweenDuration);
            
            // Tween Bubbles Up
            if (bubbleUIParent != null) bubbleUIParent.localPosition = Vector3.Lerp(initialBubbleUIPos, bubbleTargetPos, t);
            
            // Tween Words and Buttons Down
            if (wordsAndButtons != null) wordsAndButtons.localPosition = Vector3.Lerp(initialWordsAndButtonsPos, wordsTargetPos, t);
            
            yield return null;
        }

        SpawnPhase2Items();
    }

   private void SpawnPhase2Items()
    {
        if (currentTrialScene == null || currentTrialScene.enviornment_parant == null) return;

        GrabbableItem[] trialItems = currentTrialScene.enviornment_parant.GetComponentsInChildren<GrabbableItem>(true);
        List<GrabbableItem> itemsToMove = new List<GrabbableItem>(trialItems);

        for (int i = 0; i < itemsToMove.Count; i++)
        {
            GrabbableItem temp = itemsToMove[i];
            int randomIndex = Random.Range(i, itemsToMove.Count);
            itemsToMove[i] = itemsToMove[randomIndex];
            itemsToMove[randomIndex] = temp;
        }

        for (int i = 0; i < itemsToMove.Count; i++)
        {
            if (i >= itemSpawnPoints.Count) break; 

            GrabbableItem item = itemsToMove[i];
            
            item.gameObject.SetActive(true);

            Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers) r.enabled = true;

            item.transform.position = itemSpawnPoints[i].position;
            item.transform.rotation = itemSpawnPoints[i].rotation;

            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = true;
                rb.isKinematic = false;
            }

            activeSpawnedItems.Add(item.gameObject);
        }
    }

    public void Submit3DOrder()
    {
        Debug.Log("<color=green>[Phase 2] 3D Sorting Complete! Logging Spatial Order...</color>");

        for (int i = 0; i < Bubbles.Count; i++)
        {
            string loggedItemName = "Empty";

            if (Bubbles[i].currentlyHeldObject != null)
            {
                GrabbableItem item = Bubbles[i].currentlyHeldObject.GetComponent<GrabbableItem>();
                if (item != null) loggedItemName = item.ItemName;
            }

            if (LoggingManager.Instance != null)
            {
                LoggingManager.Instance.LogEvent(currentTrialData.TrialID, "bubble_placement", i, loggedItemName);
            }
        }

        // --- NEW: Instantly clean up the items so they don't linger in the scene ---
        Clear3DItems();

        // The Director will call testingEnvironment.EndTestingPhase() to tween the entire board away
        Director.Instance.EndTestingPhase();
    }

    // --- RESET / CLEANUP ---

    private void Clear3DItems()
    {
        // 1. Drop and hide the table items
        foreach (var obj in activeSpawnedItems)
        {
            if (obj != null) 
            {
                GrabbableItem gi = obj.GetComponent<GrabbableItem>();
                if (gi != null) gi.OnPhysicalHandGrabbed(); 
                
                obj.SetActive(false); 
            }
        }
        activeSpawnedItems.Clear();

        // 2. Empty the bubbles
        foreach (var b in Bubbles)
        {
            if (b.currentlyHeldObject != null) b.RemoveObject(b.currentlyHeldObject);
        }
    }

    public void ResetSentence()
    {
        is3DPhaseActive = false;
        activeSlotIndex = -1;
        filledSlotsCount = 0;

        for (int i = 0; i < 6; i++)
        {
            sentenceChunks[i] = "_____";
            slotIsFilled[i] = false;
        }

        // Snap everything perfectly back to its starting local position
        if (bubbleUIParent != null) bubbleUIParent.localPosition = initialBubbleUIPos;
        if (wordsAndButtons != null) wordsAndButtons.localPosition = initialWordsAndButtonsPos;

        // Uses the new helper method to guarantee everything is wiped
        Clear3DItems();

        UpdateSentenceDisplay();
    }
    
    public void InitializeSentenceBuilder(ExperimentTrialData trialData, TrialScene trialScene)
    {
        currentTrialData = trialData;
        currentTrialScene = trialScene;
        ResetSentence(); // Resets local coordinates before TestingEnvironmentManager tweens the board back up
    }
}