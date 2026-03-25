using System.Collections;
using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [Tooltip("How far and in which direction the door slides. (X, Y, Z)")]
    public Vector3 slideOffset = new Vector3(2f, 0f, 0f);

    [Tooltip("How long it takes to open or close, in seconds.")]
    public float slideDuration = 1f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen = false;
    private Coroutine slideCoroutine;

    void Start()
    {
        // 1. Calculate our start and end positions
        closedPosition = transform.localPosition;
        openPosition = closedPosition + slideOffset;

        // 2. Connect to the Singleton Director
        // We do this in Start() so we know the Director has already run its Awake() setup.
        if (Director.Instance != null)
        {
            Director.Instance.OnDoorToggle += ToggleDoor;
        }
        else
        {
            Debug.LogWarning("SlidingDoor: Could not find the Director Singleton!");
        }
    }

    void OnDestroy()
    {
        // 3. CRITICAL: Always disconnect when this object is destroyed.
        // This prevents Unity from trying to tell a deleted door to open.
        if (Director.Instance != null)
        {
            Director.Instance.OnDoorToggle -= ToggleDoor;
        }
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen; // Flip state

        // Stop the door if it's currently mid-slide so it can smoothly reverse
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }
        
        Vector3 targetPosition = isOpen ? openPosition : closedPosition;
        slideCoroutine = StartCoroutine(SlideToPosition(targetPosition));
    }

    private IEnumerator SlideToPosition(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.localPosition;
        float timeElapsed = 0f;

        while (timeElapsed < slideDuration)
        {
            timeElapsed += Time.deltaTime;
            
            // Calculate progress from 0 to 1
            float t = timeElapsed / slideDuration;
            
            // Simple math formula to make the slide smooth (Ease-in, Ease-out)
            t = t * t * (3f - 2f * t); 

            // Move the mesh
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            
            // Wait until the next frame
            yield return null; 
        }
        
        // Snap perfectly to the final position at the end
        transform.localPosition = targetPosition; 
    }
}