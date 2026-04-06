using UnityEngine;

public class SortingShelfManager : MonoBehaviour
{
    private SortableItem[] allItemsOnShelf;

    private void Start()
    {
        // Automatically grab all SortableItems that are children of this shelf
        allItemsOnShelf = GetComponentsInChildren<SortableItem>();
    }

    /// <summary>
    /// Called by a bin whenever a correct item is placed.
    /// Checks if the entire board is finished.
    /// </summary>
    public void CheckForCompletion()
    {
        bool allSorted = true;

        foreach (SortableItem item in allItemsOnShelf)
        {
            if (!item.isSorted)
            {
                allSorted = false;
                break; // Stop checking if we find even one unsorted item
            }
        }

        if (allSorted)
        {
            Debug.Log("<color=cyan>All items sorted! Resetting shelf.</color>");
            ResetEntireShelf();
        }
    }

    /// <summary>
    /// Teleports every child item back to its original spot.
    /// </summary>
    public void ResetEntireShelf()
    {
        foreach (SortableItem item in allItemsOnShelf)
        {
            item.ResetToStart(keepFrozen: true); // Keep frozen during reset to avoid physics issues
        }
    }
    public void ResetAndFreezeAllItems()
    {
        foreach (SortableItem item in allItemsOnShelf)
        {
            item.ResetToStart();
            
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
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