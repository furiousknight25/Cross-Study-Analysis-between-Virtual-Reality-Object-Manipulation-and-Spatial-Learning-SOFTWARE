using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class Director : MonoBehaviour
{
    public static Director Instance { get; private set; }

    public event Action OnDoorToggle;

    public List<TrialScene> trialScenes = new List<TrialScene>();
    public Dictionary<TrialScene, string> trialDataList = new Dictionary<TrialScene, string>(); //might not exist?
    private List<ExperimentEvent> currentSessionLog = new List<ExperimentEvent>();

    public bool isControlGroup;
    public float explore_time = 300f;
    public float reading_time = 20f;
    public float encode_time = 70f; //pickup, walk, encode

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
        // For testing: Press 'N' to move to the next trial
        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            Debug.Log("N");
            MoveToNextTrial();
        }
    }

    public void RegisterTrialScene(TrialScene trialScene)
    {
        if (trialScene == null)
            return;

        if (!trialScenes.Contains(trialScene))
        {
            trialScenes.Add(trialScene);
            Debug.Log($"Registered TrialScene: {trialScene.name}");
        }
    }

    public TrialScene[] GetPackedTrialScenes()
    {
        return trialScenes.ToArray();
    }


    public void MoveToNextTrial()
    {
        TrialScene next_trial = null;
        if (trialScenes.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, trialScenes.Count);
            next_trial = trialScenes[index];
        }
        trialScenes.Remove(next_trial);
        StartCoroutine(next_trial.StartTrial());

        OnDoorToggle?.Invoke(); //open door
        //once player walks past the door close it behind them
        //OnDoorToggle?.Invoke(); //close door
        // Debug.Log("Moving to the next trial...");
    }

    public void EndExperiment() //currentSessionLog.Add(new ExperimentEvent("trial1_touch", touchPos));
    {
        // Dumps the entire tidy list into the CSV
        ExperimentLogger.SaveToCSV(currentSessionLog, "participant_001");
    }

    public void logHeadsetPosition(Vector3 position)
    {
        currentSessionLog.Add(new ExperimentEvent("headset_position", position));
         ExperimentLogger.SaveToCSV(currentSessionLog, "participant_001");
    }


}