using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Whisper;
using Whisper.Utils;

public class WhisperTests
{
    private readonly string _modelPath = Path.Combine(Application.streamingAssetsPath, "Whisper/ggml-tiny.bin");
    
    [Test]
    public void InitFromFileTest()
    {
        var whisper = WhisperWrapper.InitFromFile(_modelPath);
        Assert.NotNull(whisper);
    }
    
    [Test]
    public async Task InitFromFileAsyncTest()
    {
        var whisper = await WhisperWrapper.InitFromFileAsync(_modelPath);
        Assert.NotNull(whisper);
    }
    
    [Test]
    public void InitFromBufferTest()
    {
        var file = FileUtils.ReadFile(_modelPath);
        Assert.IsNotEmpty(file);
        var whisper = WhisperWrapper.InitFromBuffer(file);
        Assert.NotNull(whisper);
    }
    
    [Test]
    public async Task InitFromBufferAsyncTest()
    {
        var file = await FileUtils.ReadFileAsync(_modelPath);
        Assert.IsNotEmpty(file);
        var whisper = await WhisperWrapper.InitFromBufferAsync(file);
        Assert.NotNull(whisper);
    }
}
