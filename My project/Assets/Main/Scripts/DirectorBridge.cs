using UnityEngine;

public class DirectorBridge : MonoBehaviour
{
    [Tooltip("Assign your VR camera rig's headset transform for continuous logging.")]
    public Transform vrHeadset; 
    
    [Tooltip("Assign the TrialScene you want to register at startup.")]
    public TrialScene trialSceneToAutoload; 
    
    void Start()
    {
        // Register the trial scene with the Director at the start of the experiment
        if (trialSceneToAutoload != null)
        {
            addTrialToAutoload(trialSceneToAutoload);
        }
    }
    
    // --- SAFE FSM TRIGGER ---
    /// <summary>
    /// This is the ONLY method that should be called by UI buttons or remote triggers 
    /// to advance the experiment. It respects the Finite State Machine.
    /// </summary>
    public void buttonPressed()
    {
        if (Director.Instance != null)
        {
            Director.Instance.ButtonPressed();
        }
        else
        {
            Debug.LogWarning("Director Instance is missing! Cannot advance state.");
        }
    }

    // --- UTILITIES ---
    public void CallEndExperiment()
    {
        if (Director.Instance != null)
        {
            Director.Instance.EndExperiment();
        }
    }
    
    public void logHeadsetPosition()
    {
        if (Director.Instance != null && vrHeadset != null)
        {
            Director.Instance.logHeadsetPosition(vrHeadset.position);
        }
    }

    public void addTrialToAutoload(TrialScene trialScene)
    {
        if (Director.Instance != null && trialScene != null)
        {
            Director.Instance.RegisterTrialScene(trialScene);
        }
    }
}