using UnityEngine;
using UnityEngine.UI;
using Whisper;
using Whisper.Utils;

public class StreamingSampleMic : MonoBehaviour
{
    public WhisperManager whisper;
    public MicrophoneRecord microphoneRecord;
    
    [Header("UI")] 
    public Button button;
    public Text buttonText;
    public Text text;
    public ScrollRect scroll;
    
    private async void Start()
    {
        var stream = await whisper.CreateStream(microphoneRecord);
        stream.OnResultUpdated += OnResult;
        
        microphoneRecord.OnRecordStop += OnRecordStop;
        button.onClick.AddListener(OnButtonPressed);
    }

    private void OnButtonPressed()
    {
        if (!microphoneRecord.IsRecording)
            microphoneRecord.StartRecord();
        else
            microphoneRecord.StopRecord();
        
        buttonText.text = microphoneRecord.IsRecording ? "Stop" : "Record";
    }
    
    private void OnRecordStop(float[] data, int frequency, int channels, float length)
    {
        buttonText.text = "Record";
    }
    
    private void OnResult(string result)
    {
        text.text = result;
        UiUtils.ScrollDown(scroll);
    }
}