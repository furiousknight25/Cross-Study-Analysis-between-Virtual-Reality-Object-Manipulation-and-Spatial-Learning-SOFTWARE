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

    private void OnTabletSpawnItemsRequested()
    {
        StartCoroutine(ReadingAndSpawnRoutine());
    }

    private IEnumerator ReadingAndSpawnRoutine()
    {
        tabletEventReceived = true; 
        HideRoomVisuals(); 

        Debug.Log($"<color=white>[TrialScene] Reading phase started. Timer: {Director.Instance.reading_time}s</color>");
        yield return StartCoroutine(StartTimer(Director.Instance.reading_time));
        
        string currentTrialId = trialData != null ? trialData.TrialID : "Unknown_Trial";
        StartCoroutine(tablet.spawn_items(currentTrialId)); 
        
        Director.Instance.SetState(Director.ExperimentState.ReadingAndInventory);
    }

    // --- TELEMETRY: Encoding Routine with 3-Second Logging ---
    public IEnumerator EncodingRoutine()
    {
        ShowRoomVisuals();
        
        float timeToWait = IsTutorial ? Director.Instance.tutorial_encode_time : Director.Instance.encode_time;
        
        Debug.Log($"<color=white>[TrialScene] Encoding started. Timer: {timeToWait}s</color>");
        
        // Start the background stillness tracker
        Coroutine monitorCoroutine = StartCoroutine(MonitorItemStillness());

        yield return StartCoroutine(StartTimer(timeToWait));
        
        // Stop the tracker when encoding is over
        if (monitorCoroutine != null) StopCoroutine(monitorCoroutine);
        
        // Log the final resting positions of all objects
        LogFinalItemPositions();

        // 🚨 NEW: Force drop items from hands and hide everything instantly
        ForceReleaseAndHideSessionObjects();

        // Hide the room visuals immediately as the phase ends and the door opens
        HideRoomVisuals();

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

    // ==========================================
    // LOGGING & CLEANUP MANAGERS
    // ==========================================

    private void ForceReleaseAndHideSessionObjects()
    {
        if (enviornment_parant == null) return;

        string currentTrialId = trialData != null ? trialData.TrialID : "Unknown_Trial";

        // 1. Force drop and deactivate all items in the room
        GrabbableItem[] items = enviornment_parant.GetComponentsInChildren<GrabbableItem>(true);
        foreach (var item in items)
        {
            if (item != null && item.gameObject.activeSelf)
            {
                // Explicit telemetry milestone tracking for kinematic analysis
                if (LoggingManager.Instance != null)
                {
                    LoggingManager.Instance.LogEvent(currentTrialId, "Forced_Release_Phase_End", -1, item.ItemName);
                }

                // Deactivating the object completely detaches any Leap hands and makes it disappear cleanly
                item.gameObject.SetActive(false);
            }
        }

        // 2. Hide the tablet's room inventory bubbles so they aren't floating in empty space
        if (tablet != null && tablet.receptacles != null)
        {
            foreach (var receptacle in tablet.receptacles)
            {
                if (receptacle != null)
                {
                    receptacle.gameObject.SetActive(false);
                }
            }
        }
    }

    private IEnumerator MonitorItemStillness()
    {
        if (enviornment_parant == null) yield break;

        GrabbableItem[] items = enviornment_parant.GetComponentsInChildren<GrabbableItem>(true);
        Dictionary<GrabbableItem, float> timeStillMap = new Dictionary<GrabbableItem, float>();
        Dictionary<GrabbableItem, bool> hasLoggedMap = new Dictionary<GrabbableItem, bool>();

        foreach(var item in items)
        {
            timeStillMap[item] = 0f;
            hasLoggedMap[item] = false;
        }

        string currentTrialId = trialData != null ? trialData.TrialID : "Unknown_Trial";

        while(true)
        {
            foreach(var item in items)
            {
                if (item.isInBubble) 
                {
                    timeStillMap[item] = 0f;
                    hasLoggedMap[item] = false;
                    continue;
                }

                Rigidbody rb = item.GetComponent<Rigidbody>();
                bool isStill = rb != null && rb.linearVelocity.sqrMagnitude < 0.005f && rb.angularVelocity.sqrMagnitude < 0.005f;

                if (isStill)
                {
                    timeStillMap[item] += 0.2f; 
                    
                    if (timeStillMap[item] >= 3.0f && !hasLoggedMap[item])
                    {
                        hasLoggedMap[item] = true; 
                        
                        string posStr = $"{item.transform.position.x:F3},{item.transform.position.y:F3},{item.transform.position.z:F3}";
                        Debug.Log($"<yellow>[Log] {item.ItemName} has been still for 3s at {posStr}</yellow>");

                        if (LoggingManager.Instance != null)
                        {
                            LoggingManager.Instance.LogEvent(currentTrialId, "Item_Placed_3s", -1, $"{item.ItemName}:{posStr}");
                        }
                    }
                }
                else
                {
                    timeStillMap[item] = 0f;
                    hasLoggedMap[item] = false;
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void LogFinalItemPositions()
    {
        if (enviornment_parant == null) return;

        GrabbableItem[] items = enviornment_parant.GetComponentsInChildren<GrabbableItem>(true);
        string currentTrialId = trialData != null ? trialData.TrialID : "Unknown_Trial";

        foreach (var item in items)
        {
            string positionLog = "null";
            
            if (!item.isInBubble)
            {
                positionLog = $"{item.transform.position.x:F3},{item.transform.position.y:F3},{item.transform.position.z:F3}";
            }

            if (LoggingManager.Instance != null)
            {
                LoggingManager.Instance.LogEvent(currentTrialId, "Encoding_Final_Position", -1, $"{item.ItemName}:{positionLog}");
            }
            
            Debug.Log($"<color=green>[Encoding Final Log] {item.ItemName} -> {positionLog}</color>");
        }
    }

    // ==========================================
    // VISIBILITY MANAGERS
    // ==========================================

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