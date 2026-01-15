using System;
using System.Collections.Concurrent;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    //--- Fields ---
    private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

    //--- Unity Methods ---
    void Update()
    {
        DoActionsInQueue();
    }

    //--- Public Methods ---
    /// <summary>
    /// 메인 스레드에서 수행할 작업을 등록합니다.
    /// </summary>
    /// <param name="action">수행할 작업</param>
    public static void Enqueue(Action action)
    {
        _executionQueue.Enqueue(action);
    }

    //--- Private Methods ---
    private static void DoActionsInQueue()
    {
        while (_executionQueue.TryDequeue(out var action))
        {
            action.Invoke();
        }
    }
}