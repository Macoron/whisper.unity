using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Whisper;
using Whisper.Utils;

public class StreamingSample : MonoBehaviour
{
    public WhisperManager whisper;
    public AudioClip clip;

    public Text text;
    private float[] _samples;
    private AudioSource _source;
    
    public ScrollRect scroll;

    private void OnStreamResultUpdated(string updatedResult)
    {
        text.text = updatedResult;
        UiUtils.ScrollDown(scroll);
    }

    public async void OnAudioClipButtonPressed()
    {
        // get data
        _samples = new float[clip.samples * clip.channels];
        clip.GetData(_samples, 0);
        
        // start playing sound
        var go = new GameObject("Audio Echo");
        _source = go.AddComponent<AudioSource>();
        _source.clip = clip;
        _source.Play();
        
        // init stream mode
        var stream = await whisper.CreateStream(clip.frequency, clip.channels);
        stream.OnResultUpdated += OnStreamResultUpdated;

        // main loop
        var lastPos = 0;
        while (_source.isPlaying)
        {
            var pos = _source.timeSamples;
            if (pos > lastPos)
            {
                var slice = new ArraySegment<float>(_samples, lastPos, pos - lastPos).ToArray();
                stream.AddToStream(slice);
                lastPos = pos;
            }
            
            await Task.Yield();
        }

        // flush rest
        stream.StopStream();
    }
}
