using System;
using System.IO;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    [ExecuteAlways]
    public class OpenSourceNoticesResource : MonoBehaviour
    {
        public const string ResourceName = SourceFileName;

        private const string SourceFileName = "NOTICE_binaries.txt.gz";
        private const string ResourceFileName = ResourceName + ".bytes";

        private void OnValidate()
        {
            FileInfo srcPathInfo = new FileInfo(Path.Join(
                new DirectoryInfo(Application.dataPath).Parent.Parent.FullName,
                SourceFileName));

            FileInfo destPathInfo = new FileInfo(Path.Join(
                Path.Join(Application.dataPath,
                "Resources"), ResourceFileName));

            if (!srcPathInfo.Exists)
            {
                throw new IOException($"Expected {srcPathInfo} to exist");
            }

            TimeSpan lastWriteTimeDelta = destPathInfo.LastWriteTimeUtc
                                          - srcPathInfo.LastWriteTimeUtc;

            if (!destPathInfo.Exists
                || Math.Floor(Math.Abs(lastWriteTimeDelta.TotalSeconds)) > 0
                || srcPathInfo.Length != destPathInfo.Length)
            {
                Debug.Log($"Importing modified {ResourceName} file");
                File.Copy(srcPathInfo.FullName, destPathInfo.FullName, true);
                File.SetLastWriteTimeUtc(destPathInfo.FullName, srcPathInfo.LastWriteTimeUtc);
            }
        }
    }
}