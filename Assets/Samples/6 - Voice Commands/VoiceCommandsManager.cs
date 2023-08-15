using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Whisper;
using Whisper.Utils;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System;
using Whisper.Native;
using System.Linq;
using UnityEngine.UI;

public class VoiceCommandsManager : MonoBehaviour
{
    private class command
    {
        public string text;
        public Delegate thenDo;
        public float score;
    }

    private List<command> commands;

    [Range(0f, 1f)]
    [Tooltip("How similar must the user's transcription be to trigger a match?")]
    public float SimilarityThreshold;

    [SerializeField]
    [Tooltip("Path to model weights file")]
    private string modelPath = "Whisper/ggml-tiny.bin";

    [SerializeField]
    [Tooltip("Determines whether the StreamingAssets folder should be prepended to the model path")]
    private bool isModelPathInStreamingAssets = true;

    [SerializeField]
    [Tooltip("Should model weights be loaded on awake?")]
    private bool initOnAwake = true;

    [SerializeField]
    [Header("Language")]
    [Tooltip("Output text language. Use empty or \"auto\" for auto-detection.")]
    public string language = "en";

    [Tooltip("Force output text to English translation. Improves translation quality.")]
    public bool translateToEnglish;


    [Header("Advanced settings")]
    [SerializeField]
    private WhisperSamplingStrategy strategy = WhisperSamplingStrategy.WHISPER_SAMPLING_GREEDY;

    [Tooltip("Do not use past transcription (if any) as initial prompt for the decoder.")]
    public bool noContext = true;

    [Tooltip("Force single segment output (useful for streaming).")]
    public bool singleSegment;


    [Tooltip("Output tokens with their confidence in each segment.")]
    public bool enableTokens;

    [Tooltip("Initial prompt as a string variable. " +
             "It should improve transcription quality or guide it to the right direction.")]
    [TextArea]
    public string initialPrompt;

    [Header("Experimental settings")]
    [Tooltip("[EXPERIMENTAL] Output timestamps for each token. Need enabled tokens to work.")]
    public bool tokensTimestamps;

    [Tooltip("[EXPERIMENTAL] Speed-up the audio by 2x using Phase Vocoder. " +
                 "These can significantly reduce the quality of the output.")]
    public bool speedUp = false;

    [Tooltip("[EXPERIMENTAL] Overwrite the audio context size (0 = use default). " +
                 "These can significantly reduce the quality of the output.")]
    public int audioCtx;

    public Text transcriptionText;
    public Text confidenceText;

    public LoopingMicrophone microphone;
    private WhisperWrapper _whisper;
    private WhisperParams _params;

    private Task<WhisperResult> _task;

    public void RegisterCommand(string command, Delegate thenDo)
    {
        initializeCommands();
        command c = new command();
        c.text = CleanText(command);
        c.thenDo = thenDo;
        c.score = 0f;
        commands.Add(c);
    }

    private void initializeCommands()
    {
        if (commands == null)
        {
            commands = new List<command>();
        }
    }

    public void UnregisterAll()
    {
        commands.Clear();//no more commands.
        microphone.StartRecord();//this will reinstantiate an audio clip.
                                 //effectively clearing out the transcription.
    }

    public bool IsModelPathInStreamingAssets
    {
        get => isModelPathInStreamingAssets;
        set
        {
            if (IsLoaded || IsLoading)
            {
                throw new InvalidOperationException("Cannot change model path after loading the model");
            }

            isModelPathInStreamingAssets = value;
        }
    }

    /// <summary>
    /// Checks if whisper weights are loaded and ready to be used.
    /// </summary>
    public bool IsLoaded => _whisper != null;

    /// <summary>
    /// Checks if whisper weights are still loading and not ready.
    /// </summary>
    public bool IsLoading { get; private set; }

    private async void Awake()
    {
        if (!initOnAwake)
            return;
        await InitModel();
        if (microphone != null)
        {
            microphone.OnEvaluate += Evaluate;
            microphone.OnRecordStop += MicrophoneOnRecordStop;
        }

        initializeCommands();

        microphone.StartRecord();
    }

    private void Start()
    {
        
    }

    /// <summary>
    /// Load model and default parameters. Prepare it for text transcription.
    /// </summary>
    public async Task InitModel()
    {
        // check if model is already loaded or actively loading
        if (IsLoaded)
        {
            Debug.LogWarning("Whisper model is already loaded and ready for use!");
            return;
        }

        if (IsLoading)
        {
            Debug.LogWarning("Whisper model is already loading!");
            return;
        }

        // load model and default params
        IsLoading = true;
        try
        {
            var path = IsModelPathInStreamingAssets
                ? Application.streamingAssetsPath + "/" + modelPath
                : modelPath;
            _whisper = await WhisperWrapper.InitFromFileAsync(path);
            _params = WhisperParams.GetDefaultParams(strategy);
            UpdateParams();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        IsLoading = false;
    }
    private void UpdateParams()
    {
        _params.Language = language;
        _params.Translate = translateToEnglish;
        _params.NoContext = noContext;
        _params.SingleSegment = singleSegment;
        _params.SpeedUp = speedUp;
        _params.AudioCtx = audioCtx;
        _params.EnableTokens = enableTokens;
        _params.TokenTimestamps = tokensTimestamps;
        _params.InitialPrompt = initialPrompt;
    }


    private async void Evaluate(AudioClip clip)
    {
        if (_task != null && !_task.IsCompleted)
            return;

        if (commands.Count == 0)
            return;

        _task = _whisper.GetTextAsync(microphone.GetData(), clip.frequency,
                clip.channels, _params);

        // append current transcription into temporary output
        var res = await _task;


        string confidenceReadout = "";
        string cleanTranscription = CleanText(res.Result);
        
        for(int i = 0; i < commands.Count; i++)
        {
            string transcription = cleanTranscription;
            int LCS = LongestCommonSubsequence(commands[i].text, transcription);
            float prevScore = 0f;
            float Score = (2f * LCS) / (commands[i].text.Length + transcription.Length);
            while ((transcription.Length > commands[i].text.Length) && Score > prevScore)
            {
                string[] pieces = transcription.Split(' ');
                if (pieces.Length > 1)
                {
                    // Join the pieces excluding the first piece (index 0)
                    transcription = string.Join(" ", pieces, 1, pieces.Length - 1);
                    LCS = LongestCommonSubsequence(commands[i].text, transcription);
                    prevScore = Score;
                    Score = (2f * LCS) / (commands[i].text.Length + transcription.Length);
                }
                else
                {
                    break;
                }
            }
            commands[i].score = Score;
            confidenceReadout += Score.ToString("F2") + " : " + commands[i].text +"\n";
        }
        Debug.Log(res.Result);
        DisplayResult(cleanTranscription, confidenceReadout);

        command maxInstance = commands.OrderByDescending(x => x.score).FirstOrDefault();
        if(maxInstance.score > SimilarityThreshold)
        {
            maxInstance.thenDo.DynamicInvoke(maxInstance.text);
        }
    }

    private void MicrophoneOnRecordStop()
    {
        //Maybe start it again...
    }

    private void DisplayResult(string cleanTranscription, string confidenceReadout)
    {
        //Debug.Log(result);
        transcriptionText.text = cleanTranscription;
        confidenceText.text = confidenceReadout;
    }

    static string CleanText(string input)
    {
        input = RemoveEncapsulatedText('[', ']', input);
        input = RemoveEncapsulatedText('(', ')', input);
        input = RemoveEncapsulatedText('*', '*', input);
        input = RemoveMultipleSpaces(input);
        input = input.ToUpper();
        input = KeepOnlyAllowedCharacters(input);
        return input;
    }

    static string KeepOnlyAllowedCharacters(string input)
    {
        string pattern = @"[^A-Z ]";
        Regex regex = new Regex(pattern);

        string result = regex.Replace(input, "");

        return result;
    }

    static string RemoveEncapsulatedText(char start, char end, string input)
    {
        // Define the regular expression pattern to match text within square brackets
        string pattern = @"\" + start + @"[^\" + end + @"]*\" + end;

        // Create a regular expression object
        Regex regex = new Regex(pattern);

        // Use the regular expression to replace matches with an empty string
        string result = regex.Replace(input, "");

        return result;
    }

    static string RemoveMultipleSpaces(string input)
    {
        // Define the regular expression pattern to match multiple spaces
        string pattern = @"\s+";

        // Create a regular expression object
        Regex regex = new Regex(pattern);

        // Use the regular expression to replace matches with a single space
        string result = regex.Replace(input, " ");

        return result;
    }

    static int LongestCommonSubsequence(string str1, string str2)
    {
        int m = str1.Length;
        int n = str2.Length;

        // Create a 2D array to store LCS lengths for subproblems
        int[,] dp = new int[m + 1, n + 1];

        // Build the LCS array in a bottom-up manner
        for (int i = 0; i <= m; i++)
        {
            for (int j = 0; j <= n; j++)
            {
                if (i == 0 || j == 0)
                {
                    dp[i, j] = 0; // Base case: LCS of an empty string and any other string is 0
                }
                else if (str1[i - 1] == str2[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1; // Characters match, extend LCS by 1
                }
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]); // Characters don't match, get the max LCS from the previous cells
                }
            }
        }

        return dp[m, n]; // The bottom-right cell contains the length of the LCS
    }
}