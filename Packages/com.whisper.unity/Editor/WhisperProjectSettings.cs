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
            },

            keywords = new HashSet<string>(new[] { "CUDA", "cuBLAS" })
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

            string[] newDefines;
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone, out var defines);

            if (value)
            {
                if (defines.Contains("WHISPER_CUDA"))
                    return;

                newDefines = defines.Append("WHISPER_CUDA").ToArray();
            }
            else
            {
                if (!defines.Contains("WHISPER_CUDA"))
                    return;

                newDefines = defines.Where(x => x != "WHISPER_CUDA").ToArray();
            }

            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, newDefines);
        }
    }
}