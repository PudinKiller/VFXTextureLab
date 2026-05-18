#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace PudinKiller.VFXTextureLab
{
    public enum VFXTextureOperationType
    {
        ValuePush,
        GradientMap,
        Invert,
        Levels,
        Threshold,
        Posterize,
        Colorize,
        ChannelPack,
        Math,
        AutoNormalize,
        Blur,
        Dilate,
        Erode
    }

    public enum VFXSourceMode
    {
        CurrentChannel,
        Red,
        Green,
        Blue,
        Alpha,
        LuminanceRGB,
        AverageRGB,
        MaxRGB,
        MinRGB,
        Constant
    }

    public enum VFXPushMode
    {
        Linear,
        Smoothstep,
        Smootherstep,
        LogisticSCurve,
        PowerEaseIn,
        PowerEaseOut,
        Exponential,
        HardStep,
        CustomCurve
    }

    public enum VFXValuePushCurveDomain
    {
        WholeCurvePivot,
        SplitEachSideLegacy
    }

    public enum VFXMathMode
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Power,
        Min,
        Max,
        DifferenceFromConstant
    }

    public enum VFXOutputFormat
    {
        PNG8Bit,
        EXRFloat
    }

    public enum VFXTextureContentMode
    {
        ColorSRGB,
        DataLinear
    }

    public enum VFXOutputFilterMode
    {
        CopyFromSource,
        Point,
        Bilinear,
        Trilinear
    }

    public enum VFXGradientMapMode
    {
        UnityGradient,
        RGBACurves,
        HSVCurves
    }

    public enum VFXPreviewMode
    {
        Color,
        Red,
        Green,
        Blue,
        Alpha,
        Luminance
    }

    [Serializable]
    public class VFXTextureLabOperation
    {
        public bool enabled = true;
        public bool foldout = true;
        public string customName = string.Empty;
        public VFXTextureOperationType type = VFXTextureOperationType.ValuePush;

        public VFXSourceMode sourceMode = VFXSourceMode.CurrentChannel;
        [Range(0f, 1f)] public float constantSourceValue = 1f;
        public bool affectR = true;
        public bool affectG = true;
        public bool affectB = true;
        public bool affectA = false;
        public bool rgbUseSingleSource = true;
        public bool invertSource = false;
        public bool invertResult = false;
        public bool clampResult01 = true;

        [Range(0f, 1f)] public float origin = 0.5f;
        public Vector2 lowerOutputRange = new Vector2(0.0f, 0.25f);
        public Vector2 upperOutputRange = new Vector2(0.75f, 1.0f);
        public VFXPushMode pushMode = VFXPushMode.Smoothstep;
        public VFXValuePushCurveDomain valuePushCurveDomain = VFXValuePushCurveDomain.WholeCurvePivot;
        [Min(0.01f)] public float power = 2.0f;
        public float exponentialStrength = 3.0f;
        [Min(0.01f)] public float logisticSteepness = 10.0f;
        public AnimationCurve customLowerCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve customUpperCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public VFXGradientMapMode gradientMapMode = VFXGradientMapMode.UnityGradient;
        public Gradient gradient = CreateDefaultGradient();
        public AnimationCurve redCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve greenCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve blueCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve alphaCurve = ConstantCurve(1f);
        public AnimationCurve hueCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve saturationCurve = ConstantCurve(0.9f);
        public AnimationCurve valueCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public bool gradientWriteRGB = true;
        public bool gradientWriteAlpha = false;
        public bool preserveOriginalAlpha = true;
        public int activeGradientCurveSlot = 0;
        public bool showSeparateCurveFields = false;

        public Vector2 inputRange = new Vector2(0f, 1f);
        [Min(0.001f)] public float gamma = 1f;
        public Vector2 outputRange = new Vector2(0f, 1f);

        [Range(0f, 1f)] public float threshold = 0.5f;
        [Range(0f, 1f)] public float thresholdFeather = 0f;
        public float thresholdLowValue = 0f;
        public float thresholdHighValue = 1f;

        [Min(2)] public int posterizeSteps = 4;

        public Color tintColor = Color.white;
        public bool colorizeUseSourceAsIntensity = true;
        public bool colorizeWriteAlpha = false;

        public VFXMathMode mathMode = VFXMathMode.Multiply;
        public float mathValue = 1f;

        [Min(1)] public int kernelRadius = 1;
        [Min(1)] public int kernelIterations = 1;

        public bool AffectsChannel(int channel)
        {
            switch (channel)
            {
                case 0: return affectR;
                case 1: return affectG;
                case 2: return affectB;
                case 3: return affectA;
                default: return false;
            }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(customName)) return customName;
                switch (type)
                {
                    case VFXTextureOperationType.ValuePush: return "Contrast / Origin Push";
                    case VFXTextureOperationType.GradientMap: return "Gradient Mapper / Curve Atlas";
                    case VFXTextureOperationType.Invert: return "Invert / Revert Color";
                    case VFXTextureOperationType.Levels: return "Levels / Remap";
                    case VFXTextureOperationType.Threshold: return "Threshold / Mask Clean";
                    case VFXTextureOperationType.Posterize: return "Posterize / Bands";
                    case VFXTextureOperationType.Colorize: return "Colorize / Tint";
                    case VFXTextureOperationType.ChannelPack: return "Channel Pack / Copy";
                    case VFXTextureOperationType.Math: return "Math";
                    case VFXTextureOperationType.AutoNormalize: return "Auto Normalize";
                    case VFXTextureOperationType.Blur: return "Blur";
                    case VFXTextureOperationType.Dilate: return "Dilate / Expand Mask";
                    case VFXTextureOperationType.Erode: return "Erode / Shrink Mask";
                    default: return type.ToString();
                }
            }
        }

        public static Gradient CreateDefaultGradient()
        {
            Gradient newGradient = new Gradient();
            newGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 0f),
                    new GradientColorKey(new Color(0.1f, 0.1f, 0.55f), 0.25f),
                    new GradientColorKey(new Color(0.1f, 0.8f, 1.0f), 0.55f),
                    new GradientColorKey(new Color(1.0f, 0.9f, 0.25f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return newGradient;
        }

        public static AnimationCurve ConstantCurve(float value)
        {
            return new AnimationCurve(new Keyframe(0f, value), new Keyframe(1f, value));
        }
    }

    [Serializable]
    public class VFXTextureLabSettings
    {
        public List<VFXTextureLabOperation> operations = new List<VFXTextureLabOperation>();

        public bool overwriteInputTextures = false;
        public string outputFolder = "Assets/VFXTextureLabOutput";
        public string outputSuffix = "_edited";
        public VFXOutputFormat outputFormat = VFXOutputFormat.PNG8Bit;
        public VFXTextureContentMode contentMode = VFXTextureContentMode.DataLinear;
        public bool generateMipMaps = false;
        public bool forceUncompressedImport = true;
        public VFXOutputFilterMode outputFilterMode = VFXOutputFilterMode.CopyFromSource;
        public bool copyWrapModeFromSource = true;
    }

    [CreateAssetMenu(fileName = "VFXTextureLabPreset", menuName = "Moshui/VFX/Texture Lab Preset")]
    public class VFXTextureLabPreset : ScriptableObject
    {
        public VFXTextureLabSettings settings = new VFXTextureLabSettings();
    }

    public class VFXTextureLabWindow : EditorWindow
    {
        [SerializeField] private List<Texture2D> inputTextures = new List<Texture2D>();
        [SerializeField] private VFXTextureLabSettings settings = new VFXTextureLabSettings();
        [SerializeField] private VFXTextureLabPreset preset;
        [SerializeField] private Texture2D previewSource;
        [SerializeField] private bool autoPreview = true;
        [SerializeField] private VFXPreviewMode previewMode = VFXPreviewMode.Color;
        [SerializeField] private int previewIndex = -1;
        [SerializeField] private bool showInput = true;
        [SerializeField] private bool showOutput = true;
        [SerializeField] private bool showPreview = true;
        [SerializeField] private bool showPreviewControls = false;
        [SerializeField] private bool showTips = true;

        private Texture2D previewProcessedTexture;
        private Texture2D previewSourceDisplayTexture;
        private Texture2D previewResultDisplayTexture;
        private ReorderableList inputList;
        private Vector2 scroll;
        private string lastStatus;
        private GUIStyle sectionStyle;
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle smallMutedStyle;
        private VFXTextureLabOperation draggingCurveOperation;
        private int draggingCurveSlot = -1;
        private int draggingCurveKeyIndex = -1;

        [MenuItem("Tools/PudinKiller/VFX Texture Lab")]
        public static void ShowWindow()
        {
            VFXTextureLabWindow window = GetWindow<VFXTextureLabWindow>();
            window.titleContent = new GUIContent("VFX Texture Lab");
            window.minSize = new Vector2(650, 760);
            window.Show();
        }

        private void OnEnable()
        {
            BuildInputList();
            EnsureValidSettings();
        }

        private void OnDisable()
        {
            DestroyPreviewTextures();
        }

        private void MakeStyles()
        {
            if (sectionStyle != null) return;

            sectionStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 12),
                margin = new RectOffset(8, 8, 10, 16)
            };

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                margin = new RectOffset(0, 0, 2, 8)
            };

            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 6, 4)
            };

            smallMutedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };
        }

        private void EnsureValidSettings()
        {
            if (settings == null) settings = new VFXTextureLabSettings();
            if (settings.operations == null) settings.operations = new List<VFXTextureLabOperation>();
            for (int i = 0; i < settings.operations.Count; i++)
            {
                if (settings.operations[i] == null) settings.operations[i] = new VFXTextureLabOperation();
                EnsureValidOperation(settings.operations[i]);
            }
        }

        private static void EnsureValidOperation(VFXTextureLabOperation operation)
        {
            if (operation == null) return;
            if (operation.type == VFXTextureOperationType.ValuePush)
            {
                operation.lowerOutputRange = new Vector2(0f, 0.5f);
                operation.upperOutputRange = new Vector2(0.5f, 1f);
                operation.valuePushCurveDomain = VFXValuePushCurveDomain.WholeCurvePivot;
            }
            if (operation.gradient == null) operation.gradient = VFXTextureLabOperation.CreateDefaultGradient();
            if (operation.customLowerCurve == null) operation.customLowerCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            if (operation.customUpperCurve == null) operation.customUpperCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            if (operation.redCurve == null) operation.redCurve = AnimationCurve.Linear(0, 0, 1, 1);
            if (operation.greenCurve == null) operation.greenCurve = AnimationCurve.Linear(0, 0, 1, 1);
            if (operation.blueCurve == null) operation.blueCurve = AnimationCurve.Linear(0, 0, 1, 1);
            if (operation.alphaCurve == null) operation.alphaCurve = VFXTextureLabOperation.ConstantCurve(1f);
            if (operation.hueCurve == null) operation.hueCurve = AnimationCurve.Linear(0, 0, 1, 1);
            if (operation.saturationCurve == null) operation.saturationCurve = VFXTextureLabOperation.ConstantCurve(0.9f);
            if (operation.valueCurve == null) operation.valueCurve = AnimationCurve.Linear(0, 0, 1, 1);
            operation.power = Mathf.Max(0.01f, operation.power);
            operation.logisticSteepness = Mathf.Max(0.01f, operation.logisticSteepness);
            operation.gamma = Mathf.Max(0.001f, operation.gamma);
            operation.posterizeSteps = Mathf.Max(2, operation.posterizeSteps);
            operation.kernelRadius = Mathf.Max(1, operation.kernelRadius);
            operation.kernelIterations = Mathf.Max(1, operation.kernelIterations);
        }

        private void BuildInputList()
        {
            inputList = new ReorderableList(inputTextures, typeof(Texture2D), true, true, true, true);
            inputList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, new GUIContent("Input Textures", "Textures processed by the operation stack. You can drag project textures here or use Add Selected."));
            inputList.elementHeight = EditorGUIUtility.singleLineHeight + 6f;
            inputList.drawElementCallback = (rect, index, active, focused) =>
            {
                rect.y += 3f;
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.BeginChangeCheck();
                inputTextures[index] = (Texture2D)EditorGUI.ObjectField(rect, inputTextures[index], typeof(Texture2D), false);
                if (EditorGUI.EndChangeCheck())
                {
                    if (previewSource == null && inputTextures[index] != null)
                    {
                        previewSource = inputTextures[index];
                        previewIndex = index;
                        RepaintPreviewIfNeeded();
                    }
                }
            };

            inputList.onAddCallback = list =>
            {
                inputTextures.Add(null);
            };

            inputList.onSelectCallback = list =>
            {
                if (list.index >= 0 && list.index < inputTextures.Count && inputTextures[list.index] != null)
                {
                    previewIndex = list.index;
                    previewSource = inputTextures[list.index];
                    RepaintPreviewIfNeeded();
                }
            };
        }

        private void OnGUI()
        {
            MakeStyles();
            EnsureValidSettings();
            if (inputList == null) BuildInputList();

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            DrawHeader();
            DrawPresetSection();
            DrawInputSection();
            DrawOperationStackSection();
            DrawOutputSection();
            DrawProcessSection();
            EditorGUILayout.EndScrollView();

            DrawPreviewSection();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                EditorGUILayout.LabelField("VFX Texture Lab", headerStyle);
                EditorGUILayout.LabelField("Batch texture utility for VFX masks, packed channels, grayscale-to-color ramps, contrast, threshold cleanup, and small Substance-style edits inside Unity.", smallMutedStyle);

                showTips = EditorGUILayout.Foldout(showTips, new GUIContent("Common recipes", "Quick reminders for common VFX texture workflows."), true);
                if (showTips)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Mask cleanup: Levels -> Threshold -> Dilate or Erode if needed.", smallMutedStyle);
                    EditorGUILayout.LabelField("Vibrant grayscale colorization: Gradient Mapper, Source = LuminanceRGB, Mode = Unity Gradient or HSV Curves.", smallMutedStyle);
                    EditorGUILayout.LabelField("Alpha from grayscale: Channel Pack, Source = LuminanceRGB, enable only A.", smallMutedStyle);
                    EditorGUILayout.LabelField("Packed masks: enable only the channel you want to edit, keep other channels untouched.", smallMutedStyle);
                }
            }
        }

        private void DrawPresetSection()
        {
            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                EditorGUILayout.LabelField("Preset", subHeaderStyle);
                preset = (VFXTextureLabPreset)EditorGUILayout.ObjectField(new GUIContent("Preset Asset", "Stores the full operation stack and output settings as a reusable asset."), preset, typeof(VFXTextureLabPreset), false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(preset == null))
                    {
                        if (GUILayout.Button(new GUIContent("Load", "Replace current settings with this preset.")))
                        {
                            Undo.RecordObject(this, "Load VFX Texture Lab Preset");
                            settings = DeepCopySettings(preset.settings);
                            EnsureValidSettings();
                            RepaintPreviewIfNeeded();
                        }

                        if (GUILayout.Button(new GUIContent("Save", "Save current settings into the assigned preset asset.")))
                        {
                            Undo.RecordObject(preset, "Save VFX Texture Lab Preset");
                            preset.settings = DeepCopySettings(settings);
                            EditorUtility.SetDirty(preset);
                            AssetDatabase.SaveAssets();
                        }
                    }

                    if (GUILayout.Button(new GUIContent("Create New Preset", "Create a new preset asset from the current settings.")))
                    {
                        CreatePresetAsset();
                    }
                }
            }
        }

        private void DrawInputSection()
        {
            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                showInput = EditorGUILayout.Foldout(showInput, new GUIContent("Input", "Textures that will be processed."), true);
                if (!showInput) return;

                inputList.DoLayoutList();

                Rect dropRect = GUILayoutUtility.GetRect(0f, 54f, GUILayout.ExpandWidth(true));
                GUI.Box(dropRect, "Drag Texture2D assets here", EditorStyles.helpBox);
                HandleDragAndDrop(dropRect);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("Add Selected", "Add selected Texture2D assets from the Project window.")))
                    {
                        foreach (UnityEngine.Object obj in Selection.objects)
                        {
                            if (obj is Texture2D texture && !inputTextures.Contains(texture))
                                inputTextures.Add(texture);
                        }
                        TrySetPreviewToFirstValid();
                    }

                    if (GUILayout.Button(new GUIContent("Remove Nulls", "Remove empty input slots.")))
                    {
                        inputTextures.RemoveAll(texture => texture == null);
                        TrySetPreviewToFirstValid();
                    }

                    if (GUILayout.Button(new GUIContent("Clear", "Remove all input textures from this tool. Does not delete assets.")))
                    {
                        inputTextures.Clear();
                        previewSource = null;
                        previewIndex = -1;
                        DestroyPreviewTextures();
                    }
                }
            }
        }

        private void DrawOperationStackSection()
        {
            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Operation Stack", subHeaderStyle);
                    if (GUILayout.Button(new GUIContent("Add Operation", "Add a new processing step. Operations run from top to bottom."), GUILayout.Width(140f)))
                    {
                        OpenAddOperationMenu();
                    }
                }

                EditorGUILayout.LabelField("Operations run in order. Disable, reorder, duplicate, or remove each block without changing the input assets.", smallMutedStyle);

                if (settings.operations.Count == 0)
                {
                    EditorGUILayout.HelpBox("No operations yet. Add Gradient Mapper, Invert, Levels, Contrast, Channel Pack, etc.", MessageType.Warning);
                }

                for (int i = 0; i < settings.operations.Count; i++)
                {
                    VFXTextureLabOperation operation = settings.operations[i];
                    if (operation == null)
                    {
                        settings.operations[i] = new VFXTextureLabOperation();
                        operation = settings.operations[i];
                    }

                    DrawOperation(operation, i);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(settings.operations.Count == 0))
                    {
                        if (GUILayout.Button(new GUIContent("Clear Stack", "Remove every operation from the stack.")))
                        {
                            if (EditorUtility.DisplayDialog("Clear Operation Stack", "Remove all operations?", "Clear", "Cancel"))
                            {
                                Undo.RecordObject(this, "Clear VFX Texture Lab Stack");
                                settings.operations.Clear();
                                RepaintPreviewIfNeeded();
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                }
            }
        }

        private void OpenAddOperationMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Remap/Contrast - Origin Push"), false, () => AddOperation(VFXTextureOperationType.ValuePush));
            menu.AddItem(new GUIContent("Remap/Levels - Input Output Range"), false, () => AddOperation(VFXTextureOperationType.Levels));
            menu.AddItem(new GUIContent("Remap/Threshold - Mask Clean"), false, () => AddOperation(VFXTextureOperationType.Threshold));
            menu.AddItem(new GUIContent("Remap/Posterize - Bands"), false, () => AddOperation(VFXTextureOperationType.Posterize));
            menu.AddSeparator("Remap/");
            menu.AddItem(new GUIContent("Color/Gradient Mapper - Curve Atlas Style"), false, () => AddOperation(VFXTextureOperationType.GradientMap));
            menu.AddItem(new GUIContent("Color/Invert - Revert Color"), false, () => AddOperation(VFXTextureOperationType.Invert));
            menu.AddItem(new GUIContent("Color/Colorize - Simple Tint"), false, () => AddOperation(VFXTextureOperationType.Colorize));
            menu.AddSeparator("Color/");
            menu.AddItem(new GUIContent("Channels/Channel Pack - Copy Source To Channels"), false, () => AddOperation(VFXTextureOperationType.ChannelPack));
            menu.AddItem(new GUIContent("Channels/Auto Normalize"), false, () => AddOperation(VFXTextureOperationType.AutoNormalize));
            menu.AddSeparator("Channels/");
            menu.AddItem(new GUIContent("Filter/Blur"), false, () => AddOperation(VFXTextureOperationType.Blur));
            menu.AddItem(new GUIContent("Filter/Dilate - Expand Bright Mask"), false, () => AddOperation(VFXTextureOperationType.Dilate));
            menu.AddItem(new GUIContent("Filter/Erode - Shrink Bright Mask"), false, () => AddOperation(VFXTextureOperationType.Erode));
            menu.AddSeparator("Filter/");
            menu.AddItem(new GUIContent("Utility/Math"), false, () => AddOperation(VFXTextureOperationType.Math));
            menu.ShowAsContext();
        }

        private void AddOperation(VFXTextureOperationType type)
        {
            Undo.RecordObject(this, "Add VFX Texture Operation");
            VFXTextureLabOperation operation = new VFXTextureLabOperation();
            operation.type = type;
            ApplyTypeDefaults(operation, true);
            settings.operations.Add(operation);
            TrySetPreviewToFirstValid();
            RepaintPreviewIfNeeded();
        }

        private static void ApplyTypeDefaults(VFXTextureLabOperation operation, bool freshAdd)
        {
            if (operation == null) return;

            if (freshAdd)
            {
                operation.enabled = true;
                operation.foldout = true;
                operation.customName = string.Empty;
                operation.invertSource = false;
                operation.invertResult = false;
                operation.clampResult01 = true;
                operation.constantSourceValue = 1f;
            }

            switch (operation.type)
            {
                case VFXTextureOperationType.GradientMap:
                    operation.sourceMode = VFXSourceMode.LuminanceRGB;
                    operation.affectR = true;
                    operation.affectG = true;
                    operation.affectB = true;
                    operation.affectA = false;
                    operation.rgbUseSingleSource = true;
                    operation.gradientWriteRGB = true;
                    operation.gradientWriteAlpha = false;
                    operation.preserveOriginalAlpha = true;
                    operation.gradientMapMode = VFXGradientMapMode.UnityGradient;
                    break;

                case VFXTextureOperationType.Colorize:
                    operation.sourceMode = VFXSourceMode.LuminanceRGB;
                    operation.affectR = true;
                    operation.affectG = true;
                    operation.affectB = true;
                    operation.affectA = false;
                    operation.rgbUseSingleSource = true;
                    break;

                case VFXTextureOperationType.ChannelPack:
                    operation.sourceMode = VFXSourceMode.LuminanceRGB;
                    operation.affectR = false;
                    operation.affectG = false;
                    operation.affectB = false;
                    operation.affectA = true;
                    operation.rgbUseSingleSource = false;
                    break;

                case VFXTextureOperationType.Blur:
                case VFXTextureOperationType.Dilate:
                case VFXTextureOperationType.Erode:
                    operation.sourceMode = VFXSourceMode.CurrentChannel;
                    operation.affectR = true;
                    operation.affectG = true;
                    operation.affectB = true;
                    operation.affectA = false;
                    operation.rgbUseSingleSource = false;
                    operation.kernelRadius = 1;
                    operation.kernelIterations = 1;
                    break;

                case VFXTextureOperationType.ValuePush:
                    operation.lowerOutputRange = new Vector2(0f, 0.5f);
                    operation.upperOutputRange = new Vector2(0.5f, 1f);
                    operation.valuePushCurveDomain = VFXValuePushCurveDomain.WholeCurvePivot;
                    operation.sourceMode = VFXSourceMode.LuminanceRGB;
                    operation.affectR = true;
                    operation.affectG = true;
                    operation.affectB = true;
                    operation.affectA = false;
                    operation.rgbUseSingleSource = true;
                    break;

                case VFXTextureOperationType.Levels:
                case VFXTextureOperationType.Threshold:
                case VFXTextureOperationType.Posterize:
                    operation.sourceMode = VFXSourceMode.LuminanceRGB;
                    operation.affectR = true;
                    operation.affectG = true;
                    operation.affectB = true;
                    operation.affectA = false;
                    operation.rgbUseSingleSource = true;
                    break;

                default:
                    operation.sourceMode = VFXSourceMode.CurrentChannel;
                    operation.affectR = true;
                    operation.affectG = true;
                    operation.affectB = true;
                    operation.affectA = false;
                    operation.rgbUseSingleSource = false;
                    break;
            }
        }

        private void DrawOperation(VFXTextureLabOperation operation, int index)
        {
            EnsureValidOperation(operation);

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    operation.enabled = EditorGUILayout.Toggle(new GUIContent(string.Empty, "Enable or bypass this operation."), operation.enabled, GUILayout.Width(20f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        RepaintPreviewIfNeeded();
                    }

                    operation.foldout = EditorGUILayout.Foldout(operation.foldout, new GUIContent((index + 1) + ". " + operation.DisplayName, "Click to expand or collapse this operation."), true);

                    EditorGUI.BeginChangeCheck();
                    VFXTextureOperationType newType = (VFXTextureOperationType)EditorGUILayout.EnumPopup(new GUIContent(string.Empty, "Change the operation type."), operation.type, GUILayout.Width(175f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(this, "Change VFX Texture Operation Type");
                        operation.type = newType;
                        ApplyTypeDefaults(operation, false);
                        RepaintPreviewIfNeeded();
                    }

                    using (new EditorGUI.DisabledScope(index <= 0))
                    {
                        if (GUILayout.Button(new GUIContent("Up", "Move this operation earlier."), GUILayout.Width(42f)))
                        {
                            SwapOperations(index, index - 1);
                            GUIUtility.ExitGUI();
                        }
                    }

                    using (new EditorGUI.DisabledScope(index >= settings.operations.Count - 1))
                    {
                        if (GUILayout.Button(new GUIContent("Down", "Move this operation later."), GUILayout.Width(52f)))
                        {
                            SwapOperations(index, index + 1);
                            GUIUtility.ExitGUI();
                        }
                    }

                    if (GUILayout.Button(new GUIContent("Dup", "Duplicate this operation."), GUILayout.Width(42f)))
                    {
                        DuplicateOperation(index);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button(new GUIContent("X", "Remove this operation."), GUILayout.Width(28f)))
                    {
                        RemoveOperation(index);
                        GUIUtility.ExitGUI();
                    }
                }

                if (!operation.foldout) return;

                EditorGUI.BeginChangeCheck();
                EditorGUI.indentLevel++;
                operation.customName = EditorGUILayout.TextField(new GUIContent("Custom Name", "Optional name shown in the operation header."), operation.customName);

                switch (operation.type)
                {
                    case VFXTextureOperationType.ValuePush:
                        DrawSourceAndChannels(operation, true, true);
                        operation.origin = EditorGUILayout.Slider(new GUIContent("Origin", "Split point. Values below origin go to the lower output range. Values equal to or above origin go to the upper output range."), operation.origin, 0f, 1f);
                        operation.lowerOutputRange = new Vector2(0f, 0.5f);
                        operation.upperOutputRange = new Vector2(0.5f, 1f);
                        operation.valuePushCurveDomain = VFXValuePushCurveDomain.WholeCurvePivot;
                        operation.pushMode = (VFXPushMode)EditorGUILayout.EnumPopup(new GUIContent("Push Mode", "One whole-texture curve pivoted around Origin. Output is fixed to 0-0.5 below the curve midpoint and 0.5-1 above it."), operation.pushMode);
                        DrawPushModeParameters(operation);
                        DrawMiniValuePushPreview(operation);
                        break;

                    case VFXTextureOperationType.GradientMap:
                        DrawSourceControls(operation, true);
                        operation.gradientMapMode = (VFXGradientMapMode)EditorGUILayout.EnumPopup(new GUIContent("Map Mode", "Unity Gradient is fastest to art direct. RGBA Curves is similar to a CurveLinearColor. HSV Curves gives hue, saturation, and value variation from grayscale."), operation.gradientMapMode);
                        DrawGradientMapControls(operation);
                        operation.gradientWriteRGB = EditorGUILayout.Toggle(new GUIContent("Write RGB", "Replace RGB with the mapped color."), operation.gradientWriteRGB);
                        operation.gradientWriteAlpha = EditorGUILayout.Toggle(new GUIContent("Write Mapped Alpha", "Replace alpha with the gradient or alpha curve result."), operation.gradientWriteAlpha);
                        operation.preserveOriginalAlpha = EditorGUILayout.Toggle(new GUIContent("Preserve Original Alpha", "If mapped alpha is not written, keep the input alpha unchanged."), operation.preserveOriginalAlpha);
                        operation.invertSource = EditorGUILayout.Toggle(new GUIContent("Invert Source", "Invert grayscale input before sampling the color curve."), operation.invertSource);
                        operation.invertResult = EditorGUILayout.Toggle(new GUIContent("Invert Result", "Invert mapped RGB after sampling."), operation.invertResult);
                        operation.clampResult01 = EditorGUILayout.Toggle(new GUIContent("Clamp Result 0-1", "Clamp final values to 0-1. Disable only when writing EXR/HDR values."), operation.clampResult01);
                        DrawGradientStrip(operation);
                        break;

                    case VFXTextureOperationType.Invert:
                        DrawChannelToggles(operation);
                        operation.clampResult01 = EditorGUILayout.Toggle(new GUIContent("Clamp Result 0-1", "Clamp final values to 0-1."), operation.clampResult01);
                        break;

                    case VFXTextureOperationType.Levels:
                        DrawSourceAndChannels(operation, true, true);
                        operation.inputRange = DrawVector2Fields(new GUIContent("Input Range", "Input min/max. Values are normalized between these numbers."), operation.inputRange);
                        operation.gamma = Mathf.Max(0.001f, EditorGUILayout.FloatField(new GUIContent("Gamma", "Midtone control. Above 1 brightens mid values. Below 1 darkens mid values."), operation.gamma));
                        operation.outputRange = DrawVector2Fields(new GUIContent("Output Range", "Output min/max after normalization and gamma."), operation.outputRange);
                        break;

                    case VFXTextureOperationType.Threshold:
                        DrawSourceAndChannels(operation, true, true);
                        operation.threshold = EditorGUILayout.Slider(new GUIContent("Threshold", "Cutoff value. Values above it become High Value; values below it become Low Value."), operation.threshold, 0f, 1f);
                        operation.thresholdFeather = EditorGUILayout.Slider(new GUIContent("Feather", "Softens the threshold around the cutoff. 0 creates a hard binary mask."), operation.thresholdFeather, 0f, 1f);
                        operation.thresholdLowValue = EditorGUILayout.FloatField(new GUIContent("Low Value", "Output for values below the threshold."), operation.thresholdLowValue);
                        operation.thresholdHighValue = EditorGUILayout.FloatField(new GUIContent("High Value", "Output for values above the threshold."), operation.thresholdHighValue);
                        break;

                    case VFXTextureOperationType.Posterize:
                        DrawSourceAndChannels(operation, true, true);
                        operation.posterizeSteps = Mathf.Max(2, EditorGUILayout.IntField(new GUIContent("Steps", "Number of value bands."), operation.posterizeSteps));
                        break;

                    case VFXTextureOperationType.Colorize:
                        DrawSourceControls(operation, true);
                        operation.tintColor = EditorGUILayout.ColorField(new GUIContent("Tint Color", "Single color used for simple tinting. For hue/value variation, use Gradient Mapper instead."), operation.tintColor);
                        operation.colorizeUseSourceAsIntensity = EditorGUILayout.Toggle(new GUIContent("Use Source As Intensity", "Multiply tint color by source grayscale value."), operation.colorizeUseSourceAsIntensity);
                        operation.colorizeWriteAlpha = EditorGUILayout.Toggle(new GUIContent("Write Alpha", "Allow the tint alpha to affect output alpha."), operation.colorizeWriteAlpha);
                        operation.invertSource = EditorGUILayout.Toggle(new GUIContent("Invert Source", "Invert source before tinting."), operation.invertSource);
                        operation.clampResult01 = EditorGUILayout.Toggle(new GUIContent("Clamp Result 0-1", "Clamp final values to 0-1."), operation.clampResult01);
                        break;

                    case VFXTextureOperationType.ChannelPack:
                        DrawSourceAndChannels(operation, true, true);
                        EditorGUILayout.HelpBox("Copies one source value into the selected destination channels. Example: Source = LuminanceRGB, only A enabled, for alpha-from-grayscale.", MessageType.None);
                        break;

                    case VFXTextureOperationType.Math:
                        DrawChannelToggles(operation);
                        operation.mathMode = (VFXMathMode)EditorGUILayout.EnumPopup(new GUIContent("Math Mode", "Math operation applied to selected channels."), operation.mathMode);
                        operation.mathValue = EditorGUILayout.FloatField(new GUIContent("Value", "Constant used by the selected math operation."), operation.mathValue);
                        operation.invertResult = EditorGUILayout.Toggle(new GUIContent("Invert Result", "Invert after math."), operation.invertResult);
                        operation.clampResult01 = EditorGUILayout.Toggle(new GUIContent("Clamp Result 0-1", "Clamp final values to 0-1."), operation.clampResult01);
                        break;

                    case VFXTextureOperationType.AutoNormalize:
                        DrawChannelToggles(operation);
                        operation.invertResult = EditorGUILayout.Toggle(new GUIContent("Invert Result", "Invert normalized result."), operation.invertResult);
                        operation.clampResult01 = EditorGUILayout.Toggle(new GUIContent("Clamp Result 0-1", "Clamp final values to 0-1."), operation.clampResult01);
                        EditorGUILayout.HelpBox("Scans selected channels and remaps their actual min/max to 0-1. Useful when masks look too flat or do not use the full range.", MessageType.None);
                        break;

                    case VFXTextureOperationType.Blur:
                    case VFXTextureOperationType.Dilate:
                    case VFXTextureOperationType.Erode:
                        DrawChannelToggles(operation);
                        operation.kernelRadius = Mathf.Max(1, EditorGUILayout.IntSlider(new GUIContent("Radius", "Pixel radius for the filter kernel."), operation.kernelRadius, 1, 16));
                        operation.kernelIterations = Mathf.Max(1, EditorGUILayout.IntSlider(new GUIContent("Iterations", "Repeats the filter multiple times for a stronger effect."), operation.kernelIterations, 1, 8));
                        operation.clampResult01 = EditorGUILayout.Toggle(new GUIContent("Clamp Result 0-1", "Clamp final values to 0-1."), operation.clampResult01);
                        break;
                }

                EditorGUI.indentLevel--;
                if (EditorGUI.EndChangeCheck())
                {
                    RepaintPreviewIfNeeded();
                }
            }
        }

        private void DrawGradientMapControls(VFXTextureLabOperation operation)
        {
            if (operation.gradientMapMode == VFXGradientMapMode.UnityGradient)
            {
                operation.gradient = EditorGUILayout.GradientField(new GUIContent("Gradient", "Maps source grayscale value into this color gradient. This is the easiest mode for vibrant texture ramps."), operation.gradient);
                return;
            }

            if (operation.gradientMapMode == VFXGradientMapMode.RGBACurves)
            {
                DrawEditableCurveOverlay(operation, VFXGradientMapMode.RGBACurves);
                EditorGUILayout.HelpBox("RGBA Curves works like a CurveLinearColor: grayscale input on X, output channel value on Y.", MessageType.None);
                return;
            }

            DrawEditableCurveOverlay(operation, VFXGradientMapMode.HSVCurves);
            EditorGUILayout.HelpBox("HSV Curves are useful for vibrant VFX textures because hue, saturation, and brightness can vary independently across the grayscale input.", MessageType.None);
        }

        private void DrawEditableCurveOverlay(VFXTextureLabOperation operation, VFXGradientMapMode mode)
        {
            string[] labels = mode == VFXGradientMapMode.RGBACurves
                ? new[] { "R", "G", "B", "A" }
                : new[] { "H", "S", "V", "A" };

            Color[] colors = mode == VFXGradientMapMode.RGBACurves
                ? new[] { Color.red, Color.green, Color.blue, Color.white }
                : new[] { Color.cyan, Color.magenta, Color.yellow, Color.white };

            operation.activeGradientCurveSlot = Mathf.Clamp(operation.activeGradientCurveSlot, 0, 3);
            operation.activeGradientCurveSlot = GUILayout.Toolbar(operation.activeGradientCurveSlot, labels);

            Rect rect = GUILayoutUtility.GetRect(1f, 150f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0.10f, 0.10f, 0.10f, 1f) : new Color(0.84f, 0.84f, 0.84f, 1f));

            Handles.BeginGUI();
            Color previousColor = Handles.color;
            DrawCurveGrid(rect);

            for (int i = 0; i < 4; i++)
            {
                AnimationCurve curve = GetGradientCurve(operation, mode, i);
                float width = i == operation.activeGradientCurveSlot ? 3.0f : 1.5f;
                DrawCurveLine(rect, curve, colors[i], width);
            }

            DrawActiveCurveKeys(rect, GetGradientCurve(operation, mode, operation.activeGradientCurveSlot), colors[operation.activeGradientCurveSlot]);

            Handles.color = previousColor;
            Handles.EndGUI();

            HandleEditableCurveInput(rect, operation, mode);

            EditorGUILayout.LabelField("Edit in one graph: choose a channel above, drag its keys. Double-click empty space to add a key. Right-click a key to delete it.", smallMutedStyle);
            operation.showSeparateCurveFields = EditorGUILayout.Foldout(operation.showSeparateCurveFields, new GUIContent("Advanced: separate Unity curve fields", "Use Unity's built-in curve fields for precise editing, tangent editing, copy/paste, and keyboard shortcuts."), true);
            if (operation.showSeparateCurveFields)
            {
                Rect curveRect = new Rect(0f, 0f, 1f, 1f);
                if (mode == VFXGradientMapMode.RGBACurves)
                {
                    operation.redCurve = EditorGUILayout.CurveField(new GUIContent("Red Curve", "Red output over source grayscale input."), operation.redCurve, Color.red, curveRect);
                    operation.greenCurve = EditorGUILayout.CurveField(new GUIContent("Green Curve", "Green output over source grayscale input."), operation.greenCurve, Color.green, curveRect);
                    operation.blueCurve = EditorGUILayout.CurveField(new GUIContent("Blue Curve", "Blue output over source grayscale input."), operation.blueCurve, Color.blue, curveRect);
                    operation.alphaCurve = EditorGUILayout.CurveField(new GUIContent("Alpha Curve", "Alpha output over source grayscale input."), operation.alphaCurve, Color.white, curveRect);
                }
                else
                {
                    operation.hueCurve = EditorGUILayout.CurveField(new GUIContent("Hue Curve", "Hue over source grayscale input. 0 and 1 are both red, 0.33 is green, 0.66 is blue."), operation.hueCurve, Color.cyan, curveRect);
                    operation.saturationCurve = EditorGUILayout.CurveField(new GUIContent("Saturation Curve", "Color intensity over source grayscale input. 0 is gray, 1 is full color."), operation.saturationCurve, Color.magenta, curveRect);
                    operation.valueCurve = EditorGUILayout.CurveField(new GUIContent("Value Curve", "Brightness over source grayscale input."), operation.valueCurve, Color.yellow, curveRect);
                    operation.alphaCurve = EditorGUILayout.CurveField(new GUIContent("Alpha Curve", "Alpha output over source grayscale input."), operation.alphaCurve, Color.white, curveRect);
                }
            }
        }

        private static AnimationCurve GetGradientCurve(VFXTextureLabOperation operation, VFXGradientMapMode mode, int slot)
        {
            if (mode == VFXGradientMapMode.RGBACurves)
            {
                switch (slot)
                {
                    case 0: return operation.redCurve;
                    case 1: return operation.greenCurve;
                    case 2: return operation.blueCurve;
                    default: return operation.alphaCurve;
                }
            }

            switch (slot)
            {
                case 0: return operation.hueCurve;
                case 1: return operation.saturationCurve;
                case 2: return operation.valueCurve;
                default: return operation.alphaCurve;
            }
        }

        private static void SetGradientCurve(VFXTextureLabOperation operation, VFXGradientMapMode mode, int slot, AnimationCurve curve)
        {
            if (mode == VFXGradientMapMode.RGBACurves)
            {
                switch (slot)
                {
                    case 0: operation.redCurve = curve; break;
                    case 1: operation.greenCurve = curve; break;
                    case 2: operation.blueCurve = curve; break;
                    default: operation.alphaCurve = curve; break;
                }
                return;
            }

            switch (slot)
            {
                case 0: operation.hueCurve = curve; break;
                case 1: operation.saturationCurve = curve; break;
                case 2: operation.valueCurve = curve; break;
                default: operation.alphaCurve = curve; break;
            }
        }

        private void HandleEditableCurveInput(Rect rect, VFXTextureLabOperation operation, VFXGradientMapMode mode)
        {
            Event evt = Event.current;
            if (!rect.Contains(evt.mousePosition) && draggingCurveOperation != operation) return;

            int slot = operation.activeGradientCurveSlot;
            AnimationCurve curve = GetGradientCurve(operation, mode, slot);
            if (curve == null)
            {
                curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                SetGradientCurve(operation, mode, slot, curve);
            }

            if (evt.type == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition))
            {
                int nearest = FindNearestCurveKey(rect, curve, evt.mousePosition, 9f);
                if (nearest >= 0)
                {
                    draggingCurveOperation = operation;
                    draggingCurveSlot = slot;
                    draggingCurveKeyIndex = nearest;
                    evt.Use();
                    return;
                }

                if (evt.clickCount >= 2)
                {
                    Keyframe key = MouseToKeyframe(rect, evt.mousePosition);
                    int added = curve.AddKey(key);
                    if (added >= 0)
                    {
                        curve.SmoothTangents(added, 0f);
                        draggingCurveOperation = operation;
                        draggingCurveSlot = slot;
                        draggingCurveKeyIndex = added;
                        RepaintPreviewIfNeeded();
                        evt.Use();
                    }
                }
            }

            if (evt.type == EventType.MouseDrag && draggingCurveOperation == operation && draggingCurveSlot == slot && draggingCurveKeyIndex >= 0)
            {
                MoveCurveKey(rect, curve, draggingCurveKeyIndex, evt.mousePosition);
                RepaintPreviewIfNeeded();
                evt.Use();
            }

            if (evt.type == EventType.MouseUp && draggingCurveOperation == operation)
            {
                draggingCurveOperation = null;
                draggingCurveSlot = -1;
                draggingCurveKeyIndex = -1;
                evt.Use();
            }

            if ((evt.type == EventType.ContextClick || (evt.type == EventType.MouseDown && evt.button == 1)) && rect.Contains(evt.mousePosition))
            {
                int nearest = FindNearestCurveKey(rect, curve, evt.mousePosition, 9f);
                if (nearest >= 0 && curve.length > 2)
                {
                    curve.RemoveKey(nearest);
                    RepaintPreviewIfNeeded();
                    evt.Use();
                }
            }
        }

        private static void DrawCurveGrid(Rect rect)
        {
            Color gridColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(0f, 0f, 0f, 0.16f);
            Handles.color = gridColor;

            for (int i = 1; i < 4; i++)
            {
                float x = Mathf.Lerp(rect.x, rect.xMax, i / 4f);
                float y = Mathf.Lerp(rect.y, rect.yMax, i / 4f);
                Handles.DrawLine(new Vector3(x, rect.y, 0f), new Vector3(x, rect.yMax, 0f));
                Handles.DrawLine(new Vector3(rect.x, y, 0f), new Vector3(rect.xMax, y, 0f));
            }

            Handles.color = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.28f) : new Color(0f, 0f, 0f, 0.28f);
            Handles.DrawLine(new Vector3(rect.x, rect.yMax, 0f), new Vector3(rect.xMax, rect.yMax, 0f));
            Handles.DrawLine(new Vector3(rect.x, rect.y, 0f), new Vector3(rect.x, rect.yMax, 0f));
        }

        private static void DrawCurveLine(Rect rect, AnimationCurve curve, Color color, float width)
        {
            if (curve == null) return;

            Handles.color = color;
            Vector3 previous = Vector3.zero;
            const int samples = 96;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)(samples - 1);
                float value = Mathf.Clamp01(curve.Evaluate(t));
                Vector3 point = new Vector3(
                    Mathf.Lerp(rect.x + 4f, rect.xMax - 4f, t),
                    Mathf.Lerp(rect.yMax - 4f, rect.y + 4f, value),
                    0f);

                if (i > 0) Handles.DrawAAPolyLine(width, previous, point);
                previous = point;
            }
        }

        private static void DrawActiveCurveKeys(Rect rect, AnimationCurve curve, Color color)
        {
            if (curve == null) return;
            Handles.color = color;
            for (int i = 0; i < curve.length; i++)
            {
                Vector2 point = KeyframeToPoint(rect, curve.keys[i]);
                Rect keyRect = new Rect(point.x - 4f, point.y - 4f, 8f, 8f);
                EditorGUI.DrawRect(keyRect, color);
            }
        }

        private static int FindNearestCurveKey(Rect rect, AnimationCurve curve, Vector2 mousePosition, float maxDistance)
        {
            int bestIndex = -1;
            float bestDistance = maxDistance;
            for (int i = 0; i < curve.length; i++)
            {
                Keyframe key = curve.keys[i];
                Vector2 point = KeyframeToPoint(rect, key);
                float distance = Vector2.Distance(point, mousePosition);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        private static Vector2 KeyframeToPoint(Rect rect, Keyframe key)
        {
            return new Vector2(
                Mathf.Lerp(rect.x + 4f, rect.xMax - 4f, Mathf.Clamp01(key.time)),
                Mathf.Lerp(rect.yMax - 4f, rect.y + 4f, Mathf.Clamp01(key.value)));
        }

        private static Keyframe MouseToKeyframe(Rect rect, Vector2 mousePosition)
        {
            float time = Mathf.InverseLerp(rect.x + 4f, rect.xMax - 4f, mousePosition.x);
            float value = Mathf.InverseLerp(rect.yMax - 4f, rect.y + 4f, mousePosition.y);
            return new Keyframe(Mathf.Clamp01(time), Mathf.Clamp01(value));
        }

        private static void MoveCurveKey(Rect rect, AnimationCurve curve, int keyIndex, Vector2 mousePosition)
        {
            if (keyIndex < 0 || keyIndex >= curve.length) return;

            Keyframe key = MouseToKeyframe(rect, mousePosition);
            if (keyIndex > 0)
                key.time = Mathf.Max(key.time, curve.keys[keyIndex - 1].time + 0.001f);
            if (keyIndex < curve.length - 1)
                key.time = Mathf.Min(key.time, curve.keys[keyIndex + 1].time - 0.001f);

            curve.MoveKey(keyIndex, key);
            curve.SmoothTangents(keyIndex, 0f);
        }

        private void DrawGradientStrip(VFXTextureLabOperation operation)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 24f, GUILayout.ExpandWidth(true));
            int steps = Mathf.Max(24, Mathf.RoundToInt(rect.width));
            for (int i = 0; i < steps; i++)
            {
                float t0 = i / (float)steps;
                float t1 = (i + 1) / (float)steps;
                Rect segment = new Rect(Mathf.Lerp(rect.x, rect.xMax, t0), rect.y, Mathf.Max(1f, rect.width / steps + 1f), rect.height);
                Color color = EvaluateGradientMapColor(operation, (t0 + t1) * 0.5f);
                EditorGUI.DrawRect(segment, color);
            }
            GUI.Box(rect, GUIContent.none);
        }

        private void DrawPushModeParameters(VFXTextureLabOperation operation)
        {
            switch (operation.pushMode)
            {
                case VFXPushMode.PowerEaseIn:
                case VFXPushMode.PowerEaseOut:
                    operation.power = Mathf.Max(0.01f, EditorGUILayout.FloatField(new GUIContent("Power", "Higher values make the curve more aggressive."), operation.power));
                    break;

                case VFXPushMode.Exponential:
                    operation.exponentialStrength = EditorGUILayout.Slider(new GUIContent("Exponential Strength", "Positive pushes toward the high end. Negative pushes toward the low end."), operation.exponentialStrength, -10f, 10f);
                    break;

                case VFXPushMode.LogisticSCurve:
                    operation.logisticSteepness = Mathf.Max(0.01f, EditorGUILayout.FloatField(new GUIContent("S-Curve Steepness", "4-8 is subtle, 10-16 is crisp, 20+ is very steep."), operation.logisticSteepness));
                    break;

                case VFXPushMode.CustomCurve:
                    operation.customLowerCurve = EditorGUILayout.CurveField(new GUIContent("Lower Curve", "Custom curve for values below Origin."), operation.customLowerCurve);
                    operation.customUpperCurve = EditorGUILayout.CurveField(new GUIContent("Upper Curve", "Custom curve for values equal to or above Origin."), operation.customUpperCurve);
                    break;
            }
        }

        private void DrawSourceAndChannels(VFXTextureLabOperation operation, bool showSource, bool showPost)
        {
            if (showSource) DrawSourceControls(operation, true);
            DrawChannelToggles(operation);
            if (showPost) DrawPostControls(operation);
        }

        private void DrawSourceControls(VFXTextureLabOperation operation, bool allowConstant)
        {
            operation.sourceMode = (VFXSourceMode)EditorGUILayout.EnumPopup(new GUIContent("Source Value", "Where the operation reads its input value from. CurrentChannel means R reads R, G reads G, etc."), operation.sourceMode);
            if (operation.sourceMode == VFXSourceMode.Constant && allowConstant)
            {
                operation.constantSourceValue = EditorGUILayout.Slider(new GUIContent("Constant Source", "Constant 0-1 value used as source."), operation.constantSourceValue, 0f, 1f);
            }
        }

        private void DrawChannelToggles(VFXTextureLabOperation operation)
        {
            EditorGUILayout.LabelField(new GUIContent("Destination Channels", "Only enabled channels are modified. Disabled channels stay unchanged."));
            using (new EditorGUILayout.HorizontalScope())
            {
                operation.affectR = GUILayout.Toggle(operation.affectR, new GUIContent("R", "Modify red channel."), "Button");
                operation.affectG = GUILayout.Toggle(operation.affectG, new GUIContent("G", "Modify green channel."), "Button");
                operation.affectB = GUILayout.Toggle(operation.affectB, new GUIContent("B", "Modify blue channel."), "Button");
                operation.affectA = GUILayout.Toggle(operation.affectA, new GUIContent("A", "Modify alpha channel."), "Button");
            }
        }

        private void DrawPostControls(VFXTextureLabOperation operation)
        {
            operation.rgbUseSingleSource = EditorGUILayout.Toggle(new GUIContent("RGB Single Source", "Prevents magenta/green color fringing on grayscale textures. When RGB channels are edited, one grayscale source value is used for R, G, and B instead of processing slightly different RGB channels independently. Disable this for packed RGB masks where each channel must be processed separately."), operation.rgbUseSingleSource);
            operation.invertSource = EditorGUILayout.Toggle(new GUIContent("Invert Source", "Invert source value before the operation."), operation.invertSource);
            operation.invertResult = EditorGUILayout.Toggle(new GUIContent("Invert Result", "Invert result after the operation."), operation.invertResult);
            operation.clampResult01 = EditorGUILayout.Toggle(new GUIContent("Clamp Result 0-1", "Clamp final values to 0-1. Disable only when writing HDR EXR values."), operation.clampResult01);
        }

        private static Vector2 DrawVector2Fields(GUIContent label, Vector2 value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                value.x = EditorGUILayout.FloatField(value.x);
                value.y = EditorGUILayout.FloatField(value.y);
            }
            return value;
        }

        private void DrawMiniValuePushPreview(VFXTextureLabOperation operation)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 70f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.15f, 1f) : new Color(0.75f, 0.75f, 0.75f, 1f));

            Handles.BeginGUI();
            Color previousColor = Handles.color;
            Handles.color = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            Vector3 previous = Vector3.zero;
            const int samples = 128;
            for (int i = 0; i < samples; i++)
            {
                float x01 = i / (float)(samples - 1);
                float y01 = PushValue(x01, operation);
                if (operation.clampResult01) y01 = Mathf.Clamp01(y01);

                Vector3 point = new Vector3(
                    Mathf.Lerp(rect.x + 4f, rect.xMax - 4f, x01),
                    Mathf.Lerp(rect.yMax - 4f, rect.y + 4f, Mathf.Clamp01(y01)),
                    0f);

                if (i > 0) Handles.DrawLine(previous, point);
                previous = point;
            }

            Handles.color = new Color(1f, 0.65f, 0.1f, 1f);
            float originX = Mathf.Lerp(rect.x + 4f, rect.xMax - 4f, operation.origin);
            Handles.DrawLine(new Vector3(originX, rect.y + 4f, 0f), new Vector3(originX, rect.yMax - 4f, 0f));
            Handles.color = previousColor;
            Handles.EndGUI();
        }

        private void SwapOperations(int a, int b)
        {
            Undo.RecordObject(this, "Reorder VFX Texture Operations");
            VFXTextureLabOperation temp = settings.operations[a];
            settings.operations[a] = settings.operations[b];
            settings.operations[b] = temp;
            RepaintPreviewIfNeeded();
        }

        private void DuplicateOperation(int index)
        {
            Undo.RecordObject(this, "Duplicate VFX Texture Operation");
            VFXTextureLabOperation duplicate = DeepCopyOperation(settings.operations[index]);
            settings.operations.Insert(index + 1, duplicate);
            RepaintPreviewIfNeeded();
        }

        private void RemoveOperation(int index)
        {
            Undo.RecordObject(this, "Remove VFX Texture Operation");
            settings.operations.RemoveAt(index);
            RepaintPreviewIfNeeded();
        }

        private void DrawPreviewSection()
        {
            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Preview", subHeaderStyle);

                    using (new EditorGUI.DisabledScope(previewSource == null))
                    {
                        if (GUILayout.Button(new GUIContent("Refresh", "Manually regenerate preview."), GUILayout.Width(72f)))
                        {
                            GeneratePreview();
                        }
                    }
                }

                showPreviewControls = EditorGUILayout.Foldout(showPreviewControls, new GUIContent("Preview Controls", "Collapsed by default to keep the preview area compact."), true);
                if (showPreviewControls)
                {
                    EditorGUI.BeginChangeCheck();
                    previewSource = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("Preview Texture", "Texture used by the preview. You can also select one from the input list or use Previous/Next."), previewSource, typeof(Texture2D), false);
                    previewMode = (VFXPreviewMode)EditorGUILayout.EnumPopup(new GUIContent("View Mode", "Show full color or isolate a channel as grayscale."), previewMode);
                    autoPreview = EditorGUILayout.Toggle(new GUIContent("Auto Preview", "Update the preview automatically whenever settings change."), autoPreview);
                    if (EditorGUI.EndChangeCheck())
                    {
                        RepaintPreviewIfNeeded();
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(new GUIContent("Previous", "Preview the previous valid texture in the input list.")))
                        {
                            GoToPreviewTexture(-1);
                        }

                        if (GUILayout.Button(new GUIContent("Next", "Preview the next valid texture in the input list.")))
                        {
                            GoToPreviewTexture(1);
                        }

                        if (GUILayout.Button(new GUIContent("First Input", "Use the first valid input texture for preview.")))
                        {
                            previewSource = GetFirstValidInputTexture();
                            GeneratePreview();
                        }
                    }
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(new GUIContent("Prev", "Preview the previous valid texture in the input list."), GUILayout.Width(56f)))
                        {
                            GoToPreviewTexture(-1);
                        }

                        if (GUILayout.Button(new GUIContent("Next", "Preview the next valid texture in the input list."), GUILayout.Width(56f)))
                        {
                            GoToPreviewTexture(1);
                        }

                        previewMode = (VFXPreviewMode)EditorGUILayout.EnumPopup(new GUIContent("", "Preview channel display mode."), previewMode, GUILayout.Width(120f));

                        bool newAutoPreview = GUILayout.Toggle(autoPreview, new GUIContent("Auto", "Automatically update preview when settings change."), "Button", GUILayout.Width(54f));
                        if (newAutoPreview != autoPreview)
                        {
                            autoPreview = newAutoPreview;
                            RepaintPreviewIfNeeded();
                        }

                        if (previewSource != null)
                        {
                            EditorGUILayout.LabelField(previewSource.name + " | " + previewSource.width + " x " + previewSource.height, smallMutedStyle);
                        }
                    }
                }

                if (previewSourceDisplayTexture != null && previewResultDisplayTexture != null)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawTexturePreviewBox("Original", previewSourceDisplayTexture);
                        DrawTexturePreviewBox("Result", previewResultDisplayTexture);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Choose a preview texture or add input textures. Auto Preview is enabled by default.", MessageType.None);
                }
            }
        }

        private void DrawTexturePreviewBox(string label, Texture2D texture)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel);
                float available = Mathf.Max(120f, (position.width - 76f) * 0.5f);
                float size = Mathf.Clamp(available, 120f, 220f);
                Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
                EditorGUI.DrawPreviewTexture(rect, texture, null, ScaleMode.ScaleToFit);
            }
        }

        private void DrawOutputSection()
        {
            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                showOutput = EditorGUILayout.Foldout(showOutput, new GUIContent("Output / Import", "Controls where processed textures are written and how Unity imports them."), true);
                if (!showOutput) return;

                settings.overwriteInputTextures = EditorGUILayout.Toggle(new GUIContent("Overwrite Input Textures", "If enabled, each processed result is written back to its source asset. Output folder and suffix are not used. Only PNG and EXR source assets can be overwritten safely."), settings.overwriteInputTextures);

                if (settings.overwriteInputTextures)
                {
                    EditorGUILayout.HelpBox("Overwrite mode writes directly over the input texture asset. Output folder, suffix, and output format are ignored. The file extension decides the format: PNG stays PNG, EXR stays EXR. Use version control or duplicate your textures first.", MessageType.Warning);
                }
                else
                {
                    settings.outputFolder = EditorGUILayout.TextField(new GUIContent("Output Folder", "Folder inside Assets where new processed textures will be created."), settings.outputFolder);
                    settings.outputSuffix = EditorGUILayout.TextField(new GUIContent("Output Suffix", "Text appended to the source texture name for new outputs."), settings.outputSuffix);
                    settings.outputFormat = (VFXOutputFormat)EditorGUILayout.EnumPopup(new GUIContent("Output Format", "PNG is 8-bit color and good for most color textures. EXR Float preserves HDR/float data and avoids 8-bit banding."), settings.outputFormat);
                }

                settings.contentMode = (VFXTextureContentMode)EditorGUILayout.EnumPopup(new GUIContent("Content Type", "ColorSRGB is best for vibrant color textures. DataLinear is best for masks, packed channels, height maps, and non-color data."), settings.contentMode);
                settings.generateMipMaps = EditorGUILayout.Toggle(new GUIContent("Generate Mip Maps", "Usually off for masks/UI ramps. Enable for textures that need distance filtering in 3D."), settings.generateMipMaps);
                settings.forceUncompressedImport = EditorGUILayout.Toggle(new GUIContent("Force Uncompressed", "Avoids block compression artifacts and color bleeding in generated textures."), settings.forceUncompressedImport);
                settings.outputFilterMode = (VFXOutputFilterMode)EditorGUILayout.EnumPopup(new GUIContent("Filter Mode", "Texture filtering for the generated asset. Point is crisp. Bilinear is smooth. CopyFromSource keeps the source filter."), settings.outputFilterMode);
                settings.copyWrapModeFromSource = EditorGUILayout.Toggle(new GUIContent("Copy Wrap From Source", "Copies repeat/clamp mode from the input texture."), settings.copyWrapModeFromSource);

                EditorGUILayout.HelpBox("The tool forces the generated importer max size to fit the source resolution, so Unity should not downscale the output after import.", MessageType.None);
            }
        }

        private void DrawProcessSection()
        {
            using (new EditorGUILayout.VerticalScope(sectionStyle))
            {
                int validCount = CountValidInputs();
                EditorGUILayout.LabelField("Ready: " + validCount + " texture(s), " + settings.operations.Count + " operation(s)");

                using (new EditorGUI.DisabledScope(validCount == 0 || settings.operations.Count == 0))
                {
                    if (GUILayout.Button(new GUIContent("Process All Textures", "Run the operation stack on every input texture."), GUILayout.Height(38f)))
                    {
                        ProcessAllTextures();
                    }
                }

                if (!string.IsNullOrEmpty(lastStatus))
                {
                    EditorGUILayout.HelpBox(lastStatus, MessageType.None);
                }
            }
        }

        private void HandleDragAndDrop(Rect dropRect)
        {
            Event evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                    {
                        if (obj is Texture2D texture && !inputTextures.Contains(texture))
                        {
                            inputTextures.Add(texture);
                        }
                    }
                    TrySetPreviewToFirstValid();
                }

                evt.Use();
            }
        }

        private void ProcessAllTextures()
        {
            if (!settings.overwriteInputTextures && !ValidateOutputFolder(settings.outputFolder))
            {
                EditorUtility.DisplayDialog("Invalid Output Folder", "Output folder must be inside Assets, for example: Assets/VFXTextureLabOutput", "OK");
                return;
            }

            if (settings.overwriteInputTextures)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Overwrite Input Textures",
                    "This will overwrite the input texture files on disk. This is destructive. Continue?",
                    "Overwrite",
                    "Cancel");

                if (!confirmed) return;
            }
            else
            {
                EnsureAssetFolder(settings.outputFolder);
            }

            int processed = 0;
            List<Texture2D> validTextures = inputTextures.FindAll(texture => texture != null);

            try
            {
                for (int i = 0; i < validTextures.Count; i++)
                {
                    Texture2D source = validTextures[i];
                    EditorUtility.DisplayProgressBar("VFX Texture Lab", "Processing " + source.name, i / Mathf.Max(1f, validTextures.Count));

                    Texture2D output = ProcessTexture(source, settings);
                    output.name = source.name + settings.outputSuffix;

                    VFXOutputFormat writeFormat;
                    string outputPath = BuildOutputPath(source, out writeFormat);
                    WriteTexture(output, outputPath, writeFormat);
                    DestroyImmediate(output);

                    AssetDatabase.ImportAsset(outputPath);
                    ApplyImporterSettings(source, outputPath, source.width, source.height);
                    processed++;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("VFX Texture Lab Error", exception.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            lastStatus = "Processed " + processed + " texture(s).";
            GeneratePreview();
        }

        private string BuildOutputPath(Texture2D source, out VFXOutputFormat writeFormat)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);

            if (settings.overwriteInputTextures)
            {
                if (!TryGetFormatFromPath(sourcePath, out writeFormat))
                {
                    throw new InvalidOperationException("Overwrite mode only supports PNG and EXR source assets. Use Save As New for this texture: " + sourcePath);
                }
                return sourcePath;
            }

            writeFormat = settings.outputFormat;
            string extension = writeFormat == VFXOutputFormat.EXRFloat ? ".exr" : ".png";
            string baseName = source.name + settings.outputSuffix + extension;
            string path = CombineAssetPath(settings.outputFolder, baseName);
            return AssetDatabase.GenerateUniqueAssetPath(path);
        }

        private static bool TryGetFormatFromPath(string path, out VFXOutputFormat format)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".png")
            {
                format = VFXOutputFormat.PNG8Bit;
                return true;
            }

            if (extension == ".exr")
            {
                format = VFXOutputFormat.EXRFloat;
                return true;
            }

            format = VFXOutputFormat.PNG8Bit;
            return false;
        }

        private static string CombineAssetPath(string folder, string fileName)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            return folder + "/" + fileName;
        }

        private static bool ValidateOutputFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return false;
            folder = folder.Replace('\\', '/');
            return folder == "Assets" || folder.StartsWith("Assets/", StringComparison.Ordinal);
        }

        private static void EnsureAssetFolder(string folder)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            string absolutePath = AssetPathToAbsolutePath(folder);
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
                AssetDatabase.Refresh();
            }
        }

        private static string AssetPathToAbsolutePath(string assetPath)
        {
            assetPath = assetPath.Replace('\\', '/');
            if (assetPath == "Assets") return Application.dataPath;
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                throw new ArgumentException("Path must be inside Assets: " + assetPath);

            return Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
        }

        private static void WriteTexture(Texture2D texture, string assetPath, VFXOutputFormat format)
        {
            byte[] bytes;
            if (format == VFXOutputFormat.EXRFloat)
            {
                bytes = texture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            }
            else
            {
                Texture2D pngTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, true);
                Color[] pixels = texture.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i].r = Mathf.Clamp01(pixels[i].r);
                    pixels[i].g = Mathf.Clamp01(pixels[i].g);
                    pixels[i].b = Mathf.Clamp01(pixels[i].b);
                    pixels[i].a = Mathf.Clamp01(pixels[i].a);
                }
                pngTexture.SetPixels(pixels);
                pngTexture.Apply(false, false);
                bytes = pngTexture.EncodeToPNG();
                DestroyImmediate(pngTexture);
            }

            File.WriteAllBytes(AssetPathToAbsolutePath(assetPath), bytes);
        }

        private void ApplyImporterSettings(Texture2D source, string outputPath, int sourceWidth, int sourceHeight)
        {
            TextureImporter importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
            if (importer == null) return;

            string sourcePath = AssetDatabase.GetAssetPath(source);
            TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = settings.contentMode == VFXTextureContentMode.ColorSRGB;
            importer.mipmapEnabled = settings.generateMipMaps;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = GetImporterMaxSize(Mathf.Max(sourceWidth, sourceHeight));

            if (settings.forceUncompressedImport)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            }

            if (settings.copyWrapModeFromSource && sourceImporter != null)
            {
                importer.wrapMode = sourceImporter.wrapMode;
            }

            switch (settings.outputFilterMode)
            {
                case VFXOutputFilterMode.CopyFromSource:
                    if (sourceImporter != null) importer.filterMode = sourceImporter.filterMode;
                    break;
                case VFXOutputFilterMode.Point:
                    importer.filterMode = FilterMode.Point;
                    break;
                case VFXOutputFilterMode.Bilinear:
                    importer.filterMode = FilterMode.Bilinear;
                    break;
                case VFXOutputFilterMode.Trilinear:
                    importer.filterMode = FilterMode.Trilinear;
                    break;
            }

            if (sourceImporter != null)
            {
                importer.anisoLevel = sourceImporter.anisoLevel;
            }

            importer.SaveAndReimport();
        }

        private static int GetImporterMaxSize(int dimension)
        {
            int[] sizes = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };
            for (int i = 0; i < sizes.Length; i++)
            {
                if (dimension <= sizes[i]) return sizes[i];
            }
            return 16384;
        }

        private void GeneratePreview()
        {
            DestroyPreviewTextures();
            if (previewSource == null) return;

            try
            {
                Texture2D readableSource = CreateReadableCopy(previewSource, IsLinearDataMode(settings), false);
                previewProcessedTexture = ProcessTexture(previewSource, settings);
                previewProcessedTexture.name = previewSource.name + "_preview_result";
                previewProcessedTexture.hideFlags = HideFlags.HideAndDontSave;

                previewSourceDisplayTexture = CreatePreviewDisplayTexture(readableSource, previewMode, previewSource.name + "_preview_source_view");
                previewResultDisplayTexture = CreatePreviewDisplayTexture(previewProcessedTexture, previewMode, previewSource.name + "_preview_result_view");

                DestroyImmediate(readableSource);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                lastStatus = "Preview failed: " + exception.Message;
            }

            Repaint();
        }

        private Texture2D CreatePreviewDisplayTexture(Texture2D source, VFXPreviewMode mode, string textureName)
        {
            Texture2D display = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
            display.name = textureName;
            display.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = source.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color color = pixels[i];
                switch (mode)
                {
                    case VFXPreviewMode.Red:
                        color = new Color(color.r, color.r, color.r, 1f);
                        break;
                    case VFXPreviewMode.Green:
                        color = new Color(color.g, color.g, color.g, 1f);
                        break;
                    case VFXPreviewMode.Blue:
                        color = new Color(color.b, color.b, color.b, 1f);
                        break;
                    case VFXPreviewMode.Alpha:
                        color = new Color(color.a, color.a, color.a, 1f);
                        break;
                    case VFXPreviewMode.Luminance:
                        float luma = color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
                        color = new Color(luma, luma, luma, 1f);
                        break;
                    case VFXPreviewMode.Color:
                    default:
                        color.a = 1f;
                        break;
                }
                pixels[i] = color;
            }

            display.SetPixels(pixels);
            display.Apply(false, false);
            return display;
        }

        private void RepaintPreviewIfNeeded()
        {
            if (autoPreview && previewSource != null)
                GeneratePreview();
            else
                Repaint();
        }

        private void DestroyPreviewTextures()
        {
            DestroyPreviewTexture(ref previewProcessedTexture);
            DestroyPreviewTexture(ref previewSourceDisplayTexture);
            DestroyPreviewTexture(ref previewResultDisplayTexture);
        }

        private static void DestroyPreviewTexture(ref Texture2D texture)
        {
            if (texture != null)
            {
                DestroyImmediate(texture);
                texture = null;
            }
        }

        private void TrySetPreviewToFirstValid()
        {
            if (previewSource != null) return;
            previewSource = GetFirstValidInputTexture();
            previewIndex = previewSource == null ? -1 : inputTextures.IndexOf(previewSource);
        }

        private Texture2D GetFirstValidInputTexture()
        {
            for (int i = 0; i < inputTextures.Count; i++)
            {
                if (inputTextures[i] != null) return inputTextures[i];
            }
            return null;
        }

        private void GoToPreviewTexture(int direction)
        {
            List<Texture2D> valid = inputTextures.FindAll(texture => texture != null);
            if (valid.Count == 0) return;

            int current = previewSource == null ? -1 : valid.IndexOf(previewSource);
            int next = current < 0 ? 0 : (current + direction + valid.Count) % valid.Count;
            previewSource = valid[next];
            previewIndex = inputTextures.IndexOf(previewSource);
            GeneratePreview();
        }

        private int CountValidInputs()
        {
            int count = 0;
            foreach (Texture2D texture in inputTextures)
            {
                if (texture != null) count++;
            }
            return count;
        }

        private static string GetTextureInfo(Texture2D texture)
        {
            if (texture == null) return string.Empty;
            string path = AssetDatabase.GetAssetPath(texture);
            return texture.name + " | " + texture.width + " x " + texture.height + " | " + path;
        }

        private void CreatePresetAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create VFX Texture Lab Preset",
                "VFXTextureLabPreset",
                "asset",
                "Choose where to save the preset asset.");

            if (string.IsNullOrEmpty(path)) return;

            VFXTextureLabPreset newPreset = CreateInstance<VFXTextureLabPreset>();
            newPreset.settings = DeepCopySettings(settings);
            AssetDatabase.CreateAsset(newPreset, path);
            AssetDatabase.SaveAssets();
            preset = newPreset;
        }

        private static VFXTextureLabSettings DeepCopySettings(VFXTextureLabSettings source)
        {
            VFXTextureLabSettings copy = new VFXTextureLabSettings();
            if (source == null) return copy;

            copy.overwriteInputTextures = source.overwriteInputTextures;
            copy.outputFolder = source.outputFolder;
            copy.outputSuffix = source.outputSuffix;
            copy.outputFormat = source.outputFormat;
            copy.contentMode = source.contentMode;
            copy.generateMipMaps = source.generateMipMaps;
            copy.forceUncompressedImport = source.forceUncompressedImport;
            copy.outputFilterMode = source.outputFilterMode;
            copy.copyWrapModeFromSource = source.copyWrapModeFromSource;

            copy.operations = new List<VFXTextureLabOperation>();
            if (source.operations != null)
            {
                foreach (VFXTextureLabOperation operation in source.operations)
                {
                    copy.operations.Add(DeepCopyOperation(operation));
                }
            }

            return copy;
        }

        private static VFXTextureLabOperation DeepCopyOperation(VFXTextureLabOperation source)
        {
            if (source == null) return new VFXTextureLabOperation();

            VFXTextureLabOperation copy = new VFXTextureLabOperation
            {
                enabled = source.enabled,
                foldout = source.foldout,
                customName = source.customName,
                type = source.type,
                sourceMode = source.sourceMode,
                constantSourceValue = source.constantSourceValue,
                affectR = source.affectR,
                affectG = source.affectG,
                affectB = source.affectB,
                affectA = source.affectA,
                rgbUseSingleSource = source.rgbUseSingleSource,
                invertSource = source.invertSource,
                invertResult = source.invertResult,
                clampResult01 = source.clampResult01,
                origin = source.origin,
                lowerOutputRange = source.lowerOutputRange,
                upperOutputRange = source.upperOutputRange,
                pushMode = source.pushMode,
                valuePushCurveDomain = source.valuePushCurveDomain,
                power = source.power,
                exponentialStrength = source.exponentialStrength,
                logisticSteepness = source.logisticSteepness,
                customLowerCurve = DeepCopyCurve(source.customLowerCurve),
                customUpperCurve = DeepCopyCurve(source.customUpperCurve),
                gradientMapMode = source.gradientMapMode,
                gradient = DeepCopyGradient(source.gradient),
                redCurve = DeepCopyCurve(source.redCurve),
                greenCurve = DeepCopyCurve(source.greenCurve),
                blueCurve = DeepCopyCurve(source.blueCurve),
                alphaCurve = DeepCopyCurve(source.alphaCurve),
                hueCurve = DeepCopyCurve(source.hueCurve),
                saturationCurve = DeepCopyCurve(source.saturationCurve),
                valueCurve = DeepCopyCurve(source.valueCurve),
                activeGradientCurveSlot = source.activeGradientCurveSlot,
                showSeparateCurveFields = source.showSeparateCurveFields,
                gradientWriteRGB = source.gradientWriteRGB,
                gradientWriteAlpha = source.gradientWriteAlpha,
                preserveOriginalAlpha = source.preserveOriginalAlpha,
                inputRange = source.inputRange,
                gamma = source.gamma,
                outputRange = source.outputRange,
                threshold = source.threshold,
                thresholdFeather = source.thresholdFeather,
                thresholdLowValue = source.thresholdLowValue,
                thresholdHighValue = source.thresholdHighValue,
                posterizeSteps = source.posterizeSteps,
                tintColor = source.tintColor,
                colorizeUseSourceAsIntensity = source.colorizeUseSourceAsIntensity,
                colorizeWriteAlpha = source.colorizeWriteAlpha,
                mathMode = source.mathMode,
                mathValue = source.mathValue,
                kernelRadius = source.kernelRadius,
                kernelIterations = source.kernelIterations
            };

            EnsureValidOperation(copy);
            return copy;
        }

        private static AnimationCurve DeepCopyCurve(AnimationCurve source)
        {
            if (source == null) return AnimationCurve.EaseInOut(0, 0, 1, 1);

            AnimationCurve copy = new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };

            return copy;
        }

        private static Gradient DeepCopyGradient(Gradient source)
        {
            if (source == null) return VFXTextureLabOperation.CreateDefaultGradient();

            Gradient copy = new Gradient();
            copy.SetKeys(source.colorKeys, source.alphaKeys);
            copy.mode = source.mode;
            return copy;
        }

        public static Texture2D ProcessTexture(Texture2D source, VFXTextureLabSettings settings)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.operations == null) settings.operations = new List<VFXTextureLabOperation>();

            bool highPrecision = settings.outputFormat == VFXOutputFormat.EXRFloat;
            bool linearData = IsLinearDataMode(settings);
            Texture2D readable = CreateReadableCopy(source, linearData, highPrecision);
            Color[] pixels = readable.GetPixels();
            int width = readable.width;
            int height = readable.height;

            for (int i = 0; i < settings.operations.Count; i++)
            {
                VFXTextureLabOperation operation = settings.operations[i];
                if (operation == null || !operation.enabled) continue;
                EnsureValidOperation(operation);
                ApplyOperation(ref pixels, width, height, operation);
            }

            TextureFormat outputTextureFormat = highPrecision ? TextureFormat.RGBAFloat : TextureFormat.RGBA32;
            Texture2D output = new Texture2D(width, height, outputTextureFormat, false, linearData);
            output.SetPixels(pixels);
            output.Apply(false, false);

            DestroyImmediate(readable);
            return output;
        }

        private static bool IsLinearDataMode(VFXTextureLabSettings settings)
        {
            return settings != null && settings.contentMode == VFXTextureContentMode.DataLinear;
        }

        private static Texture2D CreateReadableCopy(Texture2D source, bool linearData, bool highPrecision)
        {
            RenderTextureFormat desiredFormat = highPrecision && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat)
                ? RenderTextureFormat.ARGBFloat
                : RenderTextureFormat.ARGB32;

            RenderTextureReadWrite readWrite = linearData ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default;
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, desiredFormat, readWrite);
            RenderTexture previous = RenderTexture.active;

            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            TextureFormat readableFormat = desiredFormat == RenderTextureFormat.ARGBFloat ? TextureFormat.RGBAFloat : TextureFormat.RGBA32;
            Texture2D readable = new Texture2D(source.width, source.height, readableFormat, false, linearData);
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readable.Apply(false, false);

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return readable;
        }

        private static void ApplyOperation(ref Color[] pixels, int width, int height, VFXTextureLabOperation operation)
        {
            switch (operation.type)
            {
                case VFXTextureOperationType.AutoNormalize:
                    ApplyAutoNormalize(pixels, operation);
                    return;

                case VFXTextureOperationType.Blur:
                    pixels = ApplyKernelOperation(pixels, width, height, operation, KernelMode.Blur);
                    return;

                case VFXTextureOperationType.Dilate:
                    pixels = ApplyKernelOperation(pixels, width, height, operation, KernelMode.Dilate);
                    return;

                case VFXTextureOperationType.Erode:
                    pixels = ApplyKernelOperation(pixels, width, height, operation, KernelMode.Erode);
                    return;
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                Color original = pixels[i];
                Color color = original;

                switch (operation.type)
                {
                    case VFXTextureOperationType.ValuePush:
                        ApplyChannelOperation(ref color, original, operation, ApplyValuePushForChannel);
                        break;

                    case VFXTextureOperationType.GradientMap:
                        ApplyGradientMap(ref color, original, operation);
                        break;

                    case VFXTextureOperationType.Invert:
                        for (int channel = 0; channel < 4; channel++)
                        {
                            if (!operation.AffectsChannel(channel)) continue;
                            float value = 1f - GetChannel(original, channel);
                            if (operation.clampResult01) value = Mathf.Clamp01(value);
                            SetChannel(ref color, channel, value);
                        }
                        break;

                    case VFXTextureOperationType.Levels:
                        ApplyChannelOperation(ref color, original, operation, ApplyLevelsForChannel);
                        break;

                    case VFXTextureOperationType.Threshold:
                        ApplyChannelOperation(ref color, original, operation, ApplyThresholdForChannel);
                        break;

                    case VFXTextureOperationType.Posterize:
                        ApplyChannelOperation(ref color, original, operation, ApplyPosterizeForChannel);
                        break;

                    case VFXTextureOperationType.Colorize:
                        ApplyColorize(ref color, original, operation);
                        break;

                    case VFXTextureOperationType.ChannelPack:
                        ApplyChannelOperation(ref color, original, operation, ApplyChannelPackForChannel);
                        break;

                    case VFXTextureOperationType.Math:
                        for (int channel = 0; channel < 4; channel++)
                        {
                            if (!operation.AffectsChannel(channel)) continue;
                            float value = ApplyMath(GetChannel(original, channel), operation.mathMode, operation.mathValue);
                            if (operation.invertResult) value = 1f - value;
                            if (operation.clampResult01) value = Mathf.Clamp01(value);
                            SetChannel(ref color, channel, value);
                        }
                        break;
                }

                pixels[i] = color;
            }
        }

        private delegate float ChannelOperation(Color original, int channel, VFXTextureLabOperation operation);

        private static void ApplyChannelOperation(ref Color color, Color original, VFXTextureLabOperation operation, ChannelOperation channelOperation)
        {
            for (int channel = 0; channel < 4; channel++)
            {
                if (!operation.AffectsChannel(channel)) continue;
                float value = channelOperation(original, channel, operation);
                if (operation.invertResult) value = 1f - value;
                if (operation.clampResult01) value = Mathf.Clamp01(value);
                SetChannel(ref color, channel, value);
            }
        }

        private static float GetPreparedSourceValue(Color original, int destinationChannel, VFXTextureLabOperation operation)
        {
            VFXSourceMode sourceMode = operation.sourceMode;
            int sourceChannel = destinationChannel;

            if (operation.rgbUseSingleSource && destinationChannel < 3)
            {
                // The common artifact case: grayscale/noise textures are not perfectly equal in R/G/B after compression,
                // filtering, or color-space conversion. Processing each RGB channel separately through nonlinear remaps
                // amplifies tiny differences into magenta/green fringes. Use one luminance source for RGB instead.
                if (sourceMode == VFXSourceMode.CurrentChannel)
                {
                    sourceMode = VFXSourceMode.LuminanceRGB;
                }
                sourceChannel = 0;
            }

            float sourceValue = GetSourceValue(original, sourceMode, sourceChannel, operation.constantSourceValue);
            if (operation.invertSource) sourceValue = 1f - sourceValue;
            return sourceValue;
        }

        private static float ApplyValuePushForChannel(Color original, int channel, VFXTextureLabOperation operation)
        {
            float value = GetPreparedSourceValue(original, channel, operation);
            return PushValue(value, operation);
        }

        private static float ApplyLevelsForChannel(Color original, int channel, VFXTextureLabOperation operation)
        {
            float value = GetPreparedSourceValue(original, channel, operation);
            float t = SafeInverseLerp(operation.inputRange.x, operation.inputRange.y, value);
            t = Mathf.Clamp01(t);
            t = Mathf.Pow(t, 1f / Mathf.Max(0.001f, operation.gamma));
            return Mathf.Lerp(operation.outputRange.x, operation.outputRange.y, t);
        }

        private static float ApplyThresholdForChannel(Color original, int channel, VFXTextureLabOperation operation)
        {
            float value = GetPreparedSourceValue(original, channel, operation);
            if (operation.thresholdFeather <= 0.0001f)
                return value >= operation.threshold ? operation.thresholdHighValue : operation.thresholdLowValue;

            float halfFeather = operation.thresholdFeather * 0.5f;
            float t = SafeInverseLerp(operation.threshold - halfFeather, operation.threshold + halfFeather, value);
            t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            return Mathf.Lerp(operation.thresholdLowValue, operation.thresholdHighValue, t);
        }

        private static float ApplyPosterizeForChannel(Color original, int channel, VFXTextureLabOperation operation)
        {
            float value = GetPreparedSourceValue(original, channel, operation);
            int steps = Mathf.Max(2, operation.posterizeSteps);
            return Mathf.Round(value * (steps - 1)) / (steps - 1);
        }

        private static float ApplyChannelPackForChannel(Color original, int channel, VFXTextureLabOperation operation)
        {
            return GetPreparedSourceValue(original, channel, operation);
        }

        private static void ApplyGradientMap(ref Color color, Color original, VFXTextureLabOperation operation)
        {
            float t = Mathf.Clamp01(GetPreparedSourceValue(original, 0, operation));
            Color mapped = EvaluateGradientMapColor(operation, t);

            if (operation.gradientWriteRGB)
            {
                color.r = mapped.r;
                color.g = mapped.g;
                color.b = mapped.b;
            }

            if (operation.gradientWriteAlpha)
            {
                color.a = mapped.a;
            }
            else if (operation.preserveOriginalAlpha)
            {
                color.a = original.a;
            }

            if (operation.invertResult)
            {
                if (operation.gradientWriteRGB)
                {
                    color.r = 1f - color.r;
                    color.g = 1f - color.g;
                    color.b = 1f - color.b;
                }
                if (operation.gradientWriteAlpha) color.a = 1f - color.a;
            }

            if (operation.clampResult01)
            {
                color.r = Mathf.Clamp01(color.r);
                color.g = Mathf.Clamp01(color.g);
                color.b = Mathf.Clamp01(color.b);
                color.a = Mathf.Clamp01(color.a);
            }
        }

        private static Color EvaluateGradientMapColor(VFXTextureLabOperation operation, float t)
        {
            t = Mathf.Clamp01(t);
            switch (operation.gradientMapMode)
            {
                case VFXGradientMapMode.RGBACurves:
                    return new Color(
                        EvaluateCurve01(operation.redCurve, t),
                        EvaluateCurve01(operation.greenCurve, t),
                        EvaluateCurve01(operation.blueCurve, t),
                        EvaluateCurve01(operation.alphaCurve, t));

                case VFXGradientMapMode.HSVCurves:
                    float hue = Mathf.Repeat(EvaluateCurve01(operation.hueCurve, t), 1f);
                    float saturation = Mathf.Clamp01(EvaluateCurve01(operation.saturationCurve, t));
                    float value = Mathf.Clamp01(EvaluateCurve01(operation.valueCurve, t));
                    Color hsvColor = Color.HSVToRGB(hue, saturation, value);
                    hsvColor.a = Mathf.Clamp01(EvaluateCurve01(operation.alphaCurve, t));
                    return hsvColor;

                case VFXGradientMapMode.UnityGradient:
                default:
                    return operation.gradient == null ? Color.white : operation.gradient.Evaluate(t);
            }
        }

        private static float EvaluateCurve01(AnimationCurve curve, float t)
        {
            return curve == null ? t : curve.Evaluate(t);
        }

        private static void ApplyColorize(ref Color color, Color original, VFXTextureLabOperation operation)
        {
            float intensity = operation.colorizeUseSourceAsIntensity ? GetPreparedSourceValue(original, 0, operation) : 1f;
            Color result = operation.tintColor * intensity;

            if (operation.affectR) color.r = result.r;
            if (operation.affectG) color.g = result.g;
            if (operation.affectB) color.b = result.b;
            if (operation.colorizeWriteAlpha && operation.affectA) color.a = result.a;

            if (operation.invertResult)
            {
                if (operation.affectR) color.r = 1f - color.r;
                if (operation.affectG) color.g = 1f - color.g;
                if (operation.affectB) color.b = 1f - color.b;
                if (operation.colorizeWriteAlpha && operation.affectA) color.a = 1f - color.a;
            }

            if (operation.clampResult01)
            {
                color.r = Mathf.Clamp01(color.r);
                color.g = Mathf.Clamp01(color.g);
                color.b = Mathf.Clamp01(color.b);
                color.a = Mathf.Clamp01(color.a);
            }
        }

        private static void ApplyAutoNormalize(Color[] pixels, VFXTextureLabOperation operation)
        {
            float[] minValues = { float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity };
            float[] maxValues = { float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity };

            for (int i = 0; i < pixels.Length; i++)
            {
                for (int channel = 0; channel < 4; channel++)
                {
                    if (!operation.AffectsChannel(channel)) continue;
                    float value = GetChannel(pixels[i], channel);
                    if (value < minValues[channel]) minValues[channel] = value;
                    if (value > maxValues[channel]) maxValues[channel] = value;
                }
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                Color color = pixels[i];
                for (int channel = 0; channel < 4; channel++)
                {
                    if (!operation.AffectsChannel(channel)) continue;
                    float value = GetChannel(color, channel);
                    value = SafeInverseLerp(minValues[channel], maxValues[channel], value);
                    if (operation.invertResult) value = 1f - value;
                    if (operation.clampResult01) value = Mathf.Clamp01(value);
                    SetChannel(ref color, channel, value);
                }
                pixels[i] = color;
            }
        }

        private enum KernelMode
        {
            Blur,
            Dilate,
            Erode
        }

        private static Color[] ApplyKernelOperation(Color[] sourcePixels, int width, int height, VFXTextureLabOperation operation, KernelMode mode)
        {
            Color[] current = sourcePixels;
            int radius = Mathf.Max(1, operation.kernelRadius);
            int iterations = Mathf.Max(1, operation.kernelIterations);

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                Color[] output = new Color[current.Length];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        Color result = current[index];

                        for (int channel = 0; channel < 4; channel++)
                        {
                            if (!operation.AffectsChannel(channel)) continue;

                            float value;
                            if (mode == KernelMode.Blur)
                            {
                                float sum = 0f;
                                int count = 0;
                                for (int oy = -radius; oy <= radius; oy++)
                                {
                                    int sy = Mathf.Clamp(y + oy, 0, height - 1);
                                    for (int ox = -radius; ox <= radius; ox++)
                                    {
                                        int sx = Mathf.Clamp(x + ox, 0, width - 1);
                                        sum += GetChannel(current[sy * width + sx], channel);
                                        count++;
                                    }
                                }
                                value = sum / Mathf.Max(1, count);
                            }
                            else if (mode == KernelMode.Dilate)
                            {
                                value = float.NegativeInfinity;
                                for (int oy = -radius; oy <= radius; oy++)
                                {
                                    int sy = Mathf.Clamp(y + oy, 0, height - 1);
                                    for (int ox = -radius; ox <= radius; ox++)
                                    {
                                        int sx = Mathf.Clamp(x + ox, 0, width - 1);
                                        value = Mathf.Max(value, GetChannel(current[sy * width + sx], channel));
                                    }
                                }
                            }
                            else
                            {
                                value = float.PositiveInfinity;
                                for (int oy = -radius; oy <= radius; oy++)
                                {
                                    int sy = Mathf.Clamp(y + oy, 0, height - 1);
                                    for (int ox = -radius; ox <= radius; ox++)
                                    {
                                        int sx = Mathf.Clamp(x + ox, 0, width - 1);
                                        value = Mathf.Min(value, GetChannel(current[sy * width + sx], channel));
                                    }
                                }
                            }

                            if (operation.clampResult01) value = Mathf.Clamp01(value);
                            SetChannel(ref result, channel, value);
                        }

                        output[index] = result;
                    }
                }

                current = output;
            }

            return current;
        }

        public static float PushValue(float value, VFXTextureLabOperation operation)
        {
            value = Mathf.Clamp01(value);
            float origin = Mathf.Clamp01(operation.origin);

            // One curve over the whole texture. Origin becomes the curve center/pivot.
            // Output is fixed as 0-0.5 below the curve midpoint and 0.5-1 above it.
            float pivotedInput;
            if (value < origin)
            {
                pivotedInput = Mathf.Approximately(origin, 0f) ? 0f : 0.5f * SafeInverseLerp(0f, origin, value);
            }
            else
            {
                pivotedInput = Mathf.Approximately(origin, 1f) ? 1f : 0.5f + 0.5f * SafeInverseLerp(origin, 1f, value);
            }

            float curved = ApplyPushCurve(pivotedInput, operation, true);
            return Mathf.Clamp01(curved);
        }

        private static float MapCurve01ToSplitOutput(float curveValue, VFXTextureLabOperation operation)
        {
            curveValue = Mathf.Clamp01(curveValue);
            if (curveValue < 0.5f)
            {
                return Mathf.Lerp(operation.lowerOutputRange.x, operation.lowerOutputRange.y, curveValue / 0.5f);
            }

            return Mathf.Lerp(operation.upperOutputRange.x, operation.upperOutputRange.y, (curveValue - 0.5f) / 0.5f);
        }

        private static float ApplyPushCurve(float t, VFXTextureLabOperation operation, bool upperSide)
        {
            t = Mathf.Clamp01(t);
            switch (operation.pushMode)
            {
                case VFXPushMode.Linear:
                    return t;

                case VFXPushMode.Smoothstep:
                    return t * t * (3f - 2f * t);

                case VFXPushMode.Smootherstep:
                    return t * t * t * (t * (6f * t - 15f) + 10f);

                case VFXPushMode.LogisticSCurve:
                    return NormalizedLogistic(t, Mathf.Max(0.01f, operation.logisticSteepness));

                case VFXPushMode.PowerEaseIn:
                    return Mathf.Pow(t, Mathf.Max(0.01f, operation.power));

                case VFXPushMode.PowerEaseOut:
                    return 1f - Mathf.Pow(1f - t, Mathf.Max(0.01f, operation.power));

                case VFXPushMode.Exponential:
                    return ExponentialCurve(t, operation.exponentialStrength);

                case VFXPushMode.HardStep:
                    return t < 0.5f ? 0f : 1f;

                case VFXPushMode.CustomCurve:
                    AnimationCurve curve = upperSide ? operation.customUpperCurve : operation.customLowerCurve;
                    return curve == null ? t : Mathf.Clamp01(curve.Evaluate(t));

                default:
                    return t;
            }
        }

        private static float ApplyMath(float value, VFXMathMode mode, float amount)
        {
            switch (mode)
            {
                case VFXMathMode.Add: return value + amount;
                case VFXMathMode.Subtract: return value - amount;
                case VFXMathMode.Multiply: return value * amount;
                case VFXMathMode.Divide: return Mathf.Abs(amount) < 0.000001f ? value : value / amount;
                case VFXMathMode.Power: return Mathf.Pow(Mathf.Max(0f, value), amount);
                case VFXMathMode.Min: return Mathf.Min(value, amount);
                case VFXMathMode.Max: return Mathf.Max(value, amount);
                case VFXMathMode.DifferenceFromConstant: return Mathf.Abs(value - amount);
                default: return value;
            }
        }

        private static float GetSourceValue(Color color, VFXSourceMode mode, int destinationChannel, float constant)
        {
            switch (mode)
            {
                case VFXSourceMode.CurrentChannel: return GetChannel(color, destinationChannel);
                case VFXSourceMode.Red: return color.r;
                case VFXSourceMode.Green: return color.g;
                case VFXSourceMode.Blue: return color.b;
                case VFXSourceMode.Alpha: return color.a;
                case VFXSourceMode.LuminanceRGB: return color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
                case VFXSourceMode.AverageRGB: return (color.r + color.g + color.b) / 3f;
                case VFXSourceMode.MaxRGB: return Mathf.Max(color.r, Mathf.Max(color.g, color.b));
                case VFXSourceMode.MinRGB: return Mathf.Min(color.r, Mathf.Min(color.g, color.b));
                case VFXSourceMode.Constant: return constant;
                default: return GetChannel(color, destinationChannel);
            }
        }

        private static float GetChannel(Color color, int channel)
        {
            switch (channel)
            {
                case 0: return color.r;
                case 1: return color.g;
                case 2: return color.b;
                case 3: return color.a;
                default: return 0f;
            }
        }

        private static void SetChannel(ref Color color, int channel, float value)
        {
            switch (channel)
            {
                case 0:
                    color.r = value;
                    break;
                case 1:
                    color.g = value;
                    break;
                case 2:
                    color.b = value;
                    break;
                case 3:
                    color.a = value;
                    break;
            }
        }

        private static float SafeInverseLerp(float a, float b, float value)
        {
            if (Mathf.Abs(b - a) < 0.000001f) return 0f;
            return (value - a) / (b - a);
        }

        private static float NormalizedLogistic(float t, float steepness)
        {
            float a = 1f / (1f + Mathf.Exp(steepness * 0.5f));
            float b = 1f / (1f + Mathf.Exp(-steepness * 0.5f));
            float y = 1f / (1f + Mathf.Exp(-steepness * (t - 0.5f)));
            return SafeInverseLerp(a, b, y);
        }

        private static float ExponentialCurve(float t, float strength)
        {
            if (Mathf.Abs(strength) < 0.0001f) return t;
            float numerator = Mathf.Exp(strength * t) - 1f;
            float denominator = Mathf.Exp(strength) - 1f;
            return Mathf.Abs(denominator) < 0.000001f ? t : numerator / denominator;
        }
    }
}
#endif
