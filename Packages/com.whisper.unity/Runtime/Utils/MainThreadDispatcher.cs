using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Whisper.Utils
{
    /// <summary>
    /// Helper class to dispatch events from main thread.
    /// Useful to pass events from other threads to Unity main thread.
    /// </summary>
    public class MainThreadDispatcher
    {
        private readonly ConcurrentQueue<Task> _actions = new ConcurrentQueue<Task>();

        /// <summary>
        /// Add action to be executed on main Unity thread.
        /// </summary>
        public void Execute(Action action)
        {
            _actions.Enqueue(new Task(action));
        }
        
        /// <summary>
        /// Call this in Unity update to flush all pending actions.
        /// </summary>
        public void Update()
        {
            while (_actions.TryDequeue(out var task))
            {
                task.RunSynchronously();
            }
        }
    }
}