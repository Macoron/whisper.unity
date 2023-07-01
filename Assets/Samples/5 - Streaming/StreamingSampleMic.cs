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
    
    private async void Start()
    {
        var stream = await whisper.CreateStream(microphoneRecord);
        stream.OnResultUpdated += OnResult;
        
        button.onClick.AddListener(OnButtonPressed);
    }

    private void OnButtonPressed()
    {
        if (!microphoneRecord.IsRecording)
            microphoneRecord.StartRecord();
        else
            microphoneRecord.StopRecord();
        
        if (buttonText)
            buttonText.text = microphoneRecord.IsRecording ? "Stop" : "Record";
    }
    
    private void OnResult(string result)
    {
        text.text = result;
    }
}