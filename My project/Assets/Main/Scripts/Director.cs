using System;
using UnityEngine;

public class Director : MonoBehaviour
{
    // 1. THE SINGLETON INSTANCE
    // This allows any script to say "Director.Instance" to access this exact object.
    public static Director Instance { get; private set; }

    // 2. The Signal (Notice it is NO LONGER static, because it belongs to the Instance)
    public event Action OnDoorToggle;

    void Awake()
    {
        // 3. THE SINGLETON RULE: "There can be only one."
        if (Instance != null && Instance != this)
        {
            // If another Director accidentally exists, destroy the imposter.
            Destroy(gameObject); 
        }
        else
        {
            // I am the one true Director.
            Instance = this; 
            
            // Optional: Keep me alive even if we load a new scene (like Godot Autoload)
            DontDestroyOnLoad(gameObject); 
        }
    }

    void Update()
    {
        // Press Space to trigger the door
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // The '?' checks if anyone is listening before firing
            OnDoorToggle?.Invoke(); 
            Debug.Log("Director Singleton: Toggle signal emitted.");
        }
    }
}