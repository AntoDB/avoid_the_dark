using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Singleton pour exécuter des actions sur le thread principal de Unity.
/// Nécessaire car les événements MQTT sont déclenchés sur un thread séparé.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly object Lock = new object();
    private static Thread mainThread;
    private readonly Queue<Action> executionQueue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            lock (Lock)
            {
                if (_instance == null)
                {
                    var go = new GameObject("UnityMainThreadDispatcher");
                    _instance = go.AddComponent<UnityMainThreadDispatcher>();
                    DontDestroyOnLoad(go);
                }
            }
        }
        return _instance;
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            mainThread = Thread.CurrentThread;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Ajoute une action à la file d'attente pour qu'elle soit exécutée sur le thread principal
    /// </summary>
    public void Enqueue(Action action)
    {
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// Exécute une action immédiatement si on est sur le thread principal, sinon l'ajoute à la file d'attente
    /// </summary>
    public void ExecuteOnMainThread(Action action)
    {
        if (Thread.CurrentThread == mainThread)
        {
            action();
        }
        else
        {
            Enqueue(action);
        }
    }

    void Update()
    {
        // Exécute toutes les actions en attente
        while (true)
        {
            Action action = null;
            lock (executionQueue)
            {
                if (executionQueue.Count > 0)
                {
                    action = executionQueue.Dequeue();
                }
                else
                {
                    break;
                }
            }
            action?.Invoke();
        }
    }
}