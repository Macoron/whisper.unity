using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif


[UsedImplicitly]
public class DisableBitcodePostProcess
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget != BuildTarget.iOS)
            return;
        DisableBitcodeOnIos(path);
    }
    
    private static void DisableBitcodeOnIos(string path)
    {
#if UNITY_IOS
        var projectPath = Path.Combine(path, "Unity-iPhone.xcodeproj/project.pbxproj") ;
        var pbxProject = new PBXProject();
        pbxProject.ReadFromFile(projectPath);
        
        //Disabling Bitcode on all targets
        //Main
        var target = pbxProject.GetUnityMainTargetGuid();
        pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

        //Unity Tests
        target = pbxProject.TargetGuidByName(PBXProject.GetUnityTestTargetName());
        pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
        
        //Unity Framework
        target = pbxProject.GetUnityFrameworkTargetGuid();
        pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

        pbxProject.WriteToFile(projectPath);  
#endif
    }
}