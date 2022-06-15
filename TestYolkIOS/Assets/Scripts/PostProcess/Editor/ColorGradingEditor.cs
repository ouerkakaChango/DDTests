using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CenturyGame.PostProcess;

namespace CenturyGame.PostProcessEditor
{
    [PostProcessEffectEditor(typeof(FPColorGrading))]
    public class PostProcessColorGradingEditor : PostProcessEffectBaseEditor
    {
        private static class Styling
        {
            public static readonly int k_ThumbHash;
            public static readonly GUIStyle wheelThumb;
            public static readonly Vector2 wheelThumbSize;
            public static readonly GUIStyle wheelLabel;
            public static readonly GUIStyle preLabel;

            static Styling()
            {
                k_ThumbHash = "colorWheelThumb".GetHashCode();
                wheelThumb = new GUIStyle("ColorPicker2DThumb");
                wheelThumbSize = new Vector2(
                    !Mathf.Approximately(wheelThumb.fixedWidth, 0f) ? wheelThumb.fixedWidth : wheelThumb.padding.horizontal,
                    !Mathf.Approximately(wheelThumb.fixedHeight, 0f) ? wheelThumb.fixedHeight : wheelThumb.padding.vertical
                );
                wheelLabel = new GUIStyle(EditorStyles.miniLabel);
                preLabel = new GUIStyle("ShurikenLabel");
            }
        }

        private static int currentChannel = 0;

        static GUIContent[] s_Curves =
        {
            new GUIContent("Master"),
            new GUIContent("Red"),
            new GUIContent("Green"),
            new GUIContent("Blue"),
            new GUIContent("Hue Vs Hue"),
            new GUIContent("Hue Vs Sat"),
            new GUIContent("Sat Vs Sat"),
            new GUIContent("Lum Vs Sat")
        };

        private static Material s_Material;
        private static Material s_MaterialGrid;

        struct CurveSettings
        {
            public Rect bounds;
            public RectOffset padding;
            public Color selectionColor;
            public float curvePickingDistance;
            public float keyTimeClampingDistance;

            public static CurveSettings defaultSettings
            {
                get
                {
                    return new CurveSettings
                    {
                        bounds = new Rect(0f, 0f, 1f, 1f),
                        padding = new RectOffset(10, 10, 10, 10),
                        selectionColor = Color.yellow,
                        curvePickingDistance = 6f,
                        keyTimeClampingDistance = 1e-4f
                    };
                }
            }
        }

        Vector2 liftCursorPos;
        Vector2 gammaCursorPos;
        Vector2 gainCursorPos;
        int currentCurve;
        CurveEditor m_CurveEditor;
        Dictionary<SerializedProperty, Color> m_CurveDict;
        Editor m_Inspector;
        const int k_CustomToneCurveResolution = 48;
        const float k_CustomToneCurveRangeY = 1.025f;
        readonly Vector3[] m_RectVertices = new Vector3[4];
        readonly Vector3[] m_LineVertices = new Vector3[2];
        readonly Vector3[] m_CurveVertices = new Vector3[k_CustomToneCurveResolution];
        Rect m_CustomToneCurveRect;
        readonly HableCurve m_HableCurve = new HableCurve();

        SerializedProperty toneCurveToeStrength;
        SerializedProperty toneCurveToeLength;
        SerializedProperty toneCurveShoulderStrength;
        SerializedProperty toneCurveShoulderLength;
        SerializedProperty toneCurveShoulderAngle;
        SerializedProperty toneCurveGamma;

        SerializedProperty masterCurve;
        SerializedProperty redCurve;
        SerializedProperty greenCurve;
        SerializedProperty blueCurve;
        SerializedProperty hueVsHueCurve;
        SerializedProperty hueVsSatCurve;
        SerializedProperty satVsSatCurve;
        SerializedProperty lumVsSatCurve;

        public override void OnEnable()
        {
            m_CurveEditor = new CurveEditor();

            var obj = SerializedObjectHandle;

            toneCurveToeStrength = obj.FindProperty("toneCurveToeStrength");
            toneCurveToeLength = obj.FindProperty("toneCurveToeLength");
            toneCurveShoulderStrength = obj.FindProperty("toneCurveShoulderStrength");
            toneCurveShoulderLength = obj.FindProperty("toneCurveShoulderLength");
            toneCurveShoulderAngle = obj.FindProperty("toneCurveShoulderAngle");
            toneCurveGamma = obj.FindProperty("toneCurveGamma");

            masterCurve = obj.FindProperty("masterCurve");
            redCurve = obj.FindProperty("redCurve");
            greenCurve = obj.FindProperty("greenCurve");
            blueCurve = obj.FindProperty("blueCurve");
            hueVsHueCurve = obj.FindProperty("hueVsHueCurve");
            hueVsSatCurve = obj.FindProperty("hueVsSatCurve");
            satVsSatCurve = obj.FindProperty("satVsSatCurve");
            lumVsSatCurve = obj.FindProperty("lumVsSatCurve");
        }

        public override void OnInspectorGUI()
        {
            SerializedObjectHandle.Update();

            var gradingMode = SerializedObjectHandle.FindProperty("gradingMode");
            var tonemapper = SerializedObjectHandle.FindProperty("tonemapper");
            var ldrLut = SerializedObjectHandle.FindProperty("ldrLut");
            var ldrLutContribution = SerializedObjectHandle.FindProperty("ldrLutContribution");
            var temprature = SerializedObjectHandle.FindProperty("temprature");
            var tint = SerializedObjectHandle.FindProperty("tint");
            var colorFilter = SerializedObjectHandle.FindProperty("colorFilter");
            var hue = SerializedObjectHandle.FindProperty("hue");
            var saturation = SerializedObjectHandle.FindProperty("saturation");
            var brightness = SerializedObjectHandle.FindProperty("brightness");
            var contrast = SerializedObjectHandle.FindProperty("contrast");
            var mixerRedOutRedIn = SerializedObjectHandle.FindProperty("mixerRedOutRedIn");
            var mixerRedOutGreenIn = SerializedObjectHandle.FindProperty("mixerRedOutGreenIn");
            var mixerRedOutBlueIn = SerializedObjectHandle.FindProperty("mixerRedOutBlueIn");
            var mixerGreenOutRedIn = SerializedObjectHandle.FindProperty("mixerGreenOutRedIn");
            var mixerGreenOutGreenIn = SerializedObjectHandle.FindProperty("mixerGreenOutGreenIn");
            var mixerGreenOutBlueIn = SerializedObjectHandle.FindProperty("mixerGreenOutBlueIn");
            var mixerBlueOutRedIn = SerializedObjectHandle.FindProperty("mixerBlueOutRedIn");
            var mixerBlueOutGreenIn = SerializedObjectHandle.FindProperty("mixerBlueOutGreenIn");
            var mixerBlueOutBlueIn = SerializedObjectHandle.FindProperty("mixerBlueOutBlueIn");
            var lift = SerializedObjectHandle.FindProperty("lift");
            var gamma = SerializedObjectHandle.FindProperty("gamma");
            var gain = SerializedObjectHandle.FindProperty("gain");

            PropertyDrawEnum(gradingMode, (int)FPColorGrading.GradingMode.HDR);

            if (gradingMode.enumValueIndex == (int)FPColorGrading.GradingMode.LDR)
            {
                PropertyDraw(ldrLut, null);
                PropertyDraw(ldrLutContribution, 1.0f);
            }

            if (gradingMode.enumValueIndex == (int)FPColorGrading.GradingMode.HDR)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Tonemapping");
                PropertyDrawEnum(tonemapper, (int)FPColorGrading.Tonemapper.None);

                if (tonemapper.enumValueIndex == (int)FPColorGrading.Tonemapper.Custom)
                {
                    DrawCustomToneCurve();
                    PropertyDraw(toneCurveToeStrength, 0.0f);
                    PropertyDraw(toneCurveToeLength, 0.5f);
                    PropertyDraw(toneCurveShoulderStrength, 0.0f);
                    PropertyDraw(toneCurveShoulderLength, 0.5f);
                    PropertyDraw(toneCurveShoulderAngle, 0.0f);
                    PropertyDraw(toneCurveGamma, 1.0f);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.Label("White Balance");
            FPEditorGUIUtility.Separator();
            PropertyDraw(temprature, 0.0f);
            PropertyDraw(tint, 0.0f);

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.Label("Tone");
            FPEditorGUIUtility.Separator();
            PropertyDraw(colorFilter, Color.white);
            PropertyDraw(hue, 0.0f);
            PropertyDraw(saturation, 0.0f);
            PropertyDraw(brightness, 0.0f);
            PropertyDraw(contrast, 0.0f);

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.Label("Channel Mixer");
            FPEditorGUIUtility.Separator();
            GUILayout.BeginHorizontal();

            if (GUILayout.Toggle(currentChannel == 0, new GUIContent("Red", "Red output channel"), EditorStyles.miniButtonLeft)) currentChannel = 0;
            if (GUILayout.Toggle(currentChannel == 1, new GUIContent("Green", "Green output channel"), EditorStyles.miniButtonLeft)) currentChannel = 1;
            if (GUILayout.Toggle(currentChannel == 2, new GUIContent("Blue", "Blue output channel"), EditorStyles.miniButtonLeft)) currentChannel = 2;

            GUILayout.EndHorizontal();

            if (currentChannel == 0)
            {
                PropertyDraw(mixerRedOutRedIn, 100.0f);
                PropertyDraw(mixerRedOutGreenIn, 0.0f);
                PropertyDraw(mixerRedOutBlueIn, 0.0f);
            }

            if (currentChannel == 1)
            {
                PropertyDraw(mixerGreenOutRedIn, 0.0f);
                PropertyDraw(mixerGreenOutGreenIn, 100.0f);
                PropertyDraw(mixerGreenOutBlueIn, 0.0f);
            }

            if (currentChannel == 2)
            {
                PropertyDraw(mixerBlueOutRedIn, 0.0f);
                PropertyDraw(mixerBlueOutGreenIn, 0.0f);
                PropertyDraw(mixerBlueOutBlueIn, 100.0f);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.Label("Trackballs");
            FPEditorGUIUtility.Separator();

            GUILayout.BeginHorizontal();

            DoTrackball(lift, ref liftCursorPos);
            GUILayout.Space(4f);
            DoTrackball(gamma, ref gammaCursorPos);
            GUILayout.Space(4f);
            DoTrackball(gain, ref gainCursorPos);

            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.Label("Grading Curves");
            FPEditorGUIUtility.Separator();

            DoCurvesGUI(SerializedObjectHandle, false);

            SerializedObjectHandle.ApplyModifiedProperties();
        }

        void SetupCurve(SerializedProperty prop, Color color, uint minPointCount, bool loop)
        {
            var state = CurveEditor.CurveState.defaultState;
            state.color = color;
            state.visible = false;
            state.minPointCount = minPointCount;
            state.onlyShowHandlesOnSelection = true;
            state.zeroKeyConstantValue = 0.5f;
            state.loopInBounds = loop;
            m_CurveEditor.Add(prop, state);
        }

        void SetCurveVisible(SerializedProperty rawProp)
        {
            var state = m_CurveEditor.GetCurveState(rawProp);
            state.visible = true;
            state.editable = true;
            m_CurveEditor.SetCurveState(rawProp, state);
        }

        void PropertyDraw(SerializedProperty property, float defaultValue)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property);
            if (GUILayout.Button("R", GUILayout.ExpandWidth(false)))
            {
                property.floatValue = defaultValue;
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();
        }

        void PropertyDraw(SerializedProperty property, Color defaultValue)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property);
            if (GUILayout.Button("R", GUILayout.ExpandWidth(false)))
            {
                property.colorValue = defaultValue;
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();
        }

        void PropertyDraw(SerializedProperty property, UnityEngine.Object defaultValue)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property);
            if (GUILayout.Button("R", GUILayout.ExpandWidth(false)))
            {
                property.objectReferenceValue = defaultValue;
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();
        }

        void PropertyDrawEnum(SerializedProperty property, int defaultValue)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property);
            if (GUILayout.Button("R", GUILayout.ExpandWidth(false)))
            {
                property.enumValueIndex = defaultValue;
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();
        }

        void DrawCustomToneCurve()
        {
            EditorGUILayout.Space();

            // Reserve GUI space
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);
                m_CustomToneCurveRect = GUILayoutUtility.GetRect(128, 80);
            }

            if (Event.current.type != EventType.Repaint)
                return;

            // Prepare curve data
            float toeStrength = toneCurveToeStrength.floatValue;
            float toeLength = toneCurveToeLength.floatValue;
            float shoulderStrength = toneCurveShoulderStrength.floatValue;
            float shoulderLength = toneCurveShoulderLength.floatValue;
            float shoulderAngle = toneCurveShoulderAngle.floatValue;
            float gamma = toneCurveGamma.floatValue;
            m_HableCurve.Init(
                toeStrength,
                toeLength,
                shoulderStrength,
                shoulderLength,
                shoulderAngle,
                gamma
            );

            float endPoint = m_HableCurve.whitePoint;

            // Background
            m_RectVertices[0] = PointInRect(0f, 0f, endPoint);
            m_RectVertices[1] = PointInRect(endPoint, 0f, endPoint);
            m_RectVertices[2] = PointInRect(endPoint, k_CustomToneCurveRangeY, endPoint);
            m_RectVertices[3] = PointInRect(0f, k_CustomToneCurveRangeY, endPoint);
            Handles.DrawSolidRectangleWithOutline(m_RectVertices, Color.white * 0.1f, Color.white * 0.4f);

            // Vertical guides
            if (endPoint < m_CustomToneCurveRect.width / 3)
            {
                int steps = Mathf.CeilToInt(endPoint);
                for (var i = 1; i < steps; i++)
                    DrawLine(i, 0, i, k_CustomToneCurveRangeY, 0.4f, endPoint);
            }

            // Label
            Handles.Label(m_CustomToneCurveRect.position + Vector2.right, "Custom Tone Curve", EditorStyles.miniLabel);

            // Draw the acual curve
            var vcount = 0;
            while (vcount < k_CustomToneCurveResolution)
            {
                float x = endPoint * vcount / (k_CustomToneCurveResolution - 1);
                float y = m_HableCurve.Eval(x);

                if (y < k_CustomToneCurveRangeY)
                {
                    m_CurveVertices[vcount++] = PointInRect(x, y, endPoint);
                }
                else
                {
                    if (vcount > 1)
                    {
                        // Extend the last segment to the top edge of the rect.
                        var v1 = m_CurveVertices[vcount - 2];
                        var v2 = m_CurveVertices[vcount - 1];
                        var clip = (m_CustomToneCurveRect.y - v1.y) / (v2.y - v1.y);
                        m_CurveVertices[vcount - 1] = v1 + (v2 - v1) * clip;
                    }
                    break;
                }
            }

            if (vcount > 1)
            {
                Handles.color = Color.white * 0.9f;
                Handles.DrawAAPolyLine(2f, vcount, m_CurveVertices);
            }
        }

        void DrawLine(float x1, float y1, float x2, float y2, float grayscale, float rangeX)
        {
            m_LineVertices[0] = PointInRect(x1, y1, rangeX);
            m_LineVertices[1] = PointInRect(x2, y2, rangeX);
            Handles.color = Color.white * grayscale;
            Handles.DrawAAPolyLine(2f, m_LineVertices);
        }

        Vector3 PointInRect(float x, float y, float rangeX)
        {
            x = Mathf.Lerp(m_CustomToneCurveRect.x, m_CustomToneCurveRect.xMax, x / rangeX);
            y = Mathf.Lerp(m_CustomToneCurveRect.yMax, m_CustomToneCurveRect.y, y / k_CustomToneCurveRangeY);
            return new Vector3(x, y, 0);
        }

        private void DoTrackball(SerializedProperty property, ref Vector2 cursorPos)
        {
            var value = property.vector4Value;

            GUILayout.BeginVertical();

            var wheelRect = GUILayoutUtility.GetAspectRect(1f);
            float size = wheelRect.width;
            float hsize = size / 2f;
            float radius = 0.38f * size;

            Vector3 hsv;
            Color.RGBToHSV(value, out hsv.x, out hsv.y, out hsv.z);
            float offset = value.w;

            // Thumb
            var thumbPos = Vector2.zero;
            float theta = hsv.x * (Mathf.PI * 2f);
            thumbPos.x = Mathf.Cos(theta + (Mathf.PI / 2f));
            thumbPos.y = Mathf.Sin(theta - (Mathf.PI / 2f));
            thumbPos *= hsv.y * radius;

            // Draw the wheel
            if (Event.current.type == EventType.Repaint)
            {
                // Retina support
                float scale = EditorGUIUtility.pixelsPerPoint;

                if (s_Material == null)
                    s_Material = new Material(Shader.Find("Hidden/PostProcessing/Editor/Trackball")) { hideFlags = HideFlags.HideAndDontSave };

                // Wheel texture
#if UNITY_2018_1_OR_NEWER
                const RenderTextureReadWrite kReadWrite = RenderTextureReadWrite.sRGB;
#else
                const RenderTextureReadWrite kReadWrite = RenderTextureReadWrite.Linear;
#endif

                var oldRT = RenderTexture.active;
                var rt = RenderTexture.GetTemporary((int)(size * scale), (int)(size * scale), 0, RenderTextureFormat.ARGB32, kReadWrite);
                s_Material.SetFloat("_Offset", offset);
                s_Material.SetFloat("_DisabledState", 1);
                s_Material.SetVector("_Resolution", new Vector2(size * scale, size * scale / 2f));
                Graphics.Blit(null, rt, s_Material, EditorGUIUtility.isProSkin ? 0 : 1);
                RenderTexture.active = oldRT;

                GUI.DrawTexture(wheelRect, rt);
                RenderTexture.ReleaseTemporary(rt);

                var thumbSize = Styling.wheelThumbSize;
                var thumbSizeH = thumbSize / 2f;
                Styling.wheelThumb.Draw(new Rect(wheelRect.x + hsize + thumbPos.x - thumbSizeH.x, wheelRect.y + hsize + thumbPos.y - thumbSizeH.y, thumbSize.x, thumbSize.y), false, false, false, false);
            }

            // Input
            bool reset = false;
            var bounds = wheelRect;
            bounds.x += hsize - radius;
            bounds.y += hsize - radius;
            bounds.width = bounds.height = radius * 2f;
            hsv = GetInput(bounds, hsv, thumbPos, radius, ref cursorPos, ref reset);
            value = Color.HSVToRGB(hsv.x, hsv.y, 1f);
            value.w = offset;

            // Offset
            var sliderRect = GUILayoutUtility.GetRect(1f, 17f);
            float padding = sliderRect.width * 0.05f; // 5% padding
            sliderRect.xMin += padding;
            sliderRect.xMax -= padding;
            value.w = GUI.HorizontalSlider(sliderRect, value.w, -1f, 1f);

            //if (attr.mode == TrackballAttribute.Mode.None)
            //    return;



            var areaRect = GUILayoutUtility.GetRect(1f, 17f);
            var labelSize = Styling.wheelLabel.CalcSize(new GUIContent(property.displayName));
            var labelRect = new Rect(areaRect.x + areaRect.width / 2 - labelSize.x / 2, areaRect.y, labelSize.x, labelSize.y);
            var rRect = new Rect(labelRect.xMax, labelRect.yMin, labelRect.height + 4, labelRect.height);
            GUI.Label(labelRect, property.displayName, Styling.wheelLabel);
            if (GUI.Button(rRect, "R", EditorStyles.miniButton))
                reset = true;

            GUILayout.EndVertical();

            if (reset)
                value = Vector4.zero;
            property.vector4Value = value;
        }

        static Vector3 GetInput(Rect bounds, Vector3 hsv, Vector2 thumbPos, float radius, ref Vector2 cursorPos, ref bool reset)
        {
            var e = Event.current;
            var id = GUIUtility.GetControlID(Styling.k_ThumbHash, FocusType.Passive, bounds);
            var mousePos = e.mousePosition;

            if (e.type == EventType.MouseDown && GUIUtility.hotControl == 0 && bounds.Contains(mousePos))
            {
                if (e.button == 0)
                {
                    var center = new Vector2(bounds.x + radius, bounds.y + radius);
                    float dist = Vector2.Distance(center, mousePos);

                    if (dist <= radius)
                    {
                        e.Use();
                        cursorPos = new Vector2(thumbPos.x + radius, thumbPos.y + radius);
                        GUIUtility.hotControl = id;
                        GUI.changed = true;
                    }
                }
                else if (e.button == 1)
                {
                    e.Use();
                    GUI.changed = true;
                    reset = true;
                }
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && GUIUtility.hotControl == id)
            {
                e.Use();
                GUI.changed = true;
                cursorPos += e.delta * 0.2f;
                GetWheelHueSaturation(cursorPos.x, cursorPos.y, radius, out hsv.x, out hsv.y);
            }
            else if (e.rawType == EventType.MouseUp && e.button == 0 && GUIUtility.hotControl == id)
            {
                e.Use();
                GUIUtility.hotControl = 0;
            }

            return hsv;
        }

        static void GetWheelHueSaturation(float x, float y, float radius, out float hue, out float saturation)
        {
            float dx = (x - radius) / radius;
            float dy = (y - radius) / radius;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            hue = Mathf.Atan2(dx, -dy);
            hue = 1f - ((hue > 0) ? hue : (Mathf.PI * 2f) + hue) / (Mathf.PI * 2f);
            saturation = Mathf.Clamp01(d);
        }

        void DoCurvesGUI(SerializedObject obj, bool hdr)
        {
            int curveEditingId = 0;

            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                curveEditingId = DoCurveSelectionPopup(currentCurve, hdr);
                curveEditingId = Mathf.Clamp(curveEditingId, hdr ? 4 : 0, 7);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
                {
                    switch (curveEditingId)
                    {
                        case 0:
                            masterCurve.animationCurveValue = AnimationCurve.Linear(0, 0, 1, 1);
                            break;
                        case 1:
                            redCurve.animationCurveValue = AnimationCurve.Linear(0, 0, 1, 1);
                            break;
                        case 2:
                            greenCurve.animationCurveValue = AnimationCurve.Linear(0, 0, 1, 1);
                            break;
                        case 3:
                            blueCurve.animationCurveValue = AnimationCurve.Linear(0, 0, 1, 1);
                            break;
                        case 4:
                            hueVsHueCurve.animationCurveValue = new AnimationCurve();
                            break;
                        case 5:
                            hueVsSatCurve.animationCurveValue = new AnimationCurve();
                            break;
                        case 6:
                            satVsSatCurve.animationCurveValue = new AnimationCurve();
                            break;
                        case 7:
                            lumVsSatCurve.animationCurveValue = new AnimationCurve();
                            break;
                    }
                }
            }

            var settings = CurveSettings.defaultSettings;
            var rect = GUILayoutUtility.GetAspectRect(2f);
            var innerRect = settings.padding.Remove(rect);

            if (Event.current.type == EventType.Repaint)
            {
                // Background
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

                if (curveEditingId == 4 || curveEditingId == 5)
                    DrawBackgroundTexture(innerRect, 0);
                else if (curveEditingId == 6 || curveEditingId == 7)
                    DrawBackgroundTexture(innerRect, 1);

                // Bounds
                Handles.color = Color.white * (GUI.enabled ? 1f : 0.5f);
                Handles.DrawSolidRectangleWithOutline(innerRect, Color.clear, new Color(0.8f, 0.8f, 0.8f, 0.5f));

                // Grid setup
                Handles.color = new Color(1f, 1f, 1f, 0.05f);
                int hLines = (int)Mathf.Sqrt(innerRect.width);
                int vLines = (int)(hLines / (innerRect.width / innerRect.height));

                // Vertical grid
                int gridOffset = Mathf.FloorToInt(innerRect.width / hLines);
                int gridPadding = ((int)(innerRect.width) % hLines) / 2;

                for (int i = 1; i < hLines; i++)
                {
                    var offset = i * Vector2.right * gridOffset;
                    offset.x += gridPadding;
                    Handles.DrawLine(innerRect.position + offset, new Vector2(innerRect.x, innerRect.yMax - 1) + offset);
                }

                // Horizontal grid
                gridOffset = Mathf.FloorToInt(innerRect.height / vLines);
                gridPadding = ((int)(innerRect.height) % vLines) / 2;

                for (int i = 1; i < vLines; i++)
                {
                    var offset = i * Vector2.up * gridOffset;
                    offset.y += gridPadding;
                    Handles.DrawLine(innerRect.position + offset, new Vector2(innerRect.xMax - 1, innerRect.y) + offset);
                }
            }

            m_CurveEditor.RemoveAll();
            SerializedProperty currentCurveRawProp = null;

            switch (curveEditingId)
            {
                case 0:
                    currentCurveRawProp = masterCurve;
                    SetupCurve(masterCurve, new Color(1, 1, 1), 2, false);
                    break;
                case 1:
                    currentCurveRawProp = redCurve;
                    SetupCurve(redCurve, new Color(1, 0, 0), 2, false);
                    break;
                case 2:
                    currentCurveRawProp = greenCurve;
                    SetupCurve(greenCurve, new Color(0, 1, 0), 2, false);
                    break;
                case 3:
                    currentCurveRawProp = blueCurve;
                    SetupCurve(blueCurve, new Color(0, 0.5f, 1), 2, false);
                    break;
                case 4:
                    currentCurveRawProp = hueVsHueCurve;
                    SetupCurve(hueVsHueCurve, new Color(1, 1, 1), 0, true);
                    break;
                case 5:
                    currentCurveRawProp = hueVsSatCurve;
                    SetupCurve(hueVsSatCurve, new Color(1, 1, 1), 0, true);
                    break;
                case 6:
                    currentCurveRawProp = satVsSatCurve;
                    SetupCurve(satVsSatCurve, new Color(1, 1, 1), 0, false);
                    break;
                case 7:
                    currentCurveRawProp = lumVsSatCurve;
                    SetupCurve(lumVsSatCurve, new Color(1, 1, 1), 0, false);
                    break;
            }

            SetCurveVisible(currentCurveRawProp);

            if (m_CurveEditor.OnGUI(rect))
            {
                //m_Inspector.Repaint();
                GUI.changed = true;
            }

            if (Event.current.type == EventType.Repaint)
            {
                // Borders
                Handles.color = Color.black;
                Handles.DrawLine(new Vector2(rect.x, rect.y - 18f), new Vector2(rect.xMax, rect.y - 18f));
                Handles.DrawLine(new Vector2(rect.x, rect.y - 19f), new Vector2(rect.x, rect.yMax));
                Handles.DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax));
                Handles.DrawLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.y - 18f));

                bool editable = m_CurveEditor.GetCurveState(currentCurveRawProp).editable;
                string editableString = editable ? string.Empty : "(Not Overriding)\n";

                // Selection info
                var selection = m_CurveEditor.GetSelection();
                var infoRect = innerRect;
                infoRect.x += 5f;
                infoRect.width = 100f;
                infoRect.height = 30f;

                if (selection.curve != null && selection.keyframeIndex > -1)
                {
                    var key = selection.keyframe.Value;
                    GUI.Label(infoRect, string.Format("{0}\n{1}", key.time.ToString("F3"), key.value.ToString("F3")), Styling.preLabel);
                }
                else
                {
                    GUI.Label(infoRect, editableString, Styling.preLabel);
                }
            }
        }

        int DoCurveSelectionPopup(int id, bool hdr)
        {
            GUILayout.Label(s_Curves[id], EditorStyles.toolbarPopup, GUILayout.MaxWidth(150f));

            var lastRect = GUILayoutUtility.GetLastRect();
            var e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && lastRect.Contains(e.mousePosition))
            {
                var menu = new GenericMenu();

                for (int i = 0; i < s_Curves.Length; i++)
                {
                    if (i == 4)
                        menu.AddSeparator("");

                    if (hdr && i < 4)
                        menu.AddDisabledItem(s_Curves[i]);
                    else
                    {
                        int current = i; // Capture local for closure
                        menu.AddItem(s_Curves[i], current == id, () => currentCurve = current);
                    }
                }

                menu.DropDown(new Rect(lastRect.xMin, lastRect.yMax, 1f, 1f));
            }

            return id;
        }

        void DrawBackgroundTexture(Rect rect, int pass)
        {
            if (s_MaterialGrid == null)
                s_MaterialGrid = new Material(Shader.Find("Hidden/PostProcessing/Editor/CurveGrid")) { hideFlags = HideFlags.HideAndDontSave };

            float scale = EditorGUIUtility.pixelsPerPoint;

        #if UNITY_2018_1_OR_NEWER
            const RenderTextureReadWrite kReadWrite = RenderTextureReadWrite.sRGB;
        #else
            const RenderTextureReadWrite kReadWrite = RenderTextureReadWrite.Linear;
        #endif

            var oldRt = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(Mathf.CeilToInt(rect.width * scale), Mathf.CeilToInt(rect.height * scale), 0, RenderTextureFormat.ARGB32, kReadWrite);
            s_MaterialGrid.SetFloat("_DisabledState", GUI.enabled ? 1f : 0.5f);
            s_MaterialGrid.SetFloat("_PixelScaling", EditorGUIUtility.pixelsPerPoint);

            Graphics.Blit(null, rt, s_MaterialGrid, pass);
            RenderTexture.active = oldRt;

            GUI.DrawTexture(rect, rt);
            RenderTexture.ReleaseTemporary(rt);
        }
    }
}