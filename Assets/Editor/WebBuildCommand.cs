using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace TalesOfVoyages.Editor
{
    public static class WebBuildCommand
    {
        private const string OutputEnvironmentVariable = "TALES_WEB_BUILD_PATH";

        public static void Build()
        {
            var outputPath = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new InvalidOperationException($"{OutputEnvironmentVariable} must contain the WebGL output path.");

            outputPath = Path.GetFullPath(outputPath);
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
