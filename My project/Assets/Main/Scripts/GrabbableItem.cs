using UnityEngine;
using Leap.PhysicalHands;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhysicalHandEvents))] 
public class GrabbableItem : MonoBehaviour
{
    private Rigidbody rb;
    private PhysicalHandEvents handEvents; 
    
    [HideInInspector] 
    public PhysicsBubbleReceptacle currentBubble; 

    // --- FIX: Tracks multiple overlaps to prevent double-mapping ---
    private List<PhysicsBubbleReceptacle> overlappingBubbles = new List<PhysicsBubbleReceptacle>();

    [Header("Scaling Features")]
    [Tooltip("How small the item gets when slotted in a bubble (0.5 = half size)")]
    public float inBubbleScaleMultiplier = 0.5f; 
    public float scaleSpeed = 0.2f;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        handEvents = GetComponent<PhysicalHandEvents>(); 
        originalScale = transform.localScale; // Remember initial size
    }

    private void OnEnable()
    {
        if (handEvents != null)
        {
            handEvents.onGrabEnter.AddListener(HandleGrabEnter);
            handEvents.onGrabExit.AddListener(HandleGrabExit);
        }
    }

    private void OnDisable()
    {
        if (handEvents != null)
        {
            handEvents.onGrabEnter.RemoveListener(HandleGrabEnter);
            handEvents.onGrabExit.RemoveListener(HandleGrabExit);
        }
    }

    // --- NEW: Bubble Overlap Management ---

    public void AddHoveredBubble(PhysicsBubbleReceptacle bubble)
    {
        if (!overlappingBubbles.Contains(bubble))
        {
            overlappingBubbles.Add(bubble);
        }
    }

    public void RemoveHoveredBubble(PhysicsBubbleReceptacle bubble)
    {
        if (overlappingBubbles.Contains(bubble))
        {
            overlappingBubbles.Remove(bubble);
        }
    }

    private PhysicsBubbleReceptacle GetClosestValidBubble()
    {
        // Clean up any destroyed bubbles from the list just in case
        overlappingBubbles.RemoveAll(b => b == null);

        PhysicsBubbleReceptacle closest = null;
        float closestDist = float.MaxValue;

        foreach (var bubble in overlappingBubbles)
        {
            // Only consider bubbles that are EMPTY
            if (bubble.currentlyHeldObject == null)
            {
                float dist = Vector3.Distance(transform.position, bubble.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = bubble;
                }
            }
        }
        return closest;
    }

    // --- NEW: Scale Management ---

    public void ShrinkItem()
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleRoutine(originalScale * inBubbleScaleMultiplier));
    }

    public void RestoreSize()
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleRoutine(originalScale));
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale)
    {
        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;
        
        while (elapsedTime < scaleSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / scaleSpeed);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        transform.localScale = targetScale;
    }


    // --- WRAPPER METHODS ---

    private void HandleGrabEnter(ContactHand hand)
    {
        OnPhysicalHandGrabbed(); 
    }

    private void HandleGrabExit(ContactHand hand)
    {
        OnPhysicalHandReleased(); 
    }

    // --- YOUR ORIGINAL LOGIC ---

    public void OnPhysicalHandGrabbed()
    {
        if (currentBubble != null)
        {
            currentBubble.RemoveObject(rb);
            rb.useGravity = true;   
        }
    }

    public void OnPhysicalHandReleased()
    {
        // FIX: Find the absolute closest empty bubble from our overlap list
        PhysicsBubbleReceptacle targetBubble = GetClosestValidBubble();
        
        if (targetBubble != null)
        {
            targetBubble.TrySnapObject(rb); 
        }
        
        // else
        // {
        //     rb.useGravity = true;
        // }
    }
}