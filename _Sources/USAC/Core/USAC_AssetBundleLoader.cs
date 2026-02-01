using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace USAC
{
    [StaticConstructorOnStartup]
    public static class USAC_AssetBundleLoader
    {
        public static Shader SewageSprayShader;
        public static ComputeShader SewageSprayCompute;
        public static Shader SewageSprayInstancedShader;
        public static Shader OverlayShader;

        public static bool IsLoaded;

        static USAC_AssetBundleLoader()
        {
            LongEventHandler.ExecuteWhenFinished(LoadAssetBundle);
        }

        private static void LoadAssetBundle()
        {
            try
            {
                AssetBundle bundle = FindBundle("usac_visuals");
                if (bundle == null)
                {
                    Log.Error("[USAC] 无法加载 AssetBundle 'usac_visuals'. 污水特效将被禁用.");
                    return;
                }

                SewageSprayShader = bundle.LoadAsset<Shader>("Assets/Shaders/USAC_SewageSpray.shader");
                SewageSprayCompute = bundle.LoadAsset<ComputeShader>("Assets/Shaders/USAC_SewageSpray_Compute.compute");
                SewageSprayInstancedShader = bundle.LoadAsset<Shader>("Assets/Shaders/USAC_SewageSpray_Instanced.shader");
                OverlayShader = bundle.LoadAsset<Shader>("Assets/Shaders/USAC_Overlay.shader");

                // 执行指定名称备选资产加载
                if (SewageSprayShader == null) SewageSprayShader = bundle.LoadAsset<Shader>("USAC_SewageSpray");
                if (SewageSprayCompute == null) SewageSprayCompute = bundle.LoadAsset<ComputeShader>("USAC_SewageSpray_Compute");
                if (SewageSprayInstancedShader == null) SewageSprayInstancedShader = bundle.LoadAsset<Shader>("USAC_SewageSpray_Instanced");
                if (OverlayShader == null) OverlayShader = bundle.LoadAsset<Shader>("USAC_Overlay");

                if (SewageSprayShader != null && SewageSprayCompute != null && SewageSprayInstancedShader != null)
                {
                    USAC_Debug.Log("[USAC] 成功加载 GPU 粒子系统 shaders.");
                    IsLoaded = true;
                }

                if (OverlayShader != null)
                {
                    USAC_Debug.Log("[USAC] 成功加载覆盖层着色器.");
                }
                else
                {
                    Log.Error("[USAC] Failed to load shaders from bundle. Available assets:");
                    if (bundle != null)
                    {
                        foreach (var name in bundle.GetAllAssetNames())
                        {
                            Log.Error(" - " + name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[USAC] Exception loading AssetBundle: {ex}");
            }
        }

        private static AssetBundle FindBundle(string bundleName)
        {
            foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
            {
                if (mod.PackageId.ToLower().Contains("usac") || mod.Name.Contains("USAC"))
                {
                    foreach (AssetBundle bundle in mod.assetBundles.loadedAssetBundles)
                    {
                        if (bundle.name == bundleName) return bundle;
                    }
                }
            }
            return null;
        }
    }
}
