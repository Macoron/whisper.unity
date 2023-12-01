using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;


static class WhisperProjectSettingsProvider
{
    [SettingsProvider]
    public static SettingsProvider CreateMyCustomSettingsProvider()
    {
        var provider = new SettingsProvider("Project/WhisperSettings", SettingsScope.Project)
        {
            label = "Whisper",
            guiHandler = (searchContext) =>
            {
                CudaEnabled = EditorGUILayout.Toggle("Enable CUDA", CudaEnabled);
                MetalEnabled = EditorGUILayout.Toggle("Enable Metal", MetalEnabled);
            },

            keywords = new HashSet<string>(new[] { "CUDA", "cuBLAS", "Metal" })
        };

        return provider;
    }

    public static bool CudaEnabled
    {
        get
        {
#if WHISPER_CUDA
            return true;
#else
            return false;
#endif
        }
        set
        {
            if (value == CudaEnabled)
                return;
            SetDefine("WHISPER_CUDA", value);
        }
    }
    
    public static bool MetalEnabled
    {
        get
        {
#if WHISPER_METAL
            return true;
#else
            return false;
#endif
        }
        set
        {
            if (value == MetalEnabled)
                return;
            SetDefine("WHISPER_METAL", value);
        }
    }

    private static void SetDefine(string define, bool value)
    {
        string[] newDefines;
        var defines = GetStandaloneDefines();

        if (value)
        {
            if (defines.Contains(define))
                return;

            newDefines = defines.Append(define).ToArray();
        }
        else
        {
            if (!defines.Contains(define))
                return;

            newDefines = defines.Where(x => x != define).ToArray();
        }

        SetStandaloneDefines(newDefines);
    }
    

    // This is for older Unity compability
    private static string[] GetStandaloneDefines()
    {
        string[] defines;

#if UNITY_2021_3_OR_NEWER
        PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone, out defines);
#else
        var definesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
        defines = definesStr.Split(';');
#endif

        return defines;
    }

    private static void SetStandaloneDefines(string[] newDefines)
    {
#if UNITY_2021_3_OR_NEWER
        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, newDefines);
#else
        var definesStr = string.Join(";", newDefines);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, definesStr);
#endif
    }
}