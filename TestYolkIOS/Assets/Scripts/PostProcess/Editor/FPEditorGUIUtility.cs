using UnityEditor;
using UnityEngine;

#region enum
public enum FPEditorGUISize
{
    small,
    normal,
    large,
}

#endregion

public static class FPEditorGUIUtility
{
    private static class Styles
    {
        public static readonly GUIStyle HEADER = "ShurikenModuleTitle";
        public static readonly GUIStyle HEADER_BG = "ShurikenModuleBg";
        public static readonly GUIStyle HEADER_CHECKBOX = "ShurikenCheckMark";
        public static readonly GUIStyle HEADER_PLUS = "ShurikenPlus";
        public static readonly GUIStyle HEADER_MINUS = "ShurikenMinus";
        private static GUIStyle s_bigHeaderLabel;
        private static GUIStyle graphBackgroundGUIStyle;
        public static GUIStyle GraphBackgroundGUIStyle
        {
            get
            {
                if (graphBackgroundGUIStyle == null)
                {
                    Texture2D val = new Texture2D(1, 1);
                    if (EditorGUIUtility.isProSkin)
                    {
                        val.SetPixel(1, 1, new Color(0.1647f, 0.1647f, 0.1647f));
                    }
                    else
                    {
                        val.SetPixel(1, 1, new Color(0.3647f, 0.3647f, 0.3647f));
                    }
                    val.hideFlags = HideFlags.HideAndDontSave;
                    val.Apply();
                    graphBackgroundGUIStyle = new GUIStyle(GUI.skin.box);
                    graphBackgroundGUIStyle.normal.background = val;
                    graphBackgroundGUIStyle.active.background = val;
                    graphBackgroundGUIStyle.hover.background = val;
                    graphBackgroundGUIStyle.focused.background = val;
                }
                return graphBackgroundGUIStyle;
            }
        }
        public static GUIStyle BigHeaderLabel
        {
            get
            {
                if (s_bigHeaderLabel == null)
                {

                    s_bigHeaderLabel = new GUIStyle("ShurikenModuleTitle")
                    {
                        fontStyle = FontStyle.Bold,
                        fixedHeight = 30
                        };
                    }
                    return s_bigHeaderLabel;
                }
            }
        private static GUIStyle s_lineStyle;
        public static GUIStyle LineStyle
            {
                get
                {
                    if (s_lineStyle == null)
                    {
                        s_lineStyle = new GUIStyle
                        {
                            normal = { background = EditorGUIUtility.whiteTexture },
                            stretchWidth = true
                            };
                        }
                        return s_lineStyle;
                    }
                }
        static Styles()
        {
            HEADER.font = (new GUIStyle("Label")).font;
            HEADER.border = new RectOffset(15, 7, 4, 4);
            HEADER.fixedHeight = 22;
            HEADER.contentOffset = new Vector2(20f, -2f);
        }
    }
    public static readonly GUIContent TEMP_TEXT = new GUIContent();
    // Fix for retina displays
    public static float ScreenWidthRetina
    {
        get
        {
            #if UNITY_5_4_OR_NEWER
            return Screen.width / EditorGUIUtility.pixelsPerPoint;
            #else
            return Screen.width;
            #endif
        }
    }

    #region GUI Elements

    public static void ProgressBar(float value, string label)
    {
        Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
        EditorGUI.ProgressBar(rect, value, label);
        EditorGUILayout.Space();
    }

    public static GUIContent TempContent(string label, string tooltip = null)
    {
        TEMP_TEXT.text = label;
        TEMP_TEXT.tooltip = tooltip;
        return TEMP_TEXT;
    }

    public static void Separator(float space = 4f)
    {
        GUILayout.Space(4);
        Line(new Color(.3f, .3f, .3f), 1);
        Line(new Color(.9f, .9f, .9f), 1);
        GUILayout.Space(space);
    }

    public static void Separator(Rect position, float space = 4f)
    {
        Rect lineRect = position;
        lineRect.height = 1;
        Line(lineRect, new Color(.3f, .3f, .3f), 1);
        lineRect.y += 1;
        Line(lineRect, new Color(.9f, .9f, .9f), 1);
        GUILayout.Space(space);
    }

    public static void BigSeparator(float space = 4f)
    {
        GUILayout.Space(10);
        Line(new Color(.3f, .3f, .3f), 2);
        Line(new Color(.85f, .85f, .85f), 1);
        GUILayout.Space(1);
        Line(new Color(.3f, .3f, .3f), 2);
        GUILayout.Space(space);
    }

    public static void BigHeaderLabel(string label)
    {
        BigHeaderLabel(TempContent(label));
    }

    public static void BigHeaderLabel(GUIContent label)
    {
        EditorGUILayout.LabelField(label, Styles.BigHeaderLabel);
    }

    public static bool ToggleLeft(string label, bool boolValue)
    {
        return EditorGUILayout.ToggleLeft(label, boolValue, boolValue ? EditorStyles.boldLabel : EditorStyles.label);
    }
    #endregion


    #region Line

    public static void Line(float height = 2f)
    {
        Line(Color.black, height);
    }

    public static void Line(Color color, float height = 2f)
    {
        Rect position = GUILayoutUtility.GetRect(0f, float.MaxValue, height, height, Styles.LineStyle);
        if (Event.current.type == EventType.Repaint)
        {
            Color orgColor = GUI.color;
            GUI.color = orgColor * color;
            Styles.LineStyle.Draw(position, false, false, false, false);
            GUI.color = orgColor;
        }
    }

    public static void Line(Rect position, Color color, float height = 2f)
    {
        if (Event.current.type == EventType.Repaint)
        {
            Color orgColor = GUI.color;
            GUI.color = orgColor * color;
            Styles.LineStyle.Draw(position, false, false, false, false);
            GUI.color = orgColor;
        }
    }
    #endregion


    #region ToogleGroup

    public static bool ToggleGroup(string title, ref bool isDisplayed)
    {
        return ToggleGroup(TempContent(title), ref isDisplayed);
    }

    public static bool ToggleGroup(GUIContent title, ref bool isDisplayed)
    {
        Rect headerRect = GUILayoutUtility.GetRect(16f, 22f, Styles.HEADER);
        GUI.Box(headerRect, title, Styles.HEADER);
        if (Event.current.type == EventType.Repaint)
        {
            (isDisplayed ? Styles.HEADER_MINUS : Styles.HEADER_PLUS).Draw(
            new Rect(headerRect.x + 4f, headerRect.y + 8f, 13f, 13f), false, false, isDisplayed, false);
        }
        Event e = Event.current;
        if (e.type == EventType.MouseDown)
        {
            if (headerRect.Contains(e.mousePosition))
            {
                isDisplayed = !isDisplayed;
                e.Use();
            }
        }
        return isDisplayed;
    }

    public static bool ToggleGroup(string title, ref bool isDisplayed, ref bool isEnabled)
    {
        return ToggleGroup(TempContent(title), ref isDisplayed, ref isEnabled);
    }

    public static bool ToggleGroupClose(string title, bool showClose, ref bool isDisplayed, ref bool isEnabled, ref bool isClose)
    {
        Rect headerRect = GUILayoutUtility.GetRect(16f, 22f, Styles.HEADER);
        GUI.Box(headerRect, title, Styles.HEADER);
        Rect toggleRect = new Rect(headerRect.x + 6f, headerRect.y + 4f, 13f, 13f);
        Rect closeRect = new Rect(headerRect.width - 15f, headerRect.y + 2f, 20f, 15f);
        if (Event.current.type == EventType.Repaint)
        {
            Styles.HEADER_CHECKBOX.Draw(toggleRect, false, false, isEnabled, false);
        }
        if(showClose && GUI.Button(closeRect, "-"))
        {
            isClose = true;
        }
        Event e = Event.current;
        if (e.type == EventType.MouseDown)
        {
            if (toggleRect.Contains(e.mousePosition))
            {
                isEnabled = !isEnabled;
                if (isEnabled) isDisplayed = true;
                e.Use();
                GUI.changed = true;
            }

            else if (headerRect.Contains(e.mousePosition))
            {
                isDisplayed = !isDisplayed;
                e.Use();
                GUI.changed = true;
            }
        }
        return isDisplayed;
    }

    public static bool ToggleGroupClose(string title, bool showClose, ref bool isDisplayed, SerializedProperty enableProperty, ref bool isClose)
    {
        enableProperty.serializedObject.Update();
        bool isEnabled = enableProperty.boolValue;
        bool lastEnabled = isEnabled;

        Rect headerRect = GUILayoutUtility.GetRect(16f, 22f, Styles.HEADER);
        GUI.Box(headerRect, title, Styles.HEADER);
        Rect toggleRect = new Rect(headerRect.x + 6f, headerRect.y + 4f, 13f, 13f);
        Rect closeRect = new Rect(headerRect.width - 15f, headerRect.y + 2f, 20f, 15f);
        if (Event.current.type == EventType.Repaint)
        {
            Styles.HEADER_CHECKBOX.Draw(toggleRect, false, false, isEnabled, false);
        }
        if (showClose && GUI.Button(closeRect, "-"))
        {
            isClose = true;
        }
        Event e = Event.current;
        if (e.type == EventType.MouseDown)
        {
            if (toggleRect.Contains(e.mousePosition))
            {
                isEnabled = !isEnabled;
                if (isEnabled) isDisplayed = true;
                e.Use();
                GUI.changed = true;
            }

            else if (headerRect.Contains(e.mousePosition))
            {
                isDisplayed = !isDisplayed;
                e.Use();
                GUI.changed = true;
            }
        }

        if (isEnabled != lastEnabled)
            enableProperty.boolValue = isEnabled;

        enableProperty.serializedObject.ApplyModifiedProperties();

        return isDisplayed;
    }

    public static bool ToggleGroup(GUIContent title, ref bool isDisplayed, ref bool isEnabled)
    {
        Rect headerRect = GUILayoutUtility.GetRect(16f, 22f, Styles.HEADER);
        GUI.Box(headerRect, title, Styles.HEADER);
        Rect toggleRect = new Rect(headerRect.x + 4f, headerRect.y + 4f, 13f, 13f);
        if (Event.current.type == EventType.Repaint)
        {
            Styles.HEADER_CHECKBOX.Draw(toggleRect, false, false, isEnabled, false);
        }
        Event e = Event.current;
        if (e.type == EventType.MouseDown)
        {
            if (toggleRect.Contains(e.mousePosition))
            {
                isEnabled = !isEnabled;
                if (isEnabled) isDisplayed = true;
                e.Use();
                GUI.changed = true;
            }

            else if (headerRect.Contains(e.mousePosition))
            {
                isDisplayed = !isDisplayed;
                e.Use();
                GUI.changed = true;
            }
        }
        return isDisplayed;
    }

    public static void BeginGroup(bool disabled = false)
    {
        Rect vertRect = EditorGUILayout.BeginVertical();
        vertRect.xMax += 2;
        vertRect.xMin--;
        GUI.Box(vertRect, "", Styles.HEADER_BG);
        GUILayout.Space(4f);
        EditorGUI.BeginDisabledGroup(disabled);
    }

    public static void EndGroup()
    {
        EditorGUI.EndDisabledGroup();
        GUILayout.Space(8f);
        EditorGUILayout.EndVertical();
    }
    #endregion

    #region panel
    public static void BeginBGArea(Rect _rect)
    {
        GUILayout.BeginArea(_rect, Styles.GraphBackgroundGUIStyle);   
    }

    public static void EndBGArea()
    {
        GUILayout.EndArea();
    }

    #endregion

    #region text
    public static void LabelText(string _str, bool _isBlod = false, FPEditorGUISize _size = FPEditorGUISize.normal)
    {
        var boldtext = new GUIStyle(GUI.skin.label);
        
        if (_isBlod)
        {
            boldtext.fontStyle = FontStyle.Bold;
        }
        else
        {
            boldtext.fontStyle = FontStyle.Normal;
        }
        switch (_size)
        {
            case FPEditorGUISize.small:
                boldtext.fontSize = 10;
                boldtext.fixedHeight = 20;
                break;
            case FPEditorGUISize.normal:
                boldtext.fontSize = 14;
                boldtext.fixedHeight = 25;
                break;
            case FPEditorGUISize.large:
                boldtext.fontSize = 18;
                boldtext.fixedHeight = 30;
                break;
            default:
                break;
        }

        EditorGUILayout.LabelField(_str, boldtext);
    }
    #endregion

    #region button

    public static void Button(string _str, FPEditorGUISize _size)
    {
        switch (_size)
        {
            case FPEditorGUISize.small:
                GUILayout.Button(_str, GUILayout.Width(100), GUILayout.Height(30));
                break;
            case FPEditorGUISize.normal:
                GUILayout.Button(_str, GUILayout.Width(150), GUILayout.Height(40));
                break;
            case FPEditorGUISize.large:
                GUILayout.Button(_str, GUILayout.Width(200), GUILayout.Height(50));
                break;
            default:
                break;
        }
    }

    #endregion


    #region Time

    public static string Sce2TimeString(float _time)
    {
        if (_time < 60)
        {
            return string.Format("{0:N1}秒", _time);
        }
        else if (_time < 3600)
        {
            return string.Format("{0}分{1:N1}秒", (int)Mathf.Floor(_time / 60), _time % 60f);
        }
        else
        {
            return string.Format("{0}小时{1}分{2:N1}秒", Mathf.Floor(_time / 3600), (int)Mathf.Floor((_time / 60) % 60f), _time % 60f);
        }
    }
    #endregion

}
