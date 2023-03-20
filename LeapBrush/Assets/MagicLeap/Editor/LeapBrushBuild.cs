using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MagicLeap
{
    public class LeapBrushBuild
    {
        public static void CommandLineBuild()
        {
            LeapBrushBuildUserSettings settings =
                ScriptableObject.CreateInstance<LeapBrushBuildUserSettings>();

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-outputDir" && args.Length > i + 1)
                {
                    settings.OutputPath = args[i + 1];
                }
                if (args[i] == "-versionStringSuffix" && args.Length > i + 1)
                {
                    settings.VersionStringSuffix = args[i + 1];
                }
            }

            Build(settings);
        }

        public static void Build(LeapBrushBuildUserSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.OutputPath))
            {
                throw new Exception("Argument or setting -outputDir is required");
            }

            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;

            if (buildTarget == BuildTarget.Android)
            {
                PlayerSettings.SetScriptingBackend(
                    BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            }
            else
            {
                PlayerSettings.SetScriptingBackend(
                    BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            }
#if UNITY_STANDALONE_OSX
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.x64;
#endif

            BuildPlayerOptions buildPlayerOptions = new();
            buildPlayerOptions.scenes = new string[1];
            buildPlayerOptions.scenes[0] = "Assets/MagicLeap/LeapBrush/Scenes/LeapBrush.unity";

            if (!Directory.Exists(settings.OutputPath))
            {
                throw new Exception("Expected output directory " + settings.OutputPath + " to exist");
            }

            string versionString = "v" + PlayerSettings.Android.bundleVersionCode;
            if (!string.IsNullOrWhiteSpace(settings.VersionStringSuffix))
            {
                versionString += "-" + settings.VersionStringSuffix;
            }

            string outputDirWithVersion = Path.Join(
                settings.OutputPath, versionString);
            if (!Directory.Exists(outputDirWithVersion))
            {
                Directory.CreateDirectory(outputDirWithVersion);
            }

            buildPlayerOptions.target = buildTarget;
            buildPlayerOptions.options = BuildOptions.None;

            BuildReport report;
            if (buildTarget == BuildTarget.StandaloneOSX)
            {
                buildPlayerOptions.locationPathName =
                    Path.Join(outputDirWithVersion,
                        "LeapBrush-Mac-" + versionString + ".app");

                report = BuildPipeline.BuildPlayer(buildPlayerOptions);

                if (report.summary.result == BuildResult.Succeeded)
                {
                    CreateZip(Path.Join(outputDirWithVersion,
                            "LeapBrush-Mac-" + versionString + ".zip"),
                        Path.GetFileName(buildPlayerOptions.locationPathName),
                        outputDirWithVersion,
                        new[]
                        {
                            ".DS_Store",
                        });
                }
            }
            else if (buildTarget == BuildTarget.StandaloneLinux64)
            {
                string linuxParentDir = Path.Join(outputDirWithVersion,
                    "LeapBrush-Linux-" + versionString);
                if (!Directory.Exists(linuxParentDir))
                {
                    Directory.CreateDirectory(linuxParentDir);
                }
                buildPlayerOptions.locationPathName =
                    Path.Join(linuxParentDir, "LeapBrush.x86_64");

                report = BuildPipeline.BuildPlayer(buildPlayerOptions);

                if (report.summary.result == BuildResult.Succeeded)
                {
                    CreateZip(Path.Join(outputDirWithVersion,
                            "LeapBrush-Linux-" + versionString + ".zip"),
                        ".",
                        linuxParentDir,
                        new[]
                        {
                            ".DS_Store",
                            "LeapBrush_BurstDebugInformation_DoNotShip/*",
                            "LeapBrush_BackUpThisFolder_ButDontShipItWithYourGame/*"
                        });
                }
            }
            else if (buildTarget == BuildTarget.StandaloneWindows64)
            {
                string windowsParentDir = Path.Join(outputDirWithVersion,
                    "LeapBrush-Windows-" + versionString);
                if (!Directory.Exists(windowsParentDir))
                {
                    Directory.CreateDirectory(windowsParentDir);
                }
                buildPlayerOptions.locationPathName =
                    Path.Join(windowsParentDir, "LeapBrush.exe");

                report = BuildPipeline.BuildPlayer(buildPlayerOptions);

                if (report.summary.result == BuildResult.Succeeded)
                {
                    CreateZip(Path.Join(outputDirWithVersion,
                            "LeapBrush-Windows-" + versionString + ".zip"),
                        ".",
                        windowsParentDir,
                        new[]
                        {
                            ".DS_Store",
                            "LeapBrush_BurstDebugInformation_DoNotShip/*",
                        });
                }
            }
            else if (buildTarget == BuildTarget.Android)
            {
                buildPlayerOptions.locationPathName =
                    Path.Join(outputDirWithVersion,
                        Application.identifier + "-" + versionString + ".apk");

                report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            }
            else
            {
                throw new Exception("Unexpected build target " + buildTarget);
            }

            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception("Build failed");
            }

            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }

        private static void CreateZip(string outputZipPath, string sourcePath,
            string workingDirectory, string[] excludedFiles)
        {
            List<string> args = new List<string>();
            args.Add("-r");
            args.Add("--filesync");
            args.Add(outputZipPath);
            args.Add(sourcePath);
            foreach (string excludedFile in excludedFiles)
            {
                args.Add("--exclude");
                args.Add(excludedFile);
            }
            if (!ExecuteProcessHelper("zip", args.ToArray(), workingDirectory))
            {
                throw new Exception("Zip failed");
            }
        }

        public static string ShellEscapeArg(string arg)
        {
            return "\"" + arg.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        public static bool ExecuteProcessHelper(string filename, string[] args,
            string workingDirectory)
        {
            StringBuilder argsBuilder = new StringBuilder();
            foreach (string arg in args)
            {
                argsBuilder.Append(ShellEscapeArg(arg)).Append(" ");
            }

            System.Diagnostics.ProcessStartInfo startInfo = new()
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = filename,
                Arguments = argsBuilder.ToString(),
                WorkingDirectory = workingDirectory,
            };

            Debug.Log("Running " + startInfo.FileName + " " + startInfo.Arguments + "...");

            using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo))
            {
                StringBuilder output = new();
                System.Diagnostics.DataReceivedEventHandler outputAndErrorHandler =
                    delegate(object sender, System.Diagnostics.DataReceivedEventArgs e)
                    {
                        lock (output)
                        {
                            output.Append(e.Data);
                        }
                    };
                process.OutputDataReceived += outputAndErrorHandler;
                process.ErrorDataReceived += outputAndErrorHandler;
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                if (output.Length > 0)
                {
                    Debug.LogFormat("{0}: {1}\n", filename, output.ToString());
                }

                return process.ExitCode == 0;
            }
        }
    }
}
