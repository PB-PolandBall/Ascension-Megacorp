using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class BuildBundle
{
    private static string OutputPath = "Assets/StreamingAssets";
    
    [MenuItem("Build/Build Windows")]
    public static void BuildWindows()
    {
        Build("usac_visuals", BuildTarget.StandaloneWindows64);
    }
    
    private static void Build(string bundleName, BuildTarget target)
    {
        if (!Directory.Exists(OutputPath))
            Directory.CreateDirectory(OutputPath);
        
        List<string> assets = new List<string>();
        
        if (Directory.Exists("Assets/Shaders"))
        {
            foreach (string file in Directory.GetFiles("Assets/Shaders", "*.shader"))
            {
                assets.Add(file.Replace("\\", "/"));
                Debug.Log("Adding shader: " + file);
            }
            foreach (string file in Directory.GetFiles("Assets/Shaders", "*.compute"))
            {
                assets.Add(file.Replace("\\", "/"));
                Debug.Log("Adding compute shader: " + file);
            }
        }
        
        if (assets.Count == 0)
        {
            Debug.LogError("No assets found!");
            return;
        }
        
        AssetBundleBuild[] builds = new AssetBundleBuild[1];
        builds[0].assetBundleName = bundleName;
        builds[0].assetNames = assets.ToArray();
        
        Debug.Log("Building " + bundleName + "...");
        
        BuildPipeline.BuildAssetBundles(
            OutputPath, 
            builds,
            BuildAssetBundleOptions.StrictMode,
            target
        );
    }
    
    public static void BuildFromCommandLine()
    {
        BuildWindows();
    }
}
