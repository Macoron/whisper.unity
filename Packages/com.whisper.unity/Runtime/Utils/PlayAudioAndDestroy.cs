using System;
using UnityEngine;

namespace Whisper.Utils
{
    /// <summary>
    /// Plays audio once and destroy itself and audio clip.
    /// </summary>
    public class PlayAudioAndDestroy : MonoBehaviour
    {
        private AudioSource _source;

        private void Update()
        {
            if (!_source)
            {
                Destroy(gameObject);
                return;
            }

            if (!_source.isPlaying)
            {
                if (_source.clip)
                    Destroy(_source.clip);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Play audio clip once and destroy it.
        /// </summary>
        public static void Play(AudioClip clip, Vector3 position, float volume = 1f)
        {
            var go = new GameObject("One shot audio");
            go.transform.position = position;
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1.0f;
            source.volume = volume;
            source.Play();

            var comp = go.AddComponent<PlayAudioAndDestroy>();
            comp._source = source;
        }
    }
}