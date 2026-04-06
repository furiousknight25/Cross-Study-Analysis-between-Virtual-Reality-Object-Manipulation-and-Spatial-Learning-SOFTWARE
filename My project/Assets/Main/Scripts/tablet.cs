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
    [SerializeField] private TextMeshPro textMeshPro;

    void Update()
    {
        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            OnSpawnItemsRequested?.Invoke();
        }
    }
    void ShowText()
    {
        if (can_spawn_text)
        {
            textMeshPro.gameObject.SetActive(true);  
            OnSpawnItemsRequested?.Invoke();   
        }
    }

    void hideText()
    {
        textMeshPro.gameObject.SetActive(false);     
    }
    
    void Start()
    {
        hideText();
    }

public IEnumerator spawn_items()
{
    for (int i = 0; i < receptacles.Count; i++)
    {
        // 1. SAVE the home position as a Vector3 (Snapshot)
        Vector3 homePosition = receptacles[i].transform.position;

        // 2. Teleport the bubble to the start point
        receptacles[i].transform.position = startPoint.position;

        // 3. Make it visible
        receptacles[i].ShowVisuals();

        // 4. Tween from startPoint BACK to the saved homePosition
        StartCoroutine(TweenBubble(receptacles[i].transform, homePosition, 0.8f));

        yield return new WaitForSeconds(0.2f);
    }
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

