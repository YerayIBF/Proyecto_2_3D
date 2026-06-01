using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine.Rendering;

namespace BlurShadersPro
{
    public class BlurInstaller : MonoBehaviour
    {
        private static List<Pipeline> compatiblePipelines;
        private static List<Pipeline> installedPipelines;

        private static readonly string builtInPackageGUID = "5d0f20da5cc76ea4897e8a11dbb079a7";
        private static readonly string urpPackageGUID = "f23623e957ef3fb47adf8e111c538390";
        private static readonly string hdrpPackageGUID = "d6bfccdd916bc1647a5e126ca635bc43";

        private static readonly string builtInInstallGUID = "2df480a2217ecee41812d3a6e570fb0e";
        private static readonly string urpInstallGUID = "dcefd2da3aea0e04aa174a81794784b0";
        private static readonly string hdrpInstallGUID = "dbb496cc567761f458c40854cccd25e8";

        public class BlurImport : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
            {
                foreach (string str in importedAssets)
                {
                    // If we detect that this very file was reimported, trigger the installation window.
                    if (str.Contains("BlurInstaller.cs"))
                    {
                        BlurInstallerWindow.ShowWindow();
                    }
                }
            }
        }

        static BlurInstaller()
        {
            AssetDatabase.importPackageCompleted += Initialize;
        }

        private static void Initialize(string packagename)
        {
            Initialize();
        }

        public static void Initialize()
        {
            compatiblePipelines = FindCompatiblePipelines();
            installedPipelines = FindInstalledPipelines();
        }

        public enum Pipeline
        {
            BuiltinAlone,
            BuiltinPostProcess,
            URP,
            HDRP
        }

        // Get a list of every package installed via the Package Manager.
        private static List<UnityEditor.PackageManager.PackageInfo> GetInstalledPackages()
        {
            ListRequest listRequest = Client.List(true, true);

            while (listRequest.Status == StatusCode.InProgress) { }

            if (listRequest.Status == StatusCode.Failure)
            {
                Debug.LogError("(Blur Shaders Pro): Could not retrieve package list.");
            }

            PackageCollection packageCollection = listRequest.Result;
            return new List<UnityEditor.PackageManager.PackageInfo>(packageCollection);
        }

        // Check which RP packages are currently installed.
        private static List<Pipeline> FindCompatiblePipelines()
        {
            List<Pipeline> compatiblePipelines = new List<Pipeline>();

            var packageCollection = GetInstalledPackages();

            for (int i = 0; i < packageCollection.Count; ++i)
            {
                var packageInfo = packageCollection[i];

                if (packageInfo.name == "com.unity.render-pipelines.universal")
                {
                    compatiblePipelines.Add(Pipeline.URP);
                }

                if (packageInfo.name == "com.unity.render-pipelines.high-definition")
                {
                    compatiblePipelines.Add(Pipeline.HDRP);
                }

                if (packageInfo.name == "com.unity.postprocessing")
                {
                    compatiblePipelines.Add(Pipeline.BuiltinPostProcess);
                }
            }

            if (!compatiblePipelines.Contains(Pipeline.BuiltinPostProcess))
            {
                compatiblePipelines.Add(Pipeline.BuiltinAlone);
            }

            return compatiblePipelines;
        }

        private static List<Pipeline> FindInstalledPipelines()
        {
            List<Pipeline> installedPipelines = new List<Pipeline>();

            if (IsInstalled(urpInstallGUID))
            {
                installedPipelines.Add(Pipeline.URP);
            }

            if (IsInstalled(hdrpInstallGUID))
            {
                installedPipelines.Add(Pipeline.HDRP);
            }

            if (IsInstalled(builtInInstallGUID))
            {
                installedPipelines.Add(Pipeline.BuiltinPostProcess);
            }

            return installedPipelines;
        }

        // We are using built-in but PostProcessing not installed. Try to install it.
        public static bool InstallBuiltinPostProcess()
        {
            AddRequest addRequest = Client.Add("com.unity.postprocessing");

            while (addRequest.Status == StatusCode.InProgress) { }

            if (addRequest.Status == StatusCode.Failure)
            {
                Debug.LogError("(Blur Shaders Pro): Unable to install PostProcessing package for Built-in Pipeline.");
                return false;
            }

            return true;
        }

        public static void InstallBlurBuiltin()
        {
            InstallBlur(builtInPackageGUID, "Built-in");
        }

        public static void InstallBlurURP()
        {
            InstallBlur(urpPackageGUID, "URP");
        }

        public static void InstallBlurHDRP()
        {
            InstallBlur(hdrpPackageGUID, "HDRP");
        }

        private static void InstallBlur(string GUID, string pipelineName)
        {
            string path = AssetDatabase.GUIDToAssetPath(GUID);

            if (path.Length > 0)
            {
                AssetDatabase.ImportPackage(path, true);
            }
            else
            {
                Debug.LogError($"(Blur Shaders Pro): Could not find package file for {pipelineName}. Consider manually installing the package.");
            }
        }

        private static bool IsInstalled(string GUID)
        {
            string path = string.Empty;
            path = AssetDatabase.GUIDToAssetPath(GUID);

            if (path.Length > 0)
            {
                var file = AssetDatabase.LoadAssetAtPath(path, typeof(object));

                return (file != null);
            }

            return false;
        }

        public static List<Pipeline> GetCompatiblePipelines()
        {
            return compatiblePipelines;
        }

        public static List<Pipeline> GetInstalledPipelines()
        {
            return installedPipelines;
        }
    }
}

