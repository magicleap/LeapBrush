using System.Collections;
using MagicLeap.DesignToolkit.Audio;
using Unity.VisualScripting;

namespace MagicLeap.LeapBrush
{
    public class GenericAudioHandler : AudioHandler
    {
        private bool _audioHandlerStarted;

        void Start()
        {
            base.Start();
            _audioHandlerStarted = true;
        }

        public void PlaySoundSafe(SoundDefinition soundDefinition)
        {
            if (!_audioHandlerStarted)
            {
                StartCoroutine(PlaySoundOnceStartedCoroutine(soundDefinition));
                return;
            }

            PlaySound(soundDefinition);
        }

        private IEnumerator PlaySoundOnceStartedCoroutine(SoundDefinition soundDefinition)
        {
            while (!_audioHandlerStarted)
            {
                yield return null;
            }

            PlaySound(soundDefinition);
        }
    }
}
