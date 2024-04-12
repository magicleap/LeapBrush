using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    [CustomEditor(typeof(ShaderWarming))]
    public class ShaderWarmingEditor : Editor
    {
        private string shaderLogString = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ShaderWarming shaderWarming = (ShaderWarming) target;

            EditorGUILayout.HelpBox(
                "Log lines are output when ShaderWarming has the [Capture Used Shaders] " +
                "option turned on. Copy the log lines that include \"UsedNotWarmed\" and/or " +
                "\"WarmedNotUsed\" into the following text area and press the button below to " +
                "update the collection.",
                MessageType.Info);
            GUILayout.Label("Shader Log Strings (One Per Line):");
            shaderLogString = GUILayout.TextArea(shaderLogString);

            if (GUILayout.Button("Update Prewarms from Shader Log Strings"))
            {
                string[] lines = shaderLogString.Trim().Split("\n");
                foreach (string line in lines)
                {
                    string[] pieces = line.Trim().Split(";");
                    if (pieces.Length != 4)
                    {
                        Debug.LogError($"Invalid format for shader log string: {line}");
                        return;
                    }

                    bool wasUsed = pieces[0].EndsWith("UsedNotWarmed");
                    if (!wasUsed && !pieces[0].EndsWith("WarmedNotUsed"))
                    {
                        Debug.LogError("Invalid format for shader log string, expected to find " +
                                       $"\"UsedNotWarmed\" or \"WarmedNotUsed\" present: {line}");
                        return;
                    }

                    string shaderName = pieces[1];

                    List<string> keywords = new();
                    if (pieces[2].Length > 0)
                    {
                        keywords = pieces[2].Split(",").ToList();
                        keywords.Sort();
                    }

                    List<ShaderWarming.VertexAttributeDescriptorSerializable> vertexAttributes =
                        new();
                    if (pieces[3].Length > 0)
                    {
                        foreach (string vertexAttributeStr in pieces[3].Split(","))
                        {
                            vertexAttributes.Add(
                                ShaderWarming.VertexAttributeDescriptorSerializable.FromString(
                                    vertexAttributeStr));
                        }
                    }

                    Shader shader = Shader.Find(shaderName);
                    if (shader == null)
                    {
                        Debug.LogError($"Shader {shaderName} not found");
                        return;
                    }

                    ShaderWarming.ShaderWarmingInfo matchedInfo = null;
                    foreach (ShaderWarming.ShaderWarmingInfo info in shaderWarming
                                 .ShaderWarmingInfos)
                    {
                        if (info.shader != shader)
                        {
                            continue;
                        }

                        if (info.keywords.Count != keywords.Count)
                        {
                            continue;
                        }

                        bool keywordsMatch = true;
                        for (int i = 0; i < info.keywords.Count; i++)
                        {
                            if (info.keywords[i] != keywords[i])
                            {
                                keywordsMatch = false;
                            }
                        }

                        if (!keywordsMatch)
                        {
                            continue;
                        }

                        if (info.vertexDescriptors.Count != vertexAttributes.Count)
                        {
                            continue;
                        }

                        bool vertexAttributesMatch = true;
                        for (int i = 0; i < info.vertexDescriptors.Count; i++)
                        {
                            if (!info.vertexDescriptors[i].Equals(vertexAttributes[i]))
                            {
                                vertexAttributesMatch = false;
                            }
                        }

                        if (!vertexAttributesMatch)
                        {
                            continue;
                        }

                        matchedInfo = info;
                        break;
                    }

                    if (wasUsed && matchedInfo == null)
                    {
                        matchedInfo = new ShaderWarming.ShaderWarmingInfo
                        {
                            shader = shader,
                            keywords = keywords,
                            vertexDescriptors = vertexAttributes
                        };
                        shaderWarming.ShaderWarmingInfos.Add(matchedInfo);
                    }

                    if (!wasUsed && matchedInfo != null)
                    {
                        shaderWarming.ShaderWarmingInfos.Remove(matchedInfo);
                    }
                }

                shaderLogString = "";

                EditorUtility.SetDirty(shaderWarming);
            }
        }
    }
}
