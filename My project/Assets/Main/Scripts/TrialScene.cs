using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrialScene : MonoBehaviour
{
    public List<Vector3> touch_points = new List<Vector3>();
    public Tablet tablet;
    
    // FIX: Added 'f' suffixes to declare these as floats instead of doubles
    public Vector3 trialStartPosition = new Vector3(-1.4f, -0.06f, 1.48f); 
    public Vector3 trialEndPosition = new Vector3(-1.4f, 10.00f, 1.48f);

    // Removed Start() to prevent double-registration, OnEnable is sufficient
    void OnEnable()
    {
        RegisterWithDirector();
        print(transform.position);

    }


    void RegisterWithDirector()
    {
        if (Director.Instance != null)
        {
            Director.Instance.RegisterTrialScene(this);
        }
        else
        {
            Debug.LogWarning("Director instance not ready yet. Make sure Director exists in the scene.");
        }
    }

    public IEnumerator StartTrial()
    {
        transform.localScale *= 1.1f;
        StartCoroutine(TweenPosition(trialStartPosition, 2f)); 
        yield return StartCoroutine(StartTimer(Director.Instance.explore_time));
        //wait till player walks to tablet and click button
        yield return StartCoroutine(StartTimer(Director.Instance.reading_time));
        //spawn items
        yield return StartCoroutine(StartTimer(Director.Instance.encode_time));

        // Store trial data
        Director.Instance.trialDataList[this] = string.Join(", ", touch_points);
        Debug.Log("Trial data stored.");
    }

    public Dictionary<string, List<string>> end_trial() 
    {
        Dictionary<string, List<string>> trialData = new Dictionary<string, List<string>>();
        trialData["touch_points"] = touch_points.ConvertAll(point => point.ToString());
        
        StartCoroutine(TweenPosition(trialEndPosition, 2f)); // Move the player to the end position over 2 seconds
        return trialData;
    }

    IEnumerator StartTimer(float seconds)
    {
        Debug.Log("Timer started");
        yield return new WaitForSeconds(seconds);
        Debug.Log("Timer finished");
    }

    public void pointTouched(Vector3 point)
    {
        Debug.Log("Point touched: " + point);
        // FIX: Removed .ToString() so it matches the List<Vector3> type
        touch_points.Add(point);
    }

    public IEnumerator TweenPosition(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
    }
}