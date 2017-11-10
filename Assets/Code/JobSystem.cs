using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class JobSystem : MonoBehaviour
{
    // Singleton
    static JobSystem _instance;
    public static JobSystem instance
    {
        get
        {
            if (!_instance)
                Initialize();

            return _instance;
        }
    }
    public static void Initialize()
    {
        GameObject newGO = new GameObject("Job System", typeof(JobSystem));
        _instance = newGO.GetComponent<JobSystem>();
    }

    // Job class
    class Job
    {
        public Func<object> methodToThread;         // The method that needs to be threaded. It returns an object.
        public Action<object> mainThreadCallback;   // The callback that should run when the work is complete. It takes an object as a parameter.

        public Thread jobThread;                    // The thread which the threaded method is run on

        public object result;                       // The result of the thread. This will be set whenever the job is done.

        public void Begin()
        {
            result = methodToThread();
        }
    }

    List<Job> currentJobs = new List<Job>();


    // Update
    private void Update()
    {
        for (int i = currentJobs.Count - 1; i >= 0; i--)
            if (!currentJobs[i].jobThread.IsAlive)
            {
                currentJobs[i].mainThreadCallback(currentJobs[i].result);
                currentJobs.RemoveAt(i);
            }
    }

    public void DoThreaded(Func<object> inMethodToThread, Action<object> inMainThreadCallback)
    {
        Job newJob = new Job();

        lock (currentJobs)
            currentJobs.Add(newJob);

        newJob.methodToThread = inMethodToThread;
        newJob.mainThreadCallback    = inMainThreadCallback;
        newJob.jobThread      = new Thread(() => newJob.Begin());

        newJob.jobThread.Start();
    }
}