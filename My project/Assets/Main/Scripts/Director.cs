using System;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Director : MonoBehaviour
{
    public static Director Instance { get; private set; }

    public event Action OnDoorToggle;

    public DistractionTask distractionTask;
    public List<TrialScene> trialScenes = new List<TrialScene>();
    
    public List<GrabbableItem> tutorialItems = null;
    public TMP_Text instructionText = null;
    
    public bool isControlGroup;
    public bool distraction_task_completed = true; 
    private bool tutorial_completed = false;
    public float explore_time = 2f; 
    public float reading_time = 2f; 
    public float encode_time = 50f; 
    public float distraction_task_duration = 3f;
    public SentenceBuilderManager sentenceBuilder;
    public TestingEnvironmentManager testingEnvironment;
    
    
    // REMOVED public ExperimentTrialData currentTrialData;
    // NEW: We store the data of the trial we just finished so we can test on it after distraction
    private ExperimentTrialData lastCompletedTrialData; 

    private TrialScene next_trial = null;
    private bool isTransitioning = false; 

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Update()
    {
        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            if (!isTransitioning) 
            {
                ButtonPressed();
            }
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            SetControlGroupMode();
        }
    }

    public void RegisterTrialScene(TrialScene trialScene)
    {
        if (trialScene == null) return;

        if (!trialScenes.Contains(trialScene))
        {
            trialScenes.Add(trialScene);
            Debug.Log($"Registered TrialScene: {trialScene.name}");
        }
    }

public void ButtonPressed()
    {
        if (next_trial == null && distraction_task_completed)
        {
            if (lastCompletedTrialData == null)
            {
                // If there's no saved data, we are at the very beginning of the experiment.
                // Start the first learning trial!
                StartCoroutine(MoveToNextTrialCoroutine());
            }
            else
            {
                // We have saved data, which means we just finished a distraction task. Time to test!
                StartTestingPhase();
            }
        }
        else if (next_trial != null && next_trial.trial_completed)
        {
            StartCoroutine(EndCurrentTrialCoroutine());
        }
    }

    public IEnumerator MoveToNextTrialCoroutine()
    {
        if (tutorial_completed == false)
        {
            clear_tutorial();
            tutorial_completed = true;
        }
        
        isTransitioning = true;

        if (trialScenes.Count == 0)
        {
            Debug.Log("<color=red>No more trials left!</color>");
            EndExperiment();
            isTransitioning = false;
            yield break; 
        }

        int index = UnityEngine.Random.Range(0, trialScenes.Count);
        next_trial = trialScenes[index];
        trialScenes.Remove(next_trial);

        StartCoroutine(next_trial.StartTrial());

        yield return new WaitForSeconds(2.2f); 
        OnDoorToggle?.Invoke(); 

        isTransitioning = false;
    }

    private IEnumerator EndCurrentTrialCoroutine()
    {
        isTransitioning = true;
        
        OnDoorToggle?.Invoke(); 
        yield return new WaitForSeconds(1f); 

        LogSpecificTrialData(next_trial);
        
        // NEW: Save the data from the trial we just finished BEFORE we nullify it!
        lastCompletedTrialData = next_trial.trialData;

        yield return StartCoroutine(next_trial.EndTrialSequence());

        next_trial = null; 
        distraction_task_completed = false;
        isTransitioning = false;

        Debug.Log("<color=magenta>Start Distraction Task Here</color>");
        distractionTask.StartDistractionTask();
    }

    public void ToggleDoor()
    {
        OnDoorToggle?.Invoke();
    }

    public void LogSpecificTrialData(TrialScene trial)
    {
        if (trial == null) return;

        foreach (Vector3 touch_point in trial.touch_points)
        {
            LoggingManager.Instance.LogTelemetry("trial_touch_points", touch_point);
        }
        foreach (Vector3 locus_point in trial.locus_points)
        {
            LoggingManager.Instance.LogTelemetry("trial_locus_points", locus_point);
        }
    }

    public void logHeadsetPosition(Vector3 position)
    {
        LoggingManager.Instance.LogTelemetry("headset_position", position);
    }

    public void EndExperiment() 
    {
        Debug.Log("Experiment Complete. All data has been successfully streamed to disk.");
        LoggingManager.Instance.LogEvent("Global", "Experiment_Complete");
    }

    public void clear_tutorial()
    {
        foreach (GrabbableItem item in tutorialItems)
        {
            item.gameObject.SetActive(false);
        }
        if (instructionText != null) instructionText.gameObject.SetActive(false);
    }

public void StartTestingPhase()
    {
        distraction_task_completed = false; 
        
        // NEW: Pass the trial data to the sentence builder so it can grade the answers!
        if (sentenceBuilder != null) 
        {
            sentenceBuilder.InitializeSentenceBuilder(lastCompletedTrialData);
        }

        testingEnvironment.StartTestingPhase(lastCompletedTrialData);
    }

public void EndTestingPhase()
    {
        testingEnvironment.EndTestingPhase();
        
        // 1. Clear the old trial data so the Director knows we are done testing it.
        // This ensures the next button press routes to a NEW trial, not back into the test.
        lastCompletedTrialData = null; 

        // 2. Set this to true so the 'N' key (ButtonPressed) is allowed to trigger the next phase.
        distraction_task_completed = true; 

        if (LoggingManager.Instance != null)
        {
            LoggingManager.Instance.SaveToDisk();
        }

        // REMOVED: StartCoroutine(MoveToNextTrialCoroutine());
        Debug.Log("Testing phase ended. Awaiting button press to start the next trial...");
    }

public void SetControlGroupMode()
    {
        isControlGroup = true;
        
        // Find all active bubbles in the scene
        PhysicsBubbleReceptacle[] allBubbles = FindObjectsOfType<PhysicsBubbleReceptacle>();
        
        foreach (var bubble in allBubbles)
        {
            if (bubble.currentlyHeldObject != null)
            {
                bubble.TransformIntoControlCube(bubble.currentlyHeldObject);
            }
        }
        
        Debug.Log($"<color=yellow>Global Control Group Mode set to: {true}</color>");
    }
}