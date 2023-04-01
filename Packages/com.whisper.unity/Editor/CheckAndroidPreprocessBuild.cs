using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class CheckAndroidPreprocessBuild : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    
    public void OnPreprocessBuild(BuildReport report)
    {
        var sum = report.summary;
        if (sum.platform != BuildTarget.Android)
            return;

        var backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
        if (backend != ScriptingImplementation.IL2CPP)
        {
            throw new Exception($"Unsupported scripting backend {backend}! Whisper for Android only supports il2cpp. " +
                                "You can change scripting backend in Player Settings.");
        }

        var arch = PlayerSettings.Android.targetArchitectures;
        if (arch != AndroidArchitecture.ARM64)
        {
            throw new Exception("Unsupported architecture! Whisper for Android only supports ARM64. " +
                                "You can change target architecture in Player Settings.");
        }
    }
}
