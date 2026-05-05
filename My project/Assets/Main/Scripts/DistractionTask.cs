using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DistractionTask : MonoBehaviour
{
    [Tooltip("Reference to the SortingShelfManager that handles the items")]
    public SortingShelfManager shelfManager;

    [Tooltip("How long the tween takes in seconds")]
    public float tweenDuration = 1f;

    [Tooltip("Transform that should move separately when this object goes up/down")]
    public Transform auxiliaryTransform;

    [Tooltip("World position the auxiliary transform should move to when the main transform goes up")]
    public Vector3 auxiliaryTopPosition = new Vector3(-0.473f, 0.245f, -1.549f);

    [Tooltip("How far down the auxiliary transform should drop when the main transform goes down")]
    public float auxiliaryDropDistance = 5f;

    [Tooltip("End position for the main transform when it tweens down")]
    public Vector3 mainDownPosition = new Vector3(-0.36037f, 0.503f, -2.3717f);

    private Vector3 startPosition;
    private Vector3 auxiliaryStartPosition;
    private Coroutine distractionCoroutine;

    private void Start()
    {
        // Cache the starting position
        startPosition = transform.localPosition;

        // Remember auxiliary start position if assigned
        if (auxiliaryTransform != null)
        {
            auxiliaryStartPosition = auxiliaryTransform.localPosition;
        }

        // Find the SortingShelfManager if not assigned
        if (shelfManager == null)
        {
            shelfManager = FindObjectOfType<SortingShelfManager>();
        }

        // Hide all children renderers at start
        HideAllChildrenRenderers();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            StartDistractionTask();
        }
    }

    /// <summary>
    /// Starts the distraction task: tweens down, unfreezes items, waits 2 minutes,
    /// then freezes items and tweens back up.
    /// </summary>
    public void StartDistractionTask()
    {
        // Stop any existing distraction task
        if (distractionCoroutine != null)
        {
            StopCoroutine(distractionCoroutine);
        }

        distractionCoroutine = StartCoroutine(DistractionTaskCoroutine());
    }

    private IEnumerator DistractionTaskCoroutine()
    {
        Debug.Log("<color=magenta>Distraction Task started</color>");

        // Show all children renderers before tweening down
        ShowAllChildrenRenderers();

        // Tween the auxiliary transform down through the floor if assigned
        if (auxiliaryTransform != null)
        {
            yield return StartCoroutine(TweenTransform(auxiliaryTransform.localPosition, auxiliaryTransform.localPosition - Vector3.up * auxiliaryDropDistance, tweenDuration, auxiliaryTransform));
        }

        // Tween down to the configured end position
        yield return StartCoroutine(TweenTransform(mainDownPosition, tweenDuration));

        // Unfreeze all items and start the sorting task
        if (shelfManager != null)
        {
            shelfManager.ResetScore();
            shelfManager.UnfreezeAllItems();
        }
        Debug.Log("<color=magenta>Items unfrozen - distraction task active</color>");

        // Wait for 2 minutes
        yield return new WaitForSeconds(Director.Instance.distraction_task_duration);

        // Freeze all items and reset their positions
        if (shelfManager != null)
        {
            shelfManager.ResetAndFreezeAllItems();
        }
        Debug.Log("<color=magenta>Items frozen and reset</color>");

        // Tween back up 3 meters to starting position
        if (auxiliaryTransform != null)
        {
            StartCoroutine(TweenTransform(auxiliaryTransform.localPosition, auxiliaryTopPosition, tweenDuration, auxiliaryTransform));
        }

        yield return StartCoroutine(TweenTransform(startPosition, tweenDuration));

        // Hide all children renderers after tweening up
        HideAllChildrenRenderers();

        Debug.Log("<color=magenta>Distraction Task completed</color>");
        distractionCoroutine = null;
        Director.Instance.distraction_task_completed = true;

    }

    private IEnumerator TweenTransform(Vector3 endPos, float duration)
    {
        // Capture the exact current local position to prevent ANY snapping
        Vector3 currentStartPos = transform.localPosition;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / duration);

            // Smooth easing (ease-in-out)
            t = t * t * (3f - 2f * t);

            transform.localPosition = Vector3.Lerp(currentStartPos, endPos, t);
            yield return null;
        }

        // Snap to final perfectly exact position
        transform.localPosition = endPos;
    }   
    
     /// <summary>
    /// Tweens a specific transform from startPos to endPos over a duration.
    /// </summary>
    private IEnumerator TweenTransform(Vector3 startPos, Vector3 endPos, float duration, Transform targetTransform)
    {
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / duration);

            // Smooth easing (ease-in-out)
            t = t * t * (3f - 2f * t);

            targetTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        targetTransform.localPosition = endPos;
    }

    /// <summary>
    /// Hides all Renderer components on children of this transform.
    /// </summary>
    private void HideAllChildrenRenderers()
    {
        GrabbableItem[] grabbableItems = GetComponentsInChildren<GrabbableItem>(includeInactive: true);
        foreach (GrabbableItem grabbable in grabbableItems)
        {
            grabbable.OnPhysicalHandGrabbed();
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
    }

    /// <summary>
    /// Shows all Renderer components on children of this transform.
    /// </summary>
    private void ShowAllChildrenRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }
    }
}
