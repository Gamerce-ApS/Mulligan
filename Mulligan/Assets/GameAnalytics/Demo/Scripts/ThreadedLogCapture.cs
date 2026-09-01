using System.Collections.Generic;
using GameAnalyticsSDK.Events;
using UnityEngine;

/// <summary>
/// Captures logs emitted from background threads (e.g. the native C++ SDK's
/// log callback) and forwards them to GA_Debug.Messages on the main thread.
/// Application.logMessageReceived only fires for main-thread logs, so those
/// would otherwise never reach the on-screen log views.
/// </summary>
public class ThreadedLogCapture : MonoBehaviour
{
    private readonly Queue<string> queue = new Queue<string>();
    private int mainThreadId;

    /// <summary>Adds the component to the host if it isn't there already.</summary>
    public static void Ensure(GameObject host)
    {
        if (host.GetComponent<ThreadedLogCapture>() == null)
        {
            host.AddComponent<ThreadedLogCapture>();
        }
    }

    void OnEnable()
    {
        mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        Application.logMessageReceivedThreaded += HandleThreadedLog;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleThreadedLog;
    }

    private void HandleThreadedLog(string logString, string stackTrace, LogType type)
    {
        // Main-thread logs are already captured by GA_Debug.HandleLog — skip to avoid duplicates.
        if (System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId) return;

        lock (queue)
        {
            queue.Enqueue(logString);
        }
    }

    void Update()
    {
        lock (queue)
        {
            if (queue.Count == 0) return;

            if (GA_Debug.Messages == null)
            {
                GA_Debug.Messages = new List<string>();
            }
            while (queue.Count > 0)
            {
                GA_Debug.Messages.Add(queue.Dequeue());
            }
        }
    }
}
