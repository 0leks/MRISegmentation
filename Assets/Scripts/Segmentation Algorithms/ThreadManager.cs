using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreadManager {

    private List<ThreadedJob> runningThreads;     // list of running threads

    public ThreadManager()
    {
        runningThreads = new List<ThreadedJob>();
    }

    public void AddThread(ThreadedJob job)
    {
        job.StartThread();
        runningThreads.Add(job);
    }

    void Update()
    {
        // Update each thread to see if it is done yet.
        for (int index = runningThreads.Count - 1; index >= 0; index--)
        {
            if (runningThreads[index].Update())
            {
                runningThreads.Remove(runningThreads[index]);
            }
        }
    }
}
