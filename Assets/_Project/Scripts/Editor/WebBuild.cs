using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds BullseyeQ for the web (Brotli, hashed file names, BullseyeQ template) into <see cref="OutputDir"/>.
/// Batch entry: <c>-executeMethod WebBuild.Build</c>, exits with 1 on failure.
/// </summary>
public static class WebBuild
{
    public const string OutputDir = "Builds/Web";

    const string SizePlaceholder = "__BQ_TOTAL_BYTES__";

    [MenuItem("BullseyeQ/Build Web")]
    public static void BuildFromMenu() => Run();

    public static void Build()
    {
        if (!Run()) EditorApplication.Exit(1);
    }

    static bool Run()
    {
        PlayerSettings.WebGL.compressionFormat     = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.nameFilesAsHashes     = true;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.template              = "PROJECT:BullseyeQ";

        var options = new BuildPlayerOptions
        {
            scenes           = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = OutputDir,
            target           = BuildTarget.WebGL,
            options          = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Web build failed: {report.summary.result}, {report.summary.totalErrors} errors");
            return false;
        }
        StampTotalSize(OutputDir);
        Debug.Log($"Web build done: {report.summary.totalSize / 1048576} MB in {OutputDir}");
        return true;
    }

    /// <summary>Writes the summed size of all files in <c>dir/Build</c> into the loader's placeholder.</summary>
    public static void StampTotalSize(string dir)
    {
        long total   = Directory.GetFiles(Path.Combine(dir, "Build")).Sum(f => new FileInfo(f).Length);
        string index = Path.Combine(dir, "index.html");
        File.WriteAllText(index, File.ReadAllText(index).Replace(SizePlaceholder, total.ToString()));
    }
}
