using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ensures segmentation methods run parallel

public class ThreadedJob {
    private bool m_IsDone = false;
    private bool m_calledOnFinished = false;
    private object m_Handle = new object();
    private System.Threading.Thread m_Thread = null;
    public bool IsDone {
        get {
            bool tmp;
            lock( m_Handle ) {
                tmp = m_IsDone;
            }
            return tmp;
        }
        set {
            lock( m_Handle ) {
                m_IsDone = value;
            }
        }
    }

    public virtual void StartThread() {
        m_Thread = new System.Threading.Thread( Run );
        m_Thread.Start();
    }
    public virtual void Abort() {
        m_Thread.Abort();
    }

    protected virtual void ThreadFunction() { }

    protected virtual void OnFinished() { }

    public virtual bool Update() {
        if( IsDone ) {
            if( !m_calledOnFinished ) {
                OnFinished();
                m_calledOnFinished = true;
            }
            return true;
        }
        return false;
    }
    public IEnumerator WaitFor() {
        while( !Update() ) {
            yield return null;
        }
    }
    private void Run() {
        m_calledOnFinished = false;
        ThreadFunction();
        IsDone = true;
    }
}
