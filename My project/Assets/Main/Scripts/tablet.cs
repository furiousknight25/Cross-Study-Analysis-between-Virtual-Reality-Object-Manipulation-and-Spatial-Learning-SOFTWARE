using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Tablet : MonoBehaviour
{
    public event Action OnSpawnItemsRequested;

    public List<PhysicsBubbleReceptacle> receptacles = new List<PhysicsBubbleReceptacle>();
    public Transform startPoint;
    public bool can_spawn_text = false;
    public Transform button;
    public Transform bodyone;
    public Transform bodytwo;
    [SerializeField] private TextMeshPro textMeshPro;
    
    [Header("Audio")]
    [Tooltip("Drag the AudioSource containing your ding sound here.")]
    public AudioSource dingAudioSource; // NEW: Reference to the audio source
    void Update()
    {
        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            Debug.Log("<color=yellow>[DEBUG] M key pressed - Emitting OnSpawnItemsRequested event</color>");
            ShowText();
        }
    }

    public void SetupTabletText(ExperimentTrialData trialData)
    {
        if (trialData != null && textMeshPro != null)
        {
            textMeshPro.text = trialData.FullPassage; 
        }
    }

    public void showButton()
    {
       button.gameObject.SetActive(true);
       bodyone.gameObject.SetActive(true);
       bodytwo.gameObject.SetActive(true);
       // NEW: Play the ding sound when the button is shown
       if (dingAudioSource != null)
       {
           dingAudioSource.Play();
       }
    }

    public void ShowText()
    {
        if (can_spawn_text)
        {
            textMeshPro.gameObject.SetActive(true);  
            OnSpawnItemsRequested?.Invoke();   
        }
    }

    public void hideText()
    {
        if (textMeshPro != null) textMeshPro.gameObject.SetActive(false);     
    }
    
    void Start()
    {
        hideText();
        button.gameObject.SetActive(false);
       bodyone.gameObject.SetActive(false);
       bodytwo.gameObject.SetActive(false);
    }

    public IEnumerator spawn_items()
    {
        
        
        for (int i = 0; i < receptacles.Count; i++)
        {
            Vector3 homePosition = receptacles[i].transform.position;
            receptacles[i].transform.position = startPoint.position;
            receptacles[i].ShowVisuals();
            StartCoroutine(TweenBubble(receptacles[i].transform, homePosition, 0.8f));
            yield return new WaitForSeconds(0.2f);
        }
        hideText();
        
       button.gameObject.SetActive(false);
       
       bodyone.gameObject.SetActive(false);

       bodytwo.gameObject.SetActive(false);
    }

    IEnumerator TweenBubble(Transform bubble, Vector3 targetPosition, float duration)
    {
        Vector3 initialPosition = bubble.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bubble.position = Vector3.Lerp(initialPosition, targetPosition, t);
            yield return null;
        }

        bubble.position = targetPosition;
    }
}