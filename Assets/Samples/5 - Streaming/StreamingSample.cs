using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Whisper;

public class StreamingSample : MonoBehaviour
{
    public WhisperManager whisper;
    public AudioClip clip;
    public Text text;
    private float[] _samples;
    private AudioSource _source;

    public async void OnSomeButtonPressed()
    {
        // get data
        _samples = new float[clip.samples * clip.channels];
        clip.GetData(_samples, 0);
        
        // start playing sound
        var go = new GameObject("Audio Echo");
        _source = go.AddComponent<AudioSource>();
        _source.clip = clip;
        _source.Play();
        
        whisper.StartStream(clip.frequency, clip.channels);
        whisper.OnResultUpdated += result =>
        {
            text.text = result + "...";
        };

        // main loop
        var lastPos = 0;
        while (_source.isPlaying)
        {
            var pos = _source.timeSamples;
            if (pos > lastPos)
            {
                var slice = new ArraySegment<float>(_samples, lastPos, pos - lastPos).ToArray();
                whisper.Stream(slice);
                lastPos = pos;
            }
            
            await Task.Yield();
        }

        whisper.FinishStream();
    }
}
