using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsBubbleReceptacle : MonoBehaviour
{
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

    void Start()
    {
        originalScale = transform.localScale;
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

    // --- INTEGRATION METHODS (Call these from your existing events) ---

    /// <summary>
    /// Call this from your "Let Go" event. Pass in the Rigidbody that was just dropped.
    /// </summary>
    public void TrySnapObject(Rigidbody droppedObject)
    {
        // Only snap if the object was dropped INSIDE our trigger volume, and we are empty
        if (hoveringObjects.Contains(droppedObject) && currentlyHeldObject == null)
        {
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
            StopAllCoroutines(); // Stop snapping if it was mid-snap
            
            // Re-enable physics so the physical hands can interact with it again
            currentlyHeldObject.isKinematic = false;
            currentlyHeldObject = null;

            // Reset bubble visuals
            if (hoveringObjects.Count == 0)
            {
                SetBubbleScale(originalScale);
            }
        }
    }

    // --- TWEENING COROUTINES ---

    private IEnumerator SnapToCenterRoutine(Rigidbody rb)
    {
        yield return new WaitForSeconds(0.1f);
        // 1. Disable physics to lock it in place and prevent hand collisions from sending it flying
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float elapsedTime = 0f;
        Vector3 startPos = rb.position;
        Quaternion startRot = rb.rotation;

        // 2. Tween the object to the exact center of the bubble
        while (elapsedTime < snapTweenDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / snapTweenDuration); // Smooth easing

            // Use MovePosition instead of transform.position to play nicely with the physics engine
            rb.MovePosition(Vector3.Lerp(startPos, transform.position, t));
            rb.MoveRotation(Quaternion.Slerp(startRot, transform.rotation, t));
            
            yield return null;
        }

        // 3. Guarantee perfect final alignment
        rb.MovePosition(transform.position);
        rb.MoveRotation(transform.rotation);
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
}