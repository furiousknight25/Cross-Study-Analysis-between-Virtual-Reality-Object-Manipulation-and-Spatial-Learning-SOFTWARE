using System;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Director : MonoBehaviour
{
    public static Director Instance { get; private set; }

    // --- FINITE STATE MACHINE ---
    public enum ExperimentState
    {
        ReadyToLoadTrial,         
        WaitingForPlayerToEnter,  
        Exploration,              
        ReadingAndInventory,      
        Encoding,                 
        WaitingForPlayerToExit,   
        Distraction,              
        Testing,                  
        Complete
    }

    [Header("Experiment State")]
    public ExperimentState CurrentState = ExperimentState.ReadyToLoadTrial; 

    public event Action OnDoorToggle;

    [Header("References")]
    public DistractionTask distractionTask;
    public List<TrialScene> trialScenes = new List<TrialScene>();
    public TrialScene tutorialScene;
    public List<GrabbableItem> tutorialItems = null;
    public TMP_Text instructionText = null;
    public SentenceBuilderManager sentenceBuilder;
    public TestingEnvironmentManager testingEnvironment;
    
    [Header("Settings - Main Trials")]
    public bool isControlGroup;
    public float explore_time = 120f; 
    public float reading_time = 20f; 
    public float encode_time = 70f; 
    public float distraction_task_duration = 120f;

    [Header("Settings - Tutorial (First Level)")]
    public float tutorial_explore_time = 30f;
    public float tutorial_encode_time = 30f;
    public float tutorial_distraction_duration = 30f; 

    private bool tutorial_completed = false; 
    private ExperimentTrialData lastCompletedTrialData; 
    private TrialScene lastCompletedTrialScene; // NEW: Stores the physical room
    private TrialScene next_trial = null;
    private bool isTransitioning = false; 

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Update()
    {
        if (Keyboard.current.nKey.wasPressedThisFrame) ButtonPressed();
        if (Keyboard.current.cKey.wasPressedThisFrame) SetControlGroupMode();
    }

    public void RegisterTrialScene(TrialScene trialScene)
    {
        if (trialScene == null) return;
        if (!trialScenes.Contains(trialScene)) trialScenes.Add(trialScene);
    }

    public void SetState(ExperimentState newState)
    {
        CurrentState = newState;
        Debug.Log($"<color=cyan>[State Machine] Transitioned to: {newState}</color>");
    }

    public void ButtonPressed()
    {
        if (isTransitioning) return; 

        switch (CurrentState)
        {
            case ExperimentState.ReadyToLoadTrial:
                StartCoroutine(MoveToNextTrialCoroutine());
                break;
            case ExperimentState.WaitingForPlayerToEnter:
                StartCoroutine(StartExplorationCoroutine());
                break;
            case ExperimentState.ReadingAndInventory:
                StartCoroutine(StartEncodingCoroutine());
                break;
            case ExperimentState.WaitingForPlayerToExit:
                StartCoroutine(EndCurrentTrialCoroutine());
                break;
            case ExperimentState.Distraction:
                StartTestingPhase();
                break;
        }
    }

    private IEnumerator MoveToNextTrialCoroutine()
    {
        isTransitioning = true;
        
        if (!tutorial_completed && tutorialScene != null)
        {
            yield return StartCoroutine(clear_tutorial()); 
            next_trial = tutorialScene;
            tutorial_completed = true;
        }
        else
        {
            if (trialScenes.Count == 0)
            {
                EndExperiment();
                yield break; 
            }
            int index = UnityEngine.Random.Range(0, trialScenes.Count);
            next_trial = trialScenes[index];
            trialScenes.Remove(next_trial);
        }

        if (next_trial.tablet != null) next_trial.tablet.SetupTabletText(next_trial.trialData);

        yield return StartCoroutine(next_trial.TransitionInRoutine());

        OnDoorToggle?.Invoke(); 
        SetState(ExperimentState.WaitingForPlayerToEnter);
        isTransitioning = false;
    }

    private IEnumerator StartExplorationCoroutine()
    {
        isTransitioning = true;
        OnDoorToggle?.Invoke(); 
        SetState(ExperimentState.Exploration);
        isTransitioning = false;

        yield return StartCoroutine(next_trial.ExplorationRoutine());
    }

    private IEnumerator StartEncodingCoroutine()
    {
        isTransitioning = true;
        SetState(ExperimentState.Encoding);
        isTransitioning = false;

        yield return StartCoroutine(next_trial.EncodingRoutine());
    }

    public void OnEncodingFinished()
    {
        next_trial.trial_completed = true;
        next_trial.tablet.hideText();
        OnDoorToggle?.Invoke(); 
        SetState(ExperimentState.WaitingForPlayerToExit);
    }

   private IEnumerator EndCurrentTrialCoroutine()
    {
        isTransitioning = true;
        OnDoorToggle?.Invoke(); 
        yield return new WaitForSeconds(1f); 

        LogSpecificTrialData(next_trial);
        lastCompletedTrialData = next_trial.trialData;
        lastCompletedTrialScene = next_trial; // NEW: Save the room before we nullify next_trial!

        float timeToWait = (next_trial == tutorialScene) ? tutorial_distraction_duration : distraction_task_duration;

        next_trial.HideAllPhysicsAndRenderers();
        yield return StartCoroutine(next_trial.TransitionOutRoutine());

        next_trial = null; 
        SetState(ExperimentState.Distraction);
        isTransitioning = false;

        distractionTask.StartDistractionTask(timeToWait);
    }

    public void StartTestingPhase()
    {
        SetState(ExperimentState.Testing);
        if (sentenceBuilder != null) 
        {
            // NEW: Pass BOTH the data and the physical scene over
            sentenceBuilder.InitializeSentenceBuilder(lastCompletedTrialData, lastCompletedTrialScene);
        }
        testingEnvironment.StartTestingPhase(lastCompletedTrialData);
    }

    public void EndTestingPhase()
    {
        testingEnvironment.EndTestingPhase();
        lastCompletedTrialData = null; 
        lastCompletedTrialScene = null; // Clean it up
        
        if (LoggingManager.Instance != null) LoggingManager.Instance.SaveToDisk();
        
        SetState(ExperimentState.ReadyToLoadTrial);
        Debug.Log("<color=green>Testing complete. Awaiting remote to load next trial.</color>");
    }

    public void ToggleDoor() => OnDoorToggle?.Invoke();

    public void LogSpecificTrialData(TrialScene trial)
    {
        if (trial == null) return;
        foreach (Vector3 tp in trial.touch_points) LoggingManager.Instance.LogTelemetry("trial_touch_points", tp);
        foreach (Vector3 lp in trial.locus_points) LoggingManager.Instance.LogTelemetry("trial_locus_points", lp);
    }

    public void logHeadsetPosition(Vector3 position) => LoggingManager.Instance.LogTelemetry("headset_position", position);

    public void EndExperiment() 
    {
        SetState(ExperimentState.Complete);
        LoggingManager.Instance.LogEvent("Global", "Experiment_Complete");
        Debug.Log("<color=green>Experiment Complete. All data saved.</color>");
    }

    private IEnumerator clear_tutorial()
    {        
        foreach (GrabbableItem grabbable in tutorialItems) grabbable.OnPhysicalHandGrabbed();
        yield return new WaitForSeconds(1.0f);
        foreach (GrabbableItem grabbable in tutorialItems) if (grabbable != null) grabbable.gameObject.SetActive(false);
        if (instructionText != null) instructionText.gameObject.SetActive(false);
    }

    public void SetControlGroupMode()
    {
        isControlGroup = true;
        PhysicsBubbleReceptacle[] allBubbles = FindObjectsOfType<PhysicsBubbleReceptacle>();
        foreach (var bubble in allBubbles)
        {
            if (bubble.currentlyHeldObject != null) bubble.TransformIntoControlCube(bubble.currentlyHeldObject);
        }
    }
}