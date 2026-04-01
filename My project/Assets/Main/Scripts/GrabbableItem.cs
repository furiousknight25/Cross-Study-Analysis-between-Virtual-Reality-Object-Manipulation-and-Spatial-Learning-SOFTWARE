using UnityEngine;

public class GrabbableItem : MonoBehaviour
{
    private Rigidbody rb;
    
    // Hidden from inspector because it is assigned dynamically by the bubble at runtime
    [HideInInspector] 
    public PhysicsBubbleReceptacle currentBubble; 

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Triggered by your Ultraleap event
    public void OnPhysicalHandGrabbed()
    {
        // If we are currently slotted in a bubble, tell it to let us go
        if (currentBubble != null)
        {
            currentBubble.RemoveObject(rb);
        }
    }

    // Triggered by your Ultraleap event
    public void OnPhysicalHandReleased()
    {
        // If we let go while hovering inside a bubble's trigger zone, snap to it!
        if (currentBubble != null)
        {
            currentBubble.TrySnapObject(rb); 
        }
    }
}