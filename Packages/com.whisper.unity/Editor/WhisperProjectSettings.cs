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
            var defines = GetStandaloneDefines();

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

            SetStandaloneDefines(newDefines);
        }
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