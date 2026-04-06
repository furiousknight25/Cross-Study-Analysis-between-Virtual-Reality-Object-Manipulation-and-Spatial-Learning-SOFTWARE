using UnityEngine;
using System.Threading.Tasks;

public class DirectorBridge : MonoBehaviour
{
    // We create a wrapper function that talks to the Singleton
    public Transform vrHeadset; // Assign this in the Inspector to your VR camera rig's headset transform
    public TrialScene trialSceneToAutoload; // Assign this in the Inspector to the TrialScene you want to start with
    
        void Start()
        {
            // Register the trial scene with the Director at the start of the experiment
            if (trialSceneToAutoload != null)
            {
                addTrialToAutoload(trialSceneToAutoload);
            }
        }
    
    public void CallNextTrial()
    {
        if (Director.Instance != null)
        {
            StartCoroutine(Director.Instance.MoveToNextTrialCoroutine() );
        }
        else
        {
            Debug.LogWarning("Director Instance is missing!");
        }
    }

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

    public void buttonPressed()
    {
        if (Director.Instance != null)
        {
            Director.Instance.ButtonPressed();
        }
    }
}