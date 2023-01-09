using System;
using System.Collections;
using UnityEngine;

namespace MagicLeap
{
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