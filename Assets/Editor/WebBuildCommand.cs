using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TalesOfTheBrave.Editor
{
    [InitializeOnLoad]
    internal static class WebBuildBatchBootstrap
    {
        static WebBuildBatchBootstrap()
        {
            if (!Application.isBatchMode ||
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TALES_WEB_BUILD_PATH")))
                return;
            EditorApplication.delayCall += BuildAndExit;
        }

        private static void BuildAndExit()
        {
            try
            {
                WebBuildCommand.Build();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }

    public static class WebBuildCommand
    {
        private const string OutputEnvironmentVariable = "TALES_WEB_BUILD_PATH";

        public static void Build()
        {
            var outputPath = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new InvalidOperationException($"{OutputEnvironmentVariable} must contain the WebGL output path.");

            BuildTo(Path.GetFullPath(outputPath), false);
        }

        [MenuItem("Tales of the Brave/Build WebGL to WebsitePublish")]
        public static void BuildFromEditor()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Unable to determine the Unity project root.");
            var outputPath = Path.Combine(
                projectRoot, "WebsitePublish", "games", "tales-of-the-brave");

            if (!EditorUtility.DisplayDialog(
                    "Build WebGL",
                    $"Replace the existing WebGL build at:\n{outputPath}?",
                    "Build",
                    "Cancel"))
                return;

            BuildTo(outputPath, true);
            EditorUtility.RevealInFinder(outputPath);
        }

        private static void BuildTo(string outputPath, bool clearExistingOutput)
        {
            outputPath = Path.GetFullPath(outputPath);
            if (clearExistingOutput && Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);

            Directory.CreateDirectory(outputPath);

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes were found in Editor Build Settings.");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    $"WebGL build failed with {report.summary.totalErrors} errors and {report.summary.totalWarnings} warnings.");
        }
    }
}
