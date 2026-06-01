using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BlurShadersPro
{
    public class BlurInstallerWindow : EditorWindow
    {
        private static int width = 400, height = 600;
        private static Texture2D bannerTexture;

        private static GUIStyle _headerStyle;
        private static GUIStyle headerStyle
        {
            get
            {
                if (_headerStyle == null)
                {
                    _headerStyle = new GUIStyle(GUI.skin.label)
                    {
                        wordWrap = true,
                        fontSize = 16,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    };
                }

                return _headerStyle;
            }
        }

        private static GUIStyle _buttonStyle;
        private static GUIStyle buttonStyle
        {
            get
            {
                if (_buttonStyle == null)
                {
                    _buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 15,
                        fontStyle = FontStyle.Bold,
                        padding = new RectOffset(0, 0, 8, 8)
                    };
                }

                return _buttonStyle;
            }
        }

        private static GUIStyle _infoStyle;
        private static GUIStyle infoStyle
        {
            get
            {
                if (_infoStyle == null)
                {
                    _infoStyle = new GUIStyle(GUI.skin.label)
                    {
                        richText = true,
                        wordWrap = true,
                        fontSize = 12
                    };
                }

                return _infoStyle;
            }
        }

        private static GUIStyle _smallHeaderStyle;
        private static GUIStyle smallHeaderStyle
        {
            get
            {
                if (_smallHeaderStyle == null)
                {
                    _smallHeaderStyle = new GUIStyle(GUI.skin.label)
                    {
                        richText = true,
                        wordWrap = true,
                        fontSize = 12,
                        fontStyle = FontStyle.Bold
                    };
                }

                return _smallHeaderStyle;
            }
        }

        private static GUIStyle _pipelineBox;
        private static GUIStyle pipelineBox
        {
            get
            {
                if (_pipelineBox == null)
                {
                    _pipelineBox = new GUIStyle(EditorStyles.helpBox)
                    {
                        padding = new RectOffset(10, 10, 10, 10),
                        margin = new RectOffset(5, 5, 0, 0)
                    };
                }

                return _pipelineBox;
            }
        }

        [MenuItem("Tools/Blur Shaders Pro/Open Installer Window", false, 0)]
        public static void ShowWindow()
        {
            EditorWindow editorWindow = GetWindow(typeof(BlurInstallerWindow), false, "Blur Pro Installer", true);

            editorWindow.autoRepaintOnSceneChange = true;
            editorWindow.ShowAuxWindow();

            editorWindow.position = new Rect((Screen.currentResolution.width / 2f) - (width * 0.5f), (Screen.currentResolution.height / 2f) - (height * 0.7f), width, height);

            editorWindow.maxSize = new Vector2(width, height);
            editorWindow.minSize = new Vector2(width, height);

            BlurInstaller.Initialize();

            editorWindow.Show();
        }

        private void OnEnable()
        {
            BlurInstaller.Initialize();
        }

        private void OnGUI()
        {
            var compatiblePipelines = BlurInstaller.GetCompatiblePipelines();
            var installedPipelines = BlurInstaller.GetInstalledPipelines();

            // Try and retrieve the banner texture if we don't have it yet.
            if (bannerTexture == null)
            {
                bannerTexture = Resources.Load<Texture2D>("InstallerBanner");
            }

            if (bannerTexture != null)
            {
                var height = width * bannerTexture.height / bannerTexture.width;
                Rect bannerRect = new Rect(0, 0, width, height);
                GUI.DrawTexture(bannerRect, bannerTexture, ScaleMode.ScaleToFit);
                GUILayout.Space(height);
            }
            else
            {
                EditorGUILayout.LabelField("Blur Shaders Pro", headerStyle);
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Thanks for downloading Blur Shaders Pro.", headerStyle);

            if (installedPipelines.Count == 0)
            {
                EditorGUILayout.LabelField("Let's get you set up.", headerStyle);
            }
            else
            {
                EditorGUILayout.LabelField("Looks like you have it installed!", headerStyle);
            }

            if (installedPipelines.Contains(BlurInstaller.Pipeline.BuiltinPostProcess))
            {
                if (compatiblePipelines.Contains(BlurInstaller.Pipeline.URP) || compatiblePipelines.Contains(BlurInstaller.Pipeline.HDRP)
                    || installedPipelines.Contains(BlurInstaller.Pipeline.URP) || installedPipelines.Contains(BlurInstaller.Pipeline.HDRP))
                {
                    GUILayout.Space(5);

                    using (new EditorGUILayout.VerticalScope(pipelineBox))
                    {
                        EditorGUILayout.LabelField("<b><color=\"red\">Warning:</color></b> The built-in pipeline package is installed but you are using URP/HDRP. Consider deleting the \"Built-in Pipeline\" folder.", infoStyle);
                    }
                }
            }

            GUILayout.Space(10);

            if (installedPipelines.Contains(BlurInstaller.Pipeline.URP))
            {
                using (new EditorGUILayout.VerticalScope(pipelineBox))
                {
                    EditorGUILayout.LabelField("URP version already installed.", smallHeaderStyle);

                    if (GUILayout.Button("Re-install Blur Shaders Pro for URP", buttonStyle))
                    {
                        BlurInstaller.InstallBlurURP();
                    }
                }

                GUILayout.Space(10);
            }
            else if (compatiblePipelines.Contains(BlurInstaller.Pipeline.URP))
            {
                using (new EditorGUILayout.VerticalScope(pipelineBox))
                {
                    EditorGUILayout.LabelField("Universal Render Pipeline detected.", smallHeaderStyle);

                    EditorGUILayout.LabelField("<i>Blur Shaders Pro</i> for URP not detected. It may have been moved or some files may have been deleted.", infoStyle);

                    if (GUILayout.Button("Install Blur Shaders Pro for URP", buttonStyle))
                    {
                        BlurInstaller.InstallBlurURP();
                    }
                }

                GUILayout.Space(10);
            }

            if (installedPipelines.Contains(BlurInstaller.Pipeline.HDRP))
            {
                using (new EditorGUILayout.VerticalScope(pipelineBox))
                {
                    EditorGUILayout.LabelField("HDRP version already installed.", smallHeaderStyle);

                    if (GUILayout.Button("Re-install Blur Shaders Pro for HDRP", buttonStyle))
                    {
                        BlurInstaller.InstallBlurHDRP();
                    }
                }

                GUILayout.Space(10);
            }
            else if (compatiblePipelines.Contains(BlurInstaller.Pipeline.HDRP))
            {
                using (new EditorGUILayout.VerticalScope(pipelineBox))
                {
                    EditorGUILayout.LabelField("High Definition Render Pipeline detected.", smallHeaderStyle);

                    EditorGUILayout.LabelField("<i>Blur Shaders Pro</i> for HDRP not detected. It may have been moved or some files may have been deleted.", infoStyle);

                    if (GUILayout.Button("Install Blur Shaders Pro for HDRP", buttonStyle))
                    {
                        BlurInstaller.InstallBlurHDRP();
                    }
                }

                GUILayout.Space(10);
            }

            if (compatiblePipelines.Contains(BlurInstaller.Pipeline.BuiltinAlone))
            {
                using (new EditorGUILayout.VerticalScope(pipelineBox))
                {
                    EditorGUILayout.LabelField("Built-in Pipeline detected.", smallHeaderStyle);

                    EditorGUILayout.LabelField("Post Processing Stack not detected.", infoStyle);
                    EditorGUILayout.LabelField("<i>Blur Shaders Pro</i> for the built-in render pipeline requires the Post Processing Stack v2 package to be installed.", infoStyle);

                    if (GUILayout.Button("Install the Post Processing Stack v2 package.", buttonStyle))
                    {
                        BlurInstaller.InstallBuiltinPostProcess();
                    }
                }
            }
            else
            {
                if (installedPipelines.Contains(BlurInstaller.Pipeline.BuiltinPostProcess))
                {
                    using (new EditorGUILayout.VerticalScope(pipelineBox))
                    {
                        EditorGUILayout.LabelField("Built-in version already installed.", smallHeaderStyle);

                        if (GUILayout.Button("Re-install Blur Shaders Pro for Built-in", buttonStyle))
                        {
                            BlurInstaller.InstallBlurBuiltin();
                        }
                    }
                }
                else if (compatiblePipelines.Contains(BlurInstaller.Pipeline.BuiltinPostProcess))
                {
                    using (new EditorGUILayout.VerticalScope(pipelineBox))
                    {
                        EditorGUILayout.LabelField("Built-in Pipeline detected.", smallHeaderStyle);

                        EditorGUILayout.LabelField("Post Processing Stack v2 detected.", infoStyle);

                        EditorGUILayout.LabelField("<i>Blur Shaders Pro</i> for the built-in pipeline not detected. It may have been moved or some files may have been deleted.", infoStyle);

                        if (GUILayout.Button("Install Blur Shaders Pro for Built-in", buttonStyle))
                        {
                            BlurInstaller.InstallBlurBuiltin();
                        }
                    }
                }
            }
        }
    }
}
