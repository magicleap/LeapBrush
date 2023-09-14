using System.Collections;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    public class OneoffAudioSource : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _audioSource;

        void Start()
        {
            _audioSource.Play();

            StartCoroutine(DestroyAfterDelayCoroutine());
        }

        private IEnumerator DestroyAfterDelayCoroutine()
        {
            yield return new WaitForSeconds(_audioSource.clip.length + .1f);

            Destroy(gameObject);
        }
    }
}