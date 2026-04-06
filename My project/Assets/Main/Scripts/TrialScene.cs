using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrialScene : MonoBehaviour
{
    private bool tabletEventReceived = false;
    public List<Vector3> touch_points = new List<Vector3>();
    public List<Vector3> locus_points = new List<Vector3>();

    public Transform enviornment_parant; // (Note: typo in variable name kept to match your inspector)
    public Tablet tablet;
    public bool trial_completed = false;
    
    public Vector3 trialStartPosition = new Vector3(-1.4f, -0.06f, 1.48f); 
    public Vector3 trialEndPosition = new Vector3(-1.4f, 10.00f, 1.48f);

    void OnEnable()
    {
        StartCoroutine(WaitAndRegister());
    }

    IEnumerator WaitAndRegister()
    {
        while (Director.Instance == null)
        {
            yield return null; 
        }
        Director.Instance.RegisterTrialScene(this);
    }

    public IEnumerator StartTrial()
    {
        Debug.Log("Trial started");
        transform.localScale *= 1.1f;
        StartCoroutine(TweenPosition(trialStartPosition, 2f)); 
        
        yield return StartCoroutine(StartTimer(Director.Instance.explore_time));
        tablet.can_spawn_text = true; // Allow the tablet to show text and trigger events

        tabletEventReceived = false;
        tablet.OnSpawnItemsRequested += OnTabletSpawnItemsRequested;
        yield return new WaitUntil(() => tabletEventReceived);
        tablet.OnSpawnItemsRequested -= OnTabletSpawnItemsRequested;

        Debug.Log("Tablet event received, starting reading time");
        yield return StartCoroutine(StartTimer(Director.Instance.reading_time));
        
        Debug.Log("Reading time finished, starting encoding time");
        StartCoroutine(tablet.spawn_items());
        
        yield return StartCoroutine(StartTimer(Director.Instance.encode_time));
        Debug.Log("Encoding time finished, ending trial");
        
        trial_completed = true;
        HideAllChildrenRenderers();
    }

    // Converted to Coroutine to avoid cross-thread Unity API errors
    public IEnumerator EndTrialSequence()
    {
        yield return StartCoroutine(TweenPosition(trialEndPosition, 2f)); 
    }

    IEnumerator StartTimer(float seconds)
    {
        Debug.Log($"Timer started for {seconds} seconds");
        yield return new WaitForSeconds(seconds);
    }

    private void OnTabletSpawnItemsRequested()
    {
        tabletEventReceived = true;
    }

    public void pointTouched(Vector3 point)
    {
        Debug.Log("Point touched: " + point);
        touch_points.Add(point);
    }

    public IEnumerator TweenPosition(Vector3 targetPosition, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
    }

    public void HideAllChildrenRenderers()
    {
        if (enviornment_parant == null) return;

        // De-equip any grabbable objects before hiding their renderers.
        GrabbableItem[] grabbableItems = enviornment_parant.GetComponentsInChildren<GrabbableItem>(includeInactive: true);
        foreach (GrabbableItem grabbable in grabbableItems)
        {
            grabbable.OnPhysicalHandGrabbed();
        }

        // Hide every renderer on all descendants of the parent, including nested children.
        Renderer[] descendantRenderers = enviornment_parant.GetComponentsInChildren<Renderer>(includeInactive: true);
        foreach (Renderer renderer in descendantRenderers)
        {
            if (renderer.transform != enviornment_parant)
            {
                renderer.enabled = false;
            }
        }
    }
}