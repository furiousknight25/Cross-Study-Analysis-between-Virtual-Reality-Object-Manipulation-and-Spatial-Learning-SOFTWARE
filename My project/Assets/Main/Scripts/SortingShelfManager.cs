using UnityEngine;
using TMPro; 

public class SortingShelfManager : MonoBehaviour
{
    private SortableItem[] allItemsOnShelf;

    [Header("Score Tracking")]
    [Tooltip("Drag your 3D TextMeshPro object here to display the score.")]
    public TMP_Text scoreTextDisplay;
    
    [Header("Reset Logic")]
    [Tooltip("How many items must be sorted before the shelf resets?")]
    public int itemsBeforeReset = 10; // Configurable in the Inspector!
    
    private int currentScore = 0; // Tracks the total score for the active session
    private int currentBatchCount = 0; // Tracks how many have been sorted in the current cycle

    private void Start()
    {
        // Automatically grab all SortableItems that are children of this shelf
        allItemsOnShelf = GetComponentsInChildren<SortableItem>();
        UpdateScoreDisplay(); // Initialize text at 0
    }

    /// <summary>
    /// Called by the bin when a correct item is placed.
    /// Increments score, updates UI, then checks if 10 items have been sorted.
    /// </summary>
    public void RegisterCorrectSort()
    {
        // 1. Increment both the total score and the batch counter
        currentScore++;
        currentBatchCount++;
        UpdateScoreDisplay();

        // 2. Check if we reached the reset threshold (10 items)
        if (currentBatchCount >= itemsBeforeReset)
        {
            Debug.Log($"<color=cyan>{itemsBeforeReset} items sorted! Resetting shelf for the next batch.</color>");
            ResetEntireShelf();
            
            // Reset the batch counter so it can count to 10 again, 
            // but leave currentScore alone so the high score keeps climbing!
            currentBatchCount = 0; 
        }
    }

    /// <summary>
    /// Resets the score and batch counter back to 0. Called when the distraction task starts.
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        currentBatchCount = 0;
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if (scoreTextDisplay != null)
        {
            scoreTextDisplay.text = $"Score: {currentScore}";
        }
    }

    /// <summary>
    /// Teleports every child item back to its original spot so the user can sort them again.
    /// </summary>
    public void ResetEntireShelf()
    {
        foreach (SortableItem item in allItemsOnShelf)
        {
            // We pass keepFrozen: false so the items instantly become physical and 
            // grabbable again the moment they teleport back to the shelf.
            item.ResetToStart(keepFrozen: false); 
        }
    }

    public void ResetAndFreezeAllItems()
    {
        foreach (SortableItem item in allItemsOnShelf)
        {
            item.ResetToStart(keepFrozen: true);
        }
        Debug.Log("<color=yellow>All items reset and frozen.</color>");
    }

    public void UnfreezeAllItems()
    {
        foreach (SortableItem item in allItemsOnShelf)
        {
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero; // Ensure no residual velocity
                rb.angularVelocity = Vector3.zero; // Ensure no residual angular velocity
            }
        }
        Debug.Log("<color=yellow>All items unfrozen.</color>");
    }
}