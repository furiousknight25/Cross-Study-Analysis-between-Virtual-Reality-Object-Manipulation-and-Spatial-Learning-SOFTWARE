using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SortableItem : MonoBehaviour
{
    public enum MaterialType 
    { 
        Metal, 
        Rubber, 
        Wood 
    }

    [Tooltip("Set the material type for this specific object.")]
    public MaterialType itemMaterial;

    [Tooltip("Has this item been sorted already?")]
    public bool isSorted = false;

    // Variables to store the original transform data
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;

    private void Start()
    {
        // Cache the rigidbody and starting transform data in local space
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }
/// <summary>
    /// Teleports the object back to its starting local position and safely resets physics.
    /// </summary>
    public void ResetToStart(bool keepFrozen = false)
    {
        isSorted = false;

        if (rb != null)
        {
            // 1. Force kinematic ON to safely move
            rb.isKinematic = true; 

            // 2. Teleport the transform
            transform.localPosition = initialPosition;
            transform.localRotation = initialRotation;

            // 3. Zero out momentum 
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // 4. If we aren't keeping it frozen for the tween, turn physics back on
            if (!keepFrozen)
            {
                rb.isKinematic = false;
            }
        }
        else
        {
            transform.localPosition = initialPosition;
            transform.localRotation = initialRotation;
        }
    }
}