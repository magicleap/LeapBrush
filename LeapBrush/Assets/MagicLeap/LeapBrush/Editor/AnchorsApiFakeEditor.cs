using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.MagicLeap.Native;

namespace MagicLeap.LeapBrush
{
    [CustomEditor(typeof(AnchorsApiFake))]
    public class AnchorsApiFakeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            AnchorsApiFake anchorsApiFake = (AnchorsApiFake) target;

            if (GUILayout.Button("Import Anchors From Device"))
            {
                ImportAnchorsFromDevice(anchorsApiFake);
            }
        }

        private void ImportAnchorsFromDevice(AnchorsApiFake anchorsApiFake)
        {
            List<AnchorsApiFake.FakeAnchor> anchors = AdbGetAnchorData();
            anchorsApiFake.SetFakeAnchors(anchors);
            Debug.Log("Imported " + anchors.Count + " anchors from device");
        }

        private List<AnchorsApiFake.FakeAnchor> AdbGetAnchorData()
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.FileName = GetAdbPath();
            startInfo.Arguments = "shell su 0 pwscli -anchors";

            Debug.Log("Running" + startInfo.FileName + " " + startInfo.Arguments + "...");

            using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo))
            {
                StringBuilder output = new StringBuilder();
                System.Diagnostics.DataReceivedEventHandler outputAndErrorHandler =
                    delegate(object sender, System.Diagnostics.DataReceivedEventArgs e)
                    {
                        lock (output)
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                process.OutputDataReceived += outputAndErrorHandler;
                process.ErrorDataReceived += outputAndErrorHandler;
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new System.Exception("adb shell pwscli failed : " + output);
                }

                return ParseAnchorData(output.ToString());
            }
        }

        private List<AnchorsApiFake.FakeAnchor> ParseAnchorData(string pwscliOutput)
        {
            var anchors = new List<AnchorsApiFake.FakeAnchor>();
            string anchorId = null;
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            foreach (string line in pwscliOutput.Trim().Split("\n"))
            {
                Match match = Regex.Match(line, "^ID: [{]([a-f0-9-]+)[}]$");
                if (match.Success)
                {
                    anchorId = match.Groups[1].Value;
                }

                match = Regex.Match(line, "^Position: [(]([0-9.-]+), ([0-9.-]+), ([0-9.-]+)[)]$");
                if (match.Success)
                {
                    position = new Vector3(
                        float.Parse(match.Groups[1].Value),
                        -float.Parse(match.Groups[2].Value),
                        float.Parse(match.Groups[3].Value));
                    continue;
                }

                match = Regex.Match(line, "^Rotation: [(]([0-9.-]+), ([0-9.-]+), ([0-9.-]+), ([0-9.-]+)[)]$");
                if (match.Success)
                {
                    rotation = new Quaternion(
                        float.Parse(match.Groups[1].Value),
                        -float.Parse(match.Groups[2].Value),
                        float.Parse(match.Groups[3].Value),
                        -float.Parse(match.Groups[4].Value));
                    continue;
                }

                match = Regex.Match(line, "^Space: [{]([a-f0-9-]+)[}]$");
                if (match.Success)
                {
                    string spaceId = match.Groups[1].Value;

                    anchors.Add(new AnchorsApiFake.FakeAnchor
                    {
                        Id = anchorId,
                        SpaceId = spaceId,
                        Pose = new Pose(position, rotation)
                    });
                }
            }

            return anchors;
        }

        private static string GetAdbPath()
        {
            string androidSdkRoot = EditorPrefs.GetString("AndroidSdkRoot");
            if (androidSdkRoot == null)
            {
                throw new System.Exception("Could not find the Android SDK root");
            }

            string path = Path.Join(androidSdkRoot, "platform-tools", "adb");
            if (File.Exists(path))
            {
                return path;
            }

            throw new System.Exception("Could not find a adb binary within the Android SDK");
        }
    }
}
