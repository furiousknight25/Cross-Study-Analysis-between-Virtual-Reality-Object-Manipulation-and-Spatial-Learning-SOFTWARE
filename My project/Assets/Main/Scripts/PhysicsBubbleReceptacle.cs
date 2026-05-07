using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PhysicsBubbleReceptacle : MonoBehaviour
{
    
    [Header("Starting State")]
    [Tooltip("Assign a Rigidbody here in the Inspector to have it start already snapped inside the bubble.")]
    public Rigidbody startingObject; 

    [Header("Magnetic Snap Settings (PID)")]
    public float positionalSpring = 25f; 
    public float rotationalSpring = 15f; 
    public float maxVelocity = 5f;       


    [Header("Visual Settings")]
    public float hoverScaleMultiplier = 1.2f;
    public float scaleTweenDuration = 0.2f;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    public Material controlCubeMaterial; // NEW: Slot for a URP material

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
            // FIX: Tell the grabbable we are an option, but don't force it to map to us yet
            GrabbableItem grabbable = rb.GetComponent<GrabbableItem>();
            if (grabbable != null && grabbable.isInBubble)
            {
                return; // If the item is already in a bubble, ignore it
            }

            if (grabbable != null)
            {
                grabbable.AddHoveredBubble(this);
            }

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
            // FIX: Tell the grabbable we are no longer an option
            GrabbableItem grabbable = rb.GetComponent<GrabbableItem>();
            if (grabbable != null)
            {
                grabbable.RemoveHoveredBubble(this);
            }

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

        if (!hoveringObjects.Contains(rb))
        {
            hoveringObjects.Add(rb);
        }

        // Set the two-way reference and SHRINK it
        GrabbableItem grabbable = rb.GetComponent<GrabbableItem>();
        if (grabbable != null)
        {
            grabbable.currentBubble = this;
            grabbable.AddHoveredBubble(this); // Ensure it's in the hovered list for visual consistency
            grabbable.ShrinkItem(); // NEW
        }

        // Instantly teleport it so it doesn't "fly" to the center on frame 1
        rb.position = transform.position;
        rb.rotation = transform.rotation;

        StartCoroutine(SnapToCenterRoutine(rb));
    }

    // --- INTEGRATION METHODS ---

    public void TrySnapObject(Rigidbody droppedObject)
    {
        // Only snap if we are empty
        if (currentlyHeldObject == null)
        {
            Debug.Log("Snapping started!");
            currentlyHeldObject = droppedObject;
            
            // Set the two-way reference and SHRINK it
            GrabbableItem grabbable = droppedObject.GetComponent<GrabbableItem>();
            if (grabbable != null)
            {
                grabbable.currentBubble = this;
                grabbable.ShrinkItem(); // NEW
            }

            StartCoroutine(SnapToCenterRoutine(droppedObject));
        }
    }


    public void RemoveObject(Rigidbody grabbedObject)
    {
        if (currentlyHeldObject == grabbedObject)
        {
            StopAllCoroutines(); 
            
            currentlyHeldObject.useGravity = true; 
            
            // Clear reference and RESTORE size
            GrabbableItem grabbable = grabbedObject.GetComponent<GrabbableItem>();
            if (grabbable != null)
            {
                grabbable.currentBubble = null;
                grabbable.RestoreSize(); // NEW
            }

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
            
            // Teleport if more than 1 meter away
            if (positionError.magnitude > 1f)
            {
                rb.position = transform.position;
                rb.linearVelocity = Vector3.zero;
                positionError = Vector3.zero;
            }
            
            Vector3 desiredVelocity = positionError * positionalSpring;
            
            rb.linearVelocity = Vector3.ClampMagnitude(desiredVelocity, maxVelocity);

            // --- ROTATIONAL SPRING ---
            Quaternion rotationError = transform.rotation * Quaternion.Inverse(rb.rotation);
            rotationError.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

            if (angleInDegrees > 180f) angleInDegrees -= 360f;

            if (Mathf.Abs(angleInDegrees) > 0.1f)
            {
                Vector3 desiredAngularVelocity = (angleInDegrees * Mathf.Deg2Rad) * rotationAxis * rotationalSpring;
                rb.angularVelocity = desiredAngularVelocity;
            }
            else
            {
                rb.angularVelocity = Vector3.zero;
            }

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

  // --- VISUAL TOGGLES ---

    public void HideVisuals()
    {
        // 1. Hide the holographic bubble
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        // 2. Hide the held object and disable its collisions so it can't be grabbed early
        if (currentlyHeldObject != null)
        {
            Renderer[] heldRenderers = currentlyHeldObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in heldRenderers) r.enabled = false;

            Collider[] heldColliders = currentlyHeldObject.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in heldColliders) c.enabled = false;
        }
    }

    public void ShowVisuals()
    {
        // 1. Always show the holographic bubble itself
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            r.enabled = true;
        }

        // 2. State-Driven Object Reveal
        if (currentlyHeldObject != null)
        {
            bool isControlMode = Director.Instance != null && Director.Instance.isControlGroup;

            if (isControlMode)
            {
                // Ensure the proxy cube has been generated
                TransformIntoControlCube(currentlyHeldObject);

                // Turn OFF all original meshes and colliders on the Ducky/Item
                Renderer[] heldRenderers = currentlyHeldObject.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in heldRenderers) r.enabled = false;

                Collider[] heldColliders = currentlyHeldObject.GetComponentsInChildren<Collider>(true);
                foreach (Collider c in heldColliders) c.enabled = false;

                // Explicitly turn ON ONLY the Control Cube and its Collider
                Transform cube = currentlyHeldObject.transform.Find("ControlGroupCube");
                if (cube != null)
                {
                    cube.gameObject.SetActive(true);
                    
                    Renderer cubeR = cube.GetComponent<Renderer>();
                    if (cubeR != null) cubeR.enabled = true;

                    Collider cubeC = cube.GetComponent<Collider>();
                    if (cubeC != null) cubeC.enabled = true;
                }
            }
            else
            {
                // NORMAL MODE: Turn ON all original meshes and colliders
                Renderer[] heldRenderers = currentlyHeldObject.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in heldRenderers) r.enabled = true;

                Collider[] heldColliders = currentlyHeldObject.GetComponentsInChildren<Collider>(true);
                foreach (Collider c in heldColliders) c.enabled = true;

                // Turn OFF the proxy cube so it doesn't overlap the Ducky
                Transform cube = currentlyHeldObject.transform.Find("ControlGroupCube");
                if (cube != null)
                {
                    cube.gameObject.SetActive(false);
                }
            }
        }
    }

public void TransformIntoControlCube(Rigidbody rb)
    {
        // 1. Prevent doing this twice if the cube already exists
        if (rb.transform.Find("ControlGroupCube") != null) return;

        // 2. Create the generic proxy cube
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "ControlGroupCube";
        
        // 3. Parent it to the original root so the Rigidbody and GrabbableItem scripts still work
        cube.transform.SetParent(rb.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localRotation = Quaternion.identity;

        // 4. Force the cube to be exactly 10cm in world space using lossyScale math
        Vector3 parentScale = rb.transform.lossyScale;
        cube.transform.localScale = new Vector3(
            0.1f / parentScale.x, 
            0.1f / parentScale.y, 
            0.1f / parentScale.z
        );

        Renderer cubeR = cube.GetComponent<Renderer>();
        if (cubeR != null) 
        {
            // FIX: Apply the URP material to get rid of the purple
            if (controlCubeMaterial != null)
            {
                cubeR.material = controlCubeMaterial;
            }
            cubeR.enabled = false;
        }
    }
}