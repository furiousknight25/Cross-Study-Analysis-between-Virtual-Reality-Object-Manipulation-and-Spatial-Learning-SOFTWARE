using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrialScene : MonoBehaviour
{
    public List<string> touch_points = new List<string>();
    public Tablet tablet;
    void Start()
    {
        RegisterWithDirector();
    }

    void OnEnable()
    {
        RegisterWithDirector();
    }

    void OnDisable()
    {
        if (Director.Instance != null)
        {
            Director.Instance.UnregisterTrialScene(this);
        }
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
        yield return StartCoroutine(StartTimer(Director.Instance.explore_time));
        //wait till player walks to tablet and click button
        yield return StartCoroutine(StartTimer(Director.Instance.reading_time));
        //spawn items
        yield return StartCoroutine(StartTimer(Director.Instance.encode_time));

        // Store trial data
        Director.Instance.trialDataList[this] = string.Join(", ", touch_points);
        Debug.Log("Trial data stored.");

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
        touch_points.Add(point.ToString());
    }
}
