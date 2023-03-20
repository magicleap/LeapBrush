using System;
using System.Collections;
using UnityEngine;

namespace MagicLeap
{
    /// <summary>
    /// Helper to delay a UI button click event handler for a brief period to allow animations
    /// and sounds to play first.
    /// </summary>
    public class DelayedButtonHandler : MonoBehaviour
    {
        public const float DefaultButtonDelaySeconds = 0.25f;

        public void InvokeAfterDelayExclusive(Action actionToInvoke)
        {
            InvokeAfterDelayExclusive(actionToInvoke, DefaultButtonDelaySeconds);
        }

        public void InvokeAfterDelayExclusive(Action actionToInvoke, float delaySeconds)
        {
            StopAllCoroutines();

            StartCoroutine(InvokeAfterDelayCoroutine(actionToInvoke, delaySeconds));
        }

        private IEnumerator InvokeAfterDelayCoroutine(Action actionToInvoke, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);

            actionToInvoke();
        }
    }
}