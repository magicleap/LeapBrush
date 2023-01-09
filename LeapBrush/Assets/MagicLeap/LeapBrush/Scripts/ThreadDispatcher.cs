using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace MagicLeap
{
    /// <summary>
    /// Handles dispatching calls from the Magic Leap native thread to the Unity thread
    /// </summary>
    public class ThreadDispatcher
    {
        /// <summary>
        /// The concurrent queue for actions to execute on the main thread.
        /// </summary>
        private static System.Collections.Concurrent.ConcurrentQueue<System.Action>
            _mainActionQueue = new System.Collections.Concurrent.ConcurrentQueue<System.Action>();

        /// <summary>
        /// The worker thread
        /// </summary>
        private static System.Threading.Thread _thread = null;

        private static int _mainThreadId = -1;

        /// <summary>
        /// The concurrent queue of itemized work items that will execute on the worker thread.
        /// </summary>
        private static ConcurrentQueue<Action> _itemizedWork = new ConcurrentQueue<Action>();

        /// <summary>
        /// A method that schedules a callback on the worker thread.
        /// </summary>
        /// <param name="callback">A callback function to be called when the action is invoked
        /// </param>
        public static void ScheduleWork(Action callback)
        {
            _itemizedWork.Enqueue(callback);
        }

        /// <summary>
        /// A method that schedules a callback on the main thread.
        /// </summary>
        /// <param name="callback">A callback function to be called when the action is invoked
        /// </param>
        public static void ScheduleMain(Action callback)
        {
            if (_mainThreadId != -1 &&
                _mainThreadId == Thread.CurrentThread.ManagedThreadId)
            {
                callback();
            }
            else
            {
                _mainActionQueue.Enqueue(callback);
            }
        }

        /// <summary>
        /// Dispatch all queued items
        /// </summary>
        public static void DispatchAll()
        {
            if (_mainThreadId == -1)
            {
                _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            }

            Action action;
            while (_mainActionQueue.TryDequeue(out action))
            {
                action();
            }

            if (_thread == null && !_itemizedWork.IsEmpty)
            {
                _thread = new Thread(ExecuteBackgroundThread);
                _thread.Start();
            }
        }

        /// <summary>
        /// Static method that executes the background worker thread.
        /// </summary>
        /// <param name="obj">Optional object</param>
        private static void ExecuteBackgroundThread(object obj)
        {
            Thread.CurrentThread.IsBackground = true;
            AndroidJNI.AttachCurrentThread();

            while (true)
            {
                Action action;

                if (_itemizedWork.TryDequeue(out action))
                {
                    action();
                }
                else
                {
                    // Yield a reasonable timeslice.
                    Thread.Sleep(5);
                }
            }
        }
    }
}