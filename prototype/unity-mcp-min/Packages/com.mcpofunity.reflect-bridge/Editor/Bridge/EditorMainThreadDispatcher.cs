using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;

namespace UnityMcpMin.Editor.Bridge
{
    /// <summary>
    /// Runs work on the Unity editor main thread (required for GameObject / Component APIs).
    /// </summary>
    internal static class EditorMainThreadDispatcher
    {
        private static readonly Queue<Action> Queue = new Queue<Action>();
        private static volatile bool Registered;

        private static void EnsureRegistered()
        {
            if (Registered)
            {
                return;
            }

            Registered = true;
            EditorApplication.update += Pump;
        }

        private static void Pump()
        {
            while (true)
            {
                Action action = null;
                lock (Queue)
                {
                    if (Queue.Count > 0)
                    {
                        action = Queue.Dequeue();
                    }
                }

                if (action == null)
                {
                    return;
                }

                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }

        public static T Invoke<T>(Func<T> func, int timeoutMs = 60000)
        {
            EnsureRegistered();
            var gate = new ManualResetEventSlim(false);
            T result = default;
            Exception caught = null;

            lock (Queue)
            {
                Queue.Enqueue(() =>
                {
                    try
                    {
                        result = func();
                    }
                    catch (Exception ex)
                    {
                        caught = ex;
                    }
                    finally
                    {
                        gate.Set();
                    }
                });
            }

            if (!gate.Wait(timeoutMs))
            {
                throw new TimeoutException("Unity main thread dispatch timed out.");
            }

            if (caught != null)
            {
                throw caught;
            }

            return result;
        }
    }
}
