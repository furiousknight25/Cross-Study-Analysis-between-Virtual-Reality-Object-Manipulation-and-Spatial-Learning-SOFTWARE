using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestingEnvironmentManager : MonoBehaviour
{
    [Header("Movement Settings")]
    public float tweenDuration = 1.5f;
    public Vector3 hiddenPosition = new Vector3(0, 5f, 0); 
    public Vector3 activePosition = new Vector3(0, 0, 0);  

    [Header("Data & Event Channels")]
    public IntEventChannelSO slotSelectedChannel;
    public StringEventChannelSO foilSelectedChannel;

    [Header("UI References")]
    [Tooltip("Drag the 'Words' parent GameObject here. The script will automatically find all 30 buttons inside it.")]
    public Transform wordsParent;
    
    // Hidden in inspector because the script fills this out automatically now!
    [HideInInspector]
    public FoilWordBroadcaster[] allFoilButtons;

    private Coroutine movementCoroutine;

    private void Awake()
    {
        // Automatically grab all 30 buttons from the children of the Words object
        if (wordsParent != null)
        {
            // The 'true' parameter tells it to also find inactive children
            allFoilButtons = wordsParent.GetComponentsInChildren<FoilWordBroadcaster>(true);
            
            if (allFoilButtons.Length != 30)
            {
                Debug.LogWarning($"<color=orange>Warning: Expected 30 foil buttons, but found {allFoilButtons.Length} inside {wordsParent.name}!</color>");
            }
        }
        else
        {
            Debug.LogError("Words Parent is missing! Please assign it in the TestingEnvironmentManager.");
        }
    }

    private void OnEnable()
    {
        if (slotSelectedChannel != null)
            slotSelectedChannel.OnEventRaised += HandleSlotSelected;
        
        if (foilSelectedChannel != null)
            foilSelectedChannel.OnEventRaised += HandleFoilSelected;
    }

    private void OnDisable()
    {
        if (slotSelectedChannel != null)
            slotSelectedChannel.OnEventRaised -= HandleSlotSelected;
            
        if (foilSelectedChannel != null)
            foilSelectedChannel.OnEventRaised -= HandleFoilSelected;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            ShowAllFoils();
        }
    }

    private void Start()
    {
        transform.localPosition = hiddenPosition;
        HideAllFoils();
    }

    public void StartTestingPhase(ExperimentTrialData trialData)
    {
        Debug.Log("<color=cyan>Testing Phase Started</color>");
        
        PopulateFoilButtons(trialData);
        HideAllFoils();
        
        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(TweenTransform(activePosition, tweenDuration));
    }

    public void EndTestingPhase()
    {
        Debug.Log("<color=cyan>Testing Phase Completed - Sliding Up</color>");
        HideAllFoils();
        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(TweenTransform(hiddenPosition, tweenDuration));
    }

private void PopulateFoilButtons(ExperimentTrialData trialData)
    {
        if (allFoilButtons == null || allFoilButtons.Length < 30) return;

        // Python already combined the target + 4 foils and shuffled them into a 30-item array.
        // We just map the 30 JSON items directly to the 30 UI buttons.
        for (int i = 0; i < 30; i++)
        {
            if (i < trialData.Foils.Length)
            {
                allFoilButtons[i].SetupWord(trialData.Foils[i]);
            }
        }
    }

    private void HandleSlotSelected(int slotIndex)
    {
        HideAllFoils();

        if (allFoilButtons == null) return;

        int startIndex = slotIndex * 5;
        for (int i = startIndex; i < startIndex + 5; i++)
        {
            if (i < allFoilButtons.Length)
            {
                if (allFoilButtons[i] != null && allFoilButtons[i].buttonText != null)
                {
                    allFoilButtons[i].buttonText.gameObject.SetActive(true);
                }
            }
        }
    }

    private void HandleFoilSelected(string selectedWord)
    {
        HideAllFoils();
    }

    private void HideAllFoils()
    {
        if (allFoilButtons == null) return;
        
        foreach (var btn in allFoilButtons)
        {
            if (btn != null && btn.buttonText != null)
            {
                btn.buttonText.gameObject.SetActive(false);
            }
        }
    }

    private void ShowAllFoils()
    {
        if (allFoilButtons == null) return;
        
        foreach (var btn in allFoilButtons)
        {
            if (btn != null && btn.buttonText != null)
            {
                btn.buttonText.gameObject.SetActive(true);
            }
        }
    }

    private void ShuffleList(List<string> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            string temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    private IEnumerator TweenTransform(Vector3 endPos, float duration)
    {
        Vector3 startPos = transform.localPosition;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / duration);
            t = t * t * (3f - 2f * t); 
            transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        transform.localPosition = endPos;
    }
}