using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading;

namespace FileUploader
{
    public enum StatusState
    {
        Unstarted, InProgress, Completed, Cancelled, Failed
    }

    public abstract class ThreadWrapperBase
    {
        // This is the thread where the task is carried out.
        private Thread thread;

        // Track the status of the task.
        private StatusState status = StatusState.Unstarted;
        public StatusState Status
        {
            get { return status; }
        }

        // (You could add properties to return the thread ID and name here.)

        // Start the new operation.
        public void Start()
        {
            if (status == StatusState.InProgress)
            {
                throw new InvalidOperationException("Already in progress.");
            }
            else
            {
                // Initialize the new task.
                status = StatusState.InProgress;

                // Create the thread.
                thread = new Thread(StartTaskAsync);

                // Start the thread.
                thread.Start();
            }
        }

        private void StartTaskAsync()
        {
            DoTask();
            if (CancelRequested)
            {
                status = StatusState.Cancelled;
                OnCancelled();
            }
            else
            {
                status = StatusState.Completed;
                OnCompleted();
            }
        }

        // Override this class to supply the task logic.
        protected abstract void DoTask();

        // Override this class to supply the callback logic.
        protected abstract void OnCompleted();

        public event EventHandler Cancelled;
        protected void OnCancelled()
        {
            if (Cancelled != null)
                Cancelled(this, EventArgs.Empty);
        }

        public event EventHandler Failed = delegate { };
        protected void OnFailed()
        {
            if (Failed != null)
            {
                status = StatusState.Failed;
                Failed(this, EventArgs.Empty);
            }
        }

        // Flag that indicates a stop is requested.
        private bool cancelRequested = false;
        protected bool CancelRequested
        {
            get { return cancelRequested; }
        }

        public void RequestCancel()
        {
            cancelRequested = true;
        }
    }
}
