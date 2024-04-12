// Copyright (c) 2022 Magic Leap, Inc. All Rights Reserved.
// Please see the top-level LICENSE.md in this distribution
// for terms and conditions governing this file.
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System;
using System.Text;
using UnityEngine;

namespace MagicLeap.Keyboard
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
            _mainActionQueue = new();
        /// <summary>
        /// The worker thread
        /// </summary>
        private static System.Threading.Thread _thread = null;
        private static int _mainThreadId = -1;
        /// <summary>
        /// The concurrent queue of itemized work items that will execute on the worker thread.
        /// </summary>
        private static ConcurrentQueue<Func<bool>> _itemizedWork = new();

        /// <summary>
        /// A method that schedules a callback on the worker thread.
        /// </summary>
        /// <param name="function">Function to call. Return TRUE when processing is done,
        /// FALSE to be placed back in the queue to be called again at a later time.</param>
        public static void ScheduleWork(Func<bool> function)
        {
            _itemizedWork.Enqueue(function);
        }

        /// <summary>
        /// A method that schedules a callback on the main thread.
        /// </summary>
        /// <param name="callback">A callback function to be called when the action is invoked
        /// </param>
        public static void ScheduleMain(System.Action callback)
        {
            if (_mainThreadId != -1 &&
                _mainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId)
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
                _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            }
            System.Action action;
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
            while (true)
            {
                Func<bool> function;
                if (_itemizedWork.TryDequeue(out function))
                {
                    bool result = function();
                    if (!result)
                    {
                        // Not done yet. Put it at the end of the queue to try again later.
                        _itemizedWork.Enqueue(function);
                    }
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