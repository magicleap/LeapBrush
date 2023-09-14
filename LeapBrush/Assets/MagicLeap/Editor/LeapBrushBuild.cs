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
        public static Func<BuildPlayerOptions, BuildReport> BuildPlayer = (options) =>
        {
            return BuildPipeline.BuildPlayer(options);
        };

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

            List<string> scenePaths = new();
            foreach (var sceneInfo in EditorBuildSettings.scenes)
            {
                if (sceneInfo.enabled)
                {
                    scenePaths.Add(sceneInfo.path);
                }
            }
            buildPlayerOptions.scenes = scenePaths.ToArray();

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

                report = BuildPlayer(buildPlayerOptions);

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

                report = BuildPlayer(buildPlayerOptions);

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

                report = BuildPlayer(buildPlayerOptions);

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
                using (CreateAndroidSigningContext())
                {

                    buildPlayerOptions.locationPathName =
                        Path.Join(outputDirWithVersion,
                            Application.identifier + "-" + versionString + ".apk");

                    report = BuildPlayer(buildPlayerOptions);
                }
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

        class AndroidSigningContext : IDisposable
        {
            private readonly bool _enabled;
            private readonly string _oldKeystoreName;
            private readonly bool _oldUseCustomKeystore;
            private readonly string _oldKeystorePass;
            private readonly string _oldKeyaliasName;
            private readonly string _oldKeyaliasPass;

            public AndroidSigningContext()
            {
                String keystorePath = Environment.GetEnvironmentVariable("UNITY_KEYSTORE_PATH");
                _enabled = !string.IsNullOrEmpty(keystorePath);
                if (!_enabled)
                {
                    return;
                }

                _oldKeystoreName = PlayerSettings.Android.keystoreName;
                PlayerSettings.Android.keystoreName = keystorePath;

                _oldUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
                PlayerSettings.Android.useCustomKeystore = true;

                _oldKeystorePass = PlayerSettings.Android.keystorePass;
                PlayerSettings.Android.keystorePass =
                    Environment.GetEnvironmentVariable("UNITY_KEYSTORE_PASS");

                _oldKeyaliasName = PlayerSettings.Android.keyaliasName;
                PlayerSettings.Android.keyaliasName =
                    Environment.GetEnvironmentVariable("UNITY_KEYALIAS_NAME");

                _oldKeyaliasPass = PlayerSettings.Android.keyaliasPass;
                PlayerSettings.Android.keyaliasPass =
                    Environment.GetEnvironmentVariable("UNITY_KEYALIAS_PASS");
            }

            public void Dispose()
            {
                if (!_enabled)
                {
                    return;
                }

                PlayerSettings.Android.keystoreName = _oldKeystoreName;
                PlayerSettings.Android.useCustomKeystore = _oldUseCustomKeystore;
                PlayerSettings.Android.keystorePass = _oldKeystorePass;
                PlayerSettings.Android.keyaliasName = _oldKeyaliasName;
                PlayerSettings.Android.keyaliasPass = _oldKeyaliasPass;
            }
        }

        private static IDisposable CreateAndroidSigningContext()
        {
            return new AndroidSigningContext();
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
