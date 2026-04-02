using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PhysicsBubbleReceptacle : MonoBehaviour
{
    
    [Header("Starting State")]
    [Tooltip("Assign a Rigidbody here in the Inspector to have it start already snapped inside the bubble.")]
    public Rigidbody startingObject; // NEW: Exposes an initial object slot in the Inspector

    [Header("Magnetic Snap Settings (PID)")]
    public float positionalSpring = 25f; // How strongly it pulls to center
    public float rotationalSpring = 15f; // How strongly it aligns rotation
    public float maxVelocity = 5f;       // Prevents the object from going terminal velocity if bumped hard


    [Header("Visual Settings")]
    public float hoverScaleMultiplier = 1.2f;
    public float scaleTweenDuration = 0.2f;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    [Header("Snapping Settings")]
    public float snapTweenDuration = 0.15f;
    
    [Header("State (Read Only)")]
    public Rigidbody currentlyHeldObject;
    public List<Rigidbody> hoveringObjects = new List<Rigidbody>();

    public bool IsInventory = false;

    void Start()
    {
        originalScale = transform.localScale;

        if (startingObject != null)
        {
            ForceSnapObject(startingObject);
        }

        if (IsInventory)
        {
            ShowVisuals();
        }
        else
        {
            HideVisuals();
        }
    }
    

    // --- TRIGGER LOGIC FOR HOVER VISUALS ---

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && currentlyHeldObject == null && !hoveringObjects.Contains(rb))
        {
            // NEW: Pass this bubble's reference down to the grabbable item
            GrabbableItem grabbable = rb.GetComponent<GrabbableItem>();
            if (grabbable != null)
            {
                grabbable.currentBubble = this;
            }

            // (Existing logic)
            hoveringObjects.Add(rb);
            if (hoveringObjects.Count == 1)
            {
                SetBubbleScale(originalScale * hoverScaleMultiplier);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && hoveringObjects.Contains(rb))
        {
            // NEW: Remove the reference when the item leaves the bubble
            GrabbableItem grabbable = rb.GetComponent<GrabbableItem>();
            
            // Only clear it if this bubble is still the active one (prevents bugs if bubbles overlap)
            if (grabbable != null && grabbable.currentBubble == this)
            {
                grabbable.currentBubble = null;
            }

            // (Existing logic)
            hoveringObjects.Remove(rb);
            if (hoveringObjects.Count == 0 && currentlyHeldObject == null)
            {
                SetBubbleScale(originalScale);
            }
        }
    }

    public void ForceSnapObject(Rigidbody rb)
    {
        if (rb == null) return;

        currentlyHeldObject = rb;

        // Set the two-way reference for your GrabbableItem
        GrabbableItem grabbable = rb.GetComponent<GrabbableItem>();
        if (grabbable != null)
        {
            grabbable.currentBubble = this;
        }

        // Instantly teleport it so it doesn't "fly" to the center on frame 1
        rb.position = transform.position;
        rb.rotation = transform.rotation;

        StartCoroutine(SnapToCenterRoutine(rb));
    }

    // --- INTEGRATION METHODS (Call these from your existing events) ---

    /// <summary>
    /// Call this from your "Let Go" event. Pass in the Rigidbody that was just dropped.
    /// </summary>
    public void TrySnapObject(Rigidbody droppedObject)
    {
        // Only snap if the object was dropped INSIDE our trigger volume, and we are empty
        if (hoveringObjects.Contains(droppedObject) && currentlyHeldObject == null)
        {
            Debug.Log("Snapping started!");
            currentlyHeldObject = droppedObject;
            StartCoroutine(SnapToCenterRoutine(droppedObject));
        }
    }

    /// <summary>
    /// Call this from your "Grab" event. Pass in the Rigidbody being grabbed.
    /// </summary>
public void RemoveObject(Rigidbody grabbedObject)
    {
        if (currentlyHeldObject == grabbedObject)
        {
            StopAllCoroutines(); 
            
            // Break the weld!

            currentlyHeldObject.useGravity = true; 
            currentlyHeldObject = null;

            if (hoveringObjects.Count == 0)
            {
                SetBubbleScale(originalScale);
            }
        }
    }

    // --- TWEENING COROUTINES ---

private IEnumerator SnapToCenterRoutine(Rigidbody rb)
    {
        // 1. Keep it physical, but turn off gravity so it hovers
        rb.isKinematic = false;
        rb.useGravity = false;

        // 2. The PID / Velocity Tracking Loop
        while (currentlyHeldObject == rb)
        {
            // --- POSITIONAL SPRING (The 'P' in PID) ---
            Vector3 positionError = transform.position - rb.position;
            Vector3 desiredVelocity = positionError * positionalSpring;
            
            // Apply velocity directly (more stable for VR than AddForce) and clamp it for safety
            rb.linearVelocity = Vector3.ClampMagnitude(desiredVelocity, maxVelocity);

            // --- ROTATIONAL SPRING ---
            // Calculate the difference between current rotation and bubble rotation
            Quaternion rotationError = transform.rotation * Quaternion.Inverse(rb.rotation);
            rotationError.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

            // Fix the angle to take the shortest path (Unity's ToAngleAxis sometimes returns values over 180)
            if (angleInDegrees > 180f) angleInDegrees -= 360f;

            // Only apply torque if it's actually misaligned, to prevent micro-jitters
            if (Mathf.Abs(angleInDegrees) > 0.1f)
            {
                // Convert to radians for the physics engine
                Vector3 desiredAngularVelocity = (angleInDegrees * Mathf.Deg2Rad) * rotationAxis * rotationalSpring;
                rb.angularVelocity = desiredAngularVelocity;
            }
            else
            {
                rb.angularVelocity = Vector3.zero;
            }

            // Wait for the next physics step
            yield return new WaitForFixedUpdate();
        }
    }
    private void SetBubbleScale(Vector3 targetScale)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleRoutine(targetScale));
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale)
    {
        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsedTime < scaleTweenDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / scaleTweenDuration);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
    // --- VISUAL TOGGLES ---

    public void HideVisuals()
    {
        // GetComponentsInChildren finds the renderer on this object AND any children
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }
    }

    public void ShowVisuals()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = true;
        }
    }
}

