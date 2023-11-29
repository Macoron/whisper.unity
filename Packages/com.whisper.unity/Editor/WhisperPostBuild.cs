using System;
using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;


[UsedImplicitly]
public static class WhisperPostBuild
{
    
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget != BuildTarget.StandaloneOSX)
            return;
        if (!WhisperProjectSettingsProvider.MetalEnabled)
            return;
        
        // get source file
        var metalFile = Path.GetFullPath("Packages/com.whisper.unity/Plugins/MacOS/ggml-metal.metal");
        if (!File.Exists(metalFile))
        {
            throw new Exception("Can't find metal file in project files! " +
                                $"{metalFile} doesnt exist!");
        }

        // get target folder
        var pluginsPath = Path.Combine(path, "Contents", "PlugIns");
        if (!Directory.Exists(pluginsPath))
        {
            throw new Exception("Can't find plugins directory in build app! " +
                                $"{pluginsPath} doesnt exist!");
        }

        var targetPath = Path.Combine(pluginsPath, "ggml-metal.metal");
        File.Copy(metalFile, targetPath, true);
    }
}
