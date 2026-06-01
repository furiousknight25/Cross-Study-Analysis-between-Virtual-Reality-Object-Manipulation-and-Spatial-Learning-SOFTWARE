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
    public TMP_Text finalSentenceText;
    public TestingEnvironmentManager testingEnvironment;

    [Header("Phase 2: 3D Sorting UI")]
    public Transform bubbleUIParent;
    public Transform wordsAndButtons;
    
    public Vector3 bubbleUIUpOffset = new Vector3(0, 1.5f, 0);
    public Vector3 wordsAndButtonsDownOffset = new Vector3(0, -1.5f, 0);
    public float transitionTweenDuration = 1.0f;

    [Header("Phase 2: 3D Item Spawning")]
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
    private bool isProcessingSubmission = false; // Prevents double-logging

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

        // Auto-Fill for Debugging
        if (Keyboard.current.bKey.wasPressedThisFrame && !is3DPhaseActive && !isProcessingSubmission)
        {
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

        // Master Submit Input (Laptop)
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            submit_text();
        }
    }

    // --- PHASE 1: TEXT BUILDING ---
    private void HandleSlotSelected(int slotIndex)
    {
        if (is3DPhaseActive || isProcessingSubmission) return; 
        activeSlotIndex = slotIndex;
        UpdateSentenceDisplay();
    }

    private void HandleFoilSelected(string foilText)
    {
        if (is3DPhaseActive || isProcessingSubmission) return; 

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
    
    // 🚨 UPDATED SMART ROUTER 🚨
    public void submit_text()
    {
        // 1. Safety check: Ignore click if a tween or submission is already running
        if (isProcessingSubmission) return;

        // 2. Early Return: If in Phase 2, route to 3D submission and stop here
        if (is3DPhaseActive)
        {
            Submit3DOrder();
            return;
        }

        // 3. Early Return: If in Phase 1 but missing words, reject the submission
        if (filledSlotsCount < 6)
        {
            Debug.Log("<color=orange>[Phase 1] Cannot submit yet. Not all slots are filled.</color>");
            return;
        }

        // 4. If we made it here, grade Phase 1 Text
        if (currentTrialData != null)
        {
            isProcessingSubmission = true; // Lock inputs
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
            
            if (bubbleUIParent != null) bubbleUIParent.localPosition = Vector3.Lerp(initialBubbleUIPos, bubbleTargetPos, t);
            if (wordsAndButtons != null) wordsAndButtons.localPosition = Vector3.Lerp(initialWordsAndButtonsPos, wordsTargetPos, t);
            
            yield return null;
        }

        SpawnPhase2Items();
        isProcessingSubmission = false; // Unlock inputs for Phase 2
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
        isProcessingSubmission = true; // Lock inputs to prevent double-submit
        Debug.Log("<color=green>[Phase 2] 3D Sorting Complete! Logging Spatial Order...</color>");

        for (int i = 0; i < Bubbles.Count; i++)
        {
            string loggedItemName = "Empty";

            if (Bubbles[i].currentlyHeldObject != null)
            {
                GrabbableItem item = Bubbles[i].currentlyHeldObject.GetComponent<GrabbableItem>();
                if (item != null) loggedItemName = item.ItemName;
            }

            if (LoggingManager.Instance != null && currentTrialData != null)
            {
                LoggingManager.Instance.LogEvent(currentTrialData.TrialID, "bubble_placement", i, loggedItemName);
            }
        }

        Clear3DItems();

        if (LoggingManager.Instance != null) LoggingManager.Instance.SaveToDisk();

        Director.Instance.EndTestingPhase();
    }

    // --- RESET / CLEANUP ---

    private void Clear3DItems()
    {
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

        foreach (var b in Bubbles)
        {
            if (b.currentlyHeldObject != null) b.RemoveObject(b.currentlyHeldObject);
        }
    }

    public void ResetSentence()
    {
        is3DPhaseActive = false;
        isProcessingSubmission = false; // Reset the lock
        activeSlotIndex = -1;
        filledSlotsCount = 0;

        for (int i = 0; i < 6; i++)
        {
            sentenceChunks[i] = "_____";
            slotIsFilled[i] = false;
        }

        if (bubbleUIParent != null) bubbleUIParent.localPosition = initialBubbleUIPos;
        if (wordsAndButtons != null) wordsAndButtons.localPosition = initialWordsAndButtonsPos;

        Clear3DItems();
        UpdateSentenceDisplay();
    }
    
    public void InitializeSentenceBuilder(ExperimentTrialData trialData, TrialScene trialScene)
    {
        currentTrialData = trialData;
        currentTrialScene = trialScene;
        ResetSentence(); 
    }
}