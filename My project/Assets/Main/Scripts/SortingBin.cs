using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
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
                OnCorrectSort.Invoke();
                
                // Tell the shelf manager to check if we are completely done
                if (shelfManager != null)
                {
                    shelfManager.CheckForCompletion();
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