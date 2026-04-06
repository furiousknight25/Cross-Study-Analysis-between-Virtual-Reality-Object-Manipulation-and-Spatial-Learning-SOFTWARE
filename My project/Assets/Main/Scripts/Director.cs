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
    private List<ExperimentEvent> currentSessionLog = new List<ExperimentEvent>();
    public List<GrabbableItem> tutorialItems = null;
    public TMP_Text instructionText = null;
    
    public bool isControlGroup;
    public bool distraction_task_completed = true; 
    private bool tutorial_completed = false;
    public float explore_time = 2f; 
    public float reading_time = 2f; 
    public float encode_time = 2f; 
    public float distraction_task_duration = 3f;

    private TrialScene next_trial = null;
    private bool isTransitioning = false; // Prevents double-clicking bugs

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
        if (Keyboard.current.nKey.wasPressedThisFrame || Keyboard.current.vKey.wasPressedThisFrame)
        {
            if (!isTransitioning) 
            {
                ButtonPressed();
            }
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
            StartCoroutine(MoveToNextTrialCoroutine());
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
        Debug.Log(trialScenes);
        isTransitioning = true;

        if (trialScenes.Count == 0)
        {
            Debug.Log("<color=red>No more trials left!</color>");
            EndExperiment();
            isTransitioning = false;
            yield break; // Stop execution if no trials left
        }

        // Pick random trial
        int index = UnityEngine.Random.Range(0, trialScenes.Count);
        next_trial = trialScenes[index];
        trialScenes.Remove(next_trial);

        // Start the trial sequence
        StartCoroutine(next_trial.StartTrial());

        yield return new WaitForSeconds(2.2f); // delay for door
        OnDoorToggle?.Invoke(); // open door

        isTransitioning = false;
    }

    private IEnumerator EndCurrentTrialCoroutine()
    {
        isTransitioning = true;
        
        OnDoorToggle?.Invoke(); // close door
        yield return new WaitForSeconds(1f); // delay for door

        // Pass the specific trial we are ending to the logger
        LogSpecificTrialData(next_trial);
        
        // Let the scene animate out
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

    // Now takes a specific TrialScene so we don't rely on the modified list
    public void LogSpecificTrialData(TrialScene trial)
    {
        if (trial == null) return;

        foreach (Vector3 touch_point in trial.touch_points)
        {
            currentSessionLog.Add(new ExperimentEvent("trial_touch_points", touch_point));
        }
        foreach (Vector3 locus_point in trial.locus_points)
        {
            currentSessionLog.Add(new ExperimentEvent("trial_locus_points", locus_point));
        }
        
        // ExperimentLogger.SaveToCSV(currentSessionLog, "participant_001");
    }

    public void logHeadsetPosition(Vector3 position)
    {
        currentSessionLog.Add(new ExperimentEvent("headset_position", position));
        // ExperimentLogger.SaveToCSV(currentSessionLog, "participant_001");
    }

    public void EndExperiment() 
    {
        Debug.Log("Experiment Complete. Saving CSV...");
        // ExperimentLogger.SaveToCSV(currentSessionLog, "participant_001");
    }



    public void clear_tutorial()
    {
        foreach (GrabbableItem item in tutorialItems)
        {
            item.gameObject.SetActive(false);
        }
        instructionText.gameObject.SetActive(false);


    }
} 