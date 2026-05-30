using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrialScene : MonoBehaviour
{
    public ExperimentTrialData trialData; 

    private bool tabletEventReceived = false;
    public List<Vector3> touch_points = new List<Vector3>();
    public List<Vector3> locus_points = new List<Vector3>();

    public Transform enviornment_parant;
    public Tablet tablet;
    public bool trial_completed = false;
    
    public Vector3 trialStartPosition = new Vector3(-1.4f, -0.06f, 1.48f); 
    public Vector3 trialEndPosition = new Vector3(-1.4f, 10.00f, 1.48f);

    // Dynamic check to see if this specific scene is the tutorial level
    private bool IsTutorial => Director.Instance != null && Director.Instance.tutorialScene == this;

    void OnEnable()
    {
        StartCoroutine(WaitAndRegister());
    }

    IEnumerator WaitAndRegister()
    {
        while (Director.Instance == null) yield return null; 
        Director.Instance.RegisterTrialScene(this);
    }

    public IEnumerator TransitionInRoutine()
    {
        transform.localScale *= 1.1f;
        yield return StartCoroutine(TweenPosition(trialStartPosition, 2f)); 
    }

    public IEnumerator ExplorationRoutine()
    {
        // Pull the correct timing based on whether this is the tutorial
        float timeToWait = IsTutorial ? Director.Instance.tutorial_explore_time : Director.Instance.explore_time;
        
        Debug.Log($"<color=white>[TrialScene] Exploration started. Timer: {timeToWait}s</color>");
        yield return StartCoroutine(StartTimer(timeToWait));
        
        tablet.can_spawn_text = true; 
        tablet.showButton();

        tabletEventReceived = false;
        tablet.OnSpawnItemsRequested += OnTabletSpawnItemsRequested;
        yield return new WaitUntil(() => tabletEventReceived);
        tablet.OnSpawnItemsRequested -= OnTabletSpawnItemsRequested;
    }

    // --- BUG FIX: Route the click into a timed Coroutine instead of spawning instantly ---
    private void OnTabletSpawnItemsRequested()
    {
        StartCoroutine(ReadingAndSpawnRoutine());
    }

    private IEnumerator ReadingAndSpawnRoutine()
    {
        tabletEventReceived = true; // Tell the ExplorationRoutine we clicked the button
        HideRoomVisuals(); 

        // 1. Wait for Reading Time (20 seconds)
        Debug.Log($"<color=white>[TrialScene] Reading phase started. Timer: {Director.Instance.reading_time}s</color>");
        yield return StartCoroutine(StartTimer(Director.Instance.reading_time));
        
        // 2. Spawn the items after reading is complete
        StartCoroutine(tablet.spawn_items()); 
        
        // 3. Tell the FSM we are ready for the remote to unhide the room
        Director.Instance.SetState(Director.ExperimentState.ReadingAndInventory);
    }

    public IEnumerator EncodingRoutine()
    {
        ShowRoomVisuals();
        
        // Pull the correct timing based on whether this is the tutorial
        float timeToWait = IsTutorial ? Director.Instance.tutorial_encode_time : Director.Instance.encode_time;
        
        Debug.Log($"<color=white>[TrialScene] Encoding started. Timer: {timeToWait}s</color>");
        yield return StartCoroutine(StartTimer(timeToWait));
        
        Director.Instance.OnEncodingFinished();
    }

    public IEnumerator TransitionOutRoutine()
    {
        yield return StartCoroutine(TweenPosition(trialEndPosition, 2f)); 
    }

    IEnumerator StartTimer(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    public void pointTouched(Vector3 point)
    {
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

    // --- VISIBILITY MANAGERS ---

    public void HideRoomVisuals()
    {
        if (enviornment_parant == null) return;
        Renderer[] renderers = enviornment_parant.GetComponentsInChildren<Renderer>(true);
        
        foreach (Renderer r in renderers)
        {
            if (r.GetComponentInParent<Tablet>() != null) continue;
            if (r.GetComponentInParent<GrabbableItem>() != null) continue;
            if (r.GetComponentInParent<PhysicsBubbleReceptacle>() != null) continue;

            if (r.transform != enviornment_parant)
            {
                r.enabled = false;
            }
        }
    }

    public void ShowRoomVisuals()
    {
        if (enviornment_parant == null) return;
        Renderer[] renderers = enviornment_parant.GetComponentsInChildren<Renderer>(true);
        
        foreach (Renderer r in renderers)
        {
            if (r.transform != enviornment_parant) r.enabled = true;
        }
    }

    public void HideAllPhysicsAndRenderers()
    {
        if (enviornment_parant == null) return;

        Renderer[] descendantRenderers = enviornment_parant.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in descendantRenderers)
        {
            if (renderer.transform != enviornment_parant) renderer.enabled = false;
        }

        StartCoroutine(DeactivateGrabbablesRoutine());
    }

    private IEnumerator DeactivateGrabbablesRoutine()
    {
        GrabbableItem[] grabbableItems = enviornment_parant.GetComponentsInChildren<GrabbableItem>(true);
        foreach (GrabbableItem grabbable in grabbableItems) grabbable.OnPhysicalHandGrabbed();

        yield return new WaitForSeconds(1.0f);

        foreach (GrabbableItem grabbable in grabbableItems)
        {
            if (grabbable != null) grabbable.gameObject.SetActive(false);
        }
    }
}