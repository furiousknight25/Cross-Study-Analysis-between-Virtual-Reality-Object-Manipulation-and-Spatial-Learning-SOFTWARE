using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))] // NEW: Forces an AudioSource on the Bin
public class SortingBin : MonoBehaviour
{
    [Tooltip("Which material belongs in this bin?")]
    public SortableItem.MaterialType targetMaterial;

    [Tooltip("Drag your Shelf object here so the bin can report completion.")]
    public SortingShelfManager shelfManager;

    [Tooltip("Event triggered when a correct item is sorted.")]
    public UnityEvent OnCorrectSort;

    [Tooltip("Event triggered when an incorrect item is sorted.")]
    public UnityEvent OnIncorrectSort;

    private AudioSource audioSource; // NEW: Reference to the audio component

    private void Awake()
    {
        // Cache the audio source
        audioSource = GetComponent<AudioSource>();
        // Ensure it doesn't play the 'ding' the moment the game starts
        audioSource.playOnAwake = false; 
    }

    private void OnTriggerEnter(Collider other)
    {
        SortableItem item = other.GetComponent<SortableItem>();

        if (item != null && !item.isSorted)
        {
            if (item.itemMaterial == targetMaterial)
            {
                // CORRECT SORT
                item.isSorted = true; 
                Debug.Log($"<color=green>Correct!</color> {item.name} sorted into {targetMaterial} bin.");
                
                // NEW: Play the success sound
                if (audioSource.clip != null)
                {
                    audioSource.Play();
                }

                OnCorrectSort.Invoke();
                
                // Tell the shelf manager to check if we are completely done AND update score
                if (shelfManager != null)
                {
                    shelfManager.RegisterCorrectSort();
                }
            }
            else
            {
                // INCORRECT SORT
                Debug.Log($"<color=red>Incorrect.</color> {item.name} does not go in {targetMaterial} bin.");
                OnIncorrectSort.Invoke();
                
                // Immediately teleport it back to the shelf
                item.ResetToStart(); 
            }
        }
    }
}