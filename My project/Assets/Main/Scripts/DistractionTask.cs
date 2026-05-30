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
        startPosition = transform.localPosition;

        if (auxiliaryTransform != null)
        {
            auxiliaryStartPosition = auxiliaryTransform.localPosition;
        }

        if (shelfManager == null)
        {
            shelfManager = FindObjectOfType<SortingShelfManager>();
        }

        HideAllChildrenRenderers();
    }

    private void Update()
    {
        // Local Debugging only. Production runs via Director.cs FSM
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            float debugTime = Director.Instance != null ? Director.Instance.distraction_task_duration : 120f;
            StartDistractionTask(debugTime);
        }
    }

    /// <summary>
    /// Starts the distraction task: tweens down, unfreezes items, waits for the specified duration,
    /// then freezes items and tweens back up.
    /// </summary>
    public void StartDistractionTask(float duration)
    {
        if (distractionCoroutine != null)
        {
            StopCoroutine(distractionCoroutine);
        }
        distractionCoroutine = StartCoroutine(DistractionTaskCoroutine(duration));
    }

    private IEnumerator DistractionTaskCoroutine(float duration)
    {
        Debug.Log($"<color=magenta>[Distraction] Task started. Tweening in for {duration} seconds...</color>");

        ShowAllChildrenRenderers();

        if (auxiliaryTransform != null)
        {
            yield return StartCoroutine(TweenTransform(auxiliaryTransform.localPosition, auxiliaryTransform.localPosition - Vector3.up * auxiliaryDropDistance, tweenDuration, auxiliaryTransform));
        }

        yield return StartCoroutine(TweenTransform(mainDownPosition, tweenDuration));

        // Unfreeze all items and start the sorting task
        if (shelfManager != null)
        {
            shelfManager.ResetScore();
            shelfManager.UnfreezeAllItems();
        }
        Debug.Log("<color=magenta>[Distraction] Items unfrozen - Participant is sorting.</color>");

        // Wait for the dynamically passed duration
        yield return new WaitForSeconds(duration);

        // Freeze all items and reset their positions
        if (shelfManager != null)
        {
            shelfManager.ResetAndFreezeAllItems();
        }
        Debug.Log("<color=magenta>[Distraction] Time is up. Items frozen and reset.</color>");

        // Tween back up to starting position
        if (auxiliaryTransform != null)
        {
            StartCoroutine(TweenTransform(auxiliaryTransform.localPosition, auxiliaryTopPosition, tweenDuration, auxiliaryTransform));
        }

        yield return StartCoroutine(TweenTransform(startPosition, tweenDuration));
        HideAllChildrenRenderers();

        Debug.Log("<color=magenta>[Distraction] Task completed. Awaiting Remote Trigger to start Testing Phase.</color>");
        distractionCoroutine = null;
    }

    private IEnumerator TweenTransform(Vector3 endPos, float duration)
    {
        Vector3 currentStartPos = transform.localPosition;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / duration);
            t = t * t * (3f - 2f * t); // Smooth easing (ease-in-out)

            transform.localPosition = Vector3.Lerp(currentStartPos, endPos, t);
            yield return null;
        }
        transform.localPosition = endPos;
    }   
    
    private IEnumerator TweenTransform(Vector3 startPos, Vector3 endPos, float duration, Transform targetTransform)
    {
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / duration);
            t = t * t * (3f - 2f * t); 

            targetTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        targetTransform.localPosition = endPos;
    }

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

    private void ShowAllChildrenRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }
    }
}