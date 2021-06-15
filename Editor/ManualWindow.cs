using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;

namespace ManualUtility
{
    public class ManualWindow : EditorWindow
    {
        static ManualWindow s_Window;

        Splitter splitter;
        ManualTreeView treeView;

        Manual m_Manual;
        ManualPage m_Page;

        [MenuItem("Help/Manual Window", priority = -10)]
        static void OpenWindow()
        {
            GetWindow<ManualWindow>();
        }

        private void OnEnable()
        {
            s_Window = this;
            var t = new GUIContent(EditorGUIUtility.IconContent("_Help"));
            t.text = "Manual";
            titleContent = t;
            minSize = new Vector2(180 + 340, 600);
            splitter = new Splitter(180, onDrawList, onDrawContent, Splitter.SplitLockMode.BothMinSize, new Vector2(180, 340));
            treeView = new ManualTreeView();
            treeView.onPageChanged += TreeView_onPageChanged;
            Reload();
        }

        private void TreeView_onPageChanged(ManualPage page)
        {
            m_Page = page;
            Repaint();
        }

        Manual[] m_Manuals;
        string[] m_Names;
        int selected = -1;

        public static void RefreshContent()
        {
            if (s_Window == null)
                return;

            s_Window.Repaint();
        }

        public static void Reload()
        {
            s_Window?.ReloadContent();
        }

        public void ReloadContent()
        {
            List<Manual> manuals = new List<Manual>();
            List<string> names = new List<string>();
            var assets = AssetDatabase.FindAssets("t:Manual");

            foreach(var asset in assets)
            {
                Manual m = AssetDatabase.LoadAssetAtPath<Manual>(AssetDatabase.GUIDToAssetPath(asset));
                if (m != null)
                {
                    manuals.Add(m);
                    names.Add(m.manualName);
                }
            }

            m_Manuals = manuals.ToArray();
            m_Names = names.ToArray();

            if (m_Names.Length > 0)
            {
                selected = Mathf.Clamp(selected, 0, m_Names.Length);
                LoadManual(m_Manuals[selected]);
            }
            else
                LoadManual(null);
        }

        void LoadManual(Manual m)
        {
            treeView.SetManual(m);
            Repaint();
        }


        private void OnGUI()
        {
            using(new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                int newVal = EditorGUILayout.Popup(selected, m_Names, EditorStyles.toolbarDropDown);
                if(EditorGUI.EndChangeCheck())
                {
                    LoadManual(m_Manuals[newVal]);
                    selected = newVal;

                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), EditorStyles.toolbarButton))
                    Reload();

                GUILayout.FlexibleSpace();
            }

            var r = new Rect(0, 22, this.position.width, this.position.height - 22);

            if(splitter.DoSplitter(r))
                Repaint();
        }

        void onDrawList(Rect r)
        {
            treeView.OnGUI(r);
        }

        void onDrawContent(Rect r)
        {
            r = new RectOffset(12, 12, 12, 12).Remove(r);
            using (new GUILayout.AreaScope(r))
            {
                DrawContent(m_Page);
            }
        }

        void DrawContent(ManualPage p)
        {
            if (p == null)
                return;

            GUILayout.Label(p.pageName, Styles.title);

            foreach(var content in p.contents)
            {
                GUIStyle s = EditorStyles.label;
                switch (content.contentType)
                {
                    default:
                    case ManualPage.PageContent.PageContentType.Paragraph:
                        s = Styles.p;
                        break;
                    case ManualPage.PageContent.PageContentType.Header1:
                        s = Styles.h1;
                        break;
                    case ManualPage.PageContent.PageContentType.Header2:
                        s = Styles.h2;
                        break;
                    case ManualPage.PageContent.PageContentType.Header3:
                        s = Styles.h3;
                        break;
                    case ManualPage.PageContent.PageContentType.List:
                        s = Styles.p;
                        break;
                    case ManualPage.PageContent.PageContentType.Links:
                        s = Styles.p;
                        break;
                }

                if(content.contentType != ManualPage.PageContent.PageContentType.Image)
                {
                    foreach(var line in content.lines)
                    {
                        GUILayout.Label(line, s);
                        EditorGUILayout.Space();
                    }
                }
                EditorGUILayout.Space();
            }
        }


        static class Styles
        {
            public static GUIStyle title;
            public static GUIStyle h1;
            public static GUIStyle h2;
            public static GUIStyle h3;

            public static GUIStyle p;


            static Styles()
            {
                title = new GUIStyle(EditorStyles.boldLabel);
                title.fontSize = 18;

                h1 = new GUIStyle(EditorStyles.boldLabel);
                h1.fontSize = 16;
                h2 = new GUIStyle(EditorStyles.boldLabel);
                h2.fontSize = 14;
                h3 = new GUIStyle(EditorStyles.boldLabel);
                h3.fontSize = 12;

                p = new GUIStyle(EditorStyles.label);
                p.wordWrap = true;

            }
        }

        #region TREE VIEW
        private class ManualTreeView : TreeView
        {
            public delegate void PageChangedDelegate(ManualPage page);
            public event PageChangedDelegate onPageChanged;


            static Dictionary<int, ManualPage> s_Pages;
            Manual m_Manual;

            public ManualTreeView() : base(new TreeViewState())
            {
                Reload();
            }

            public void SetManual(Manual manual)
            {
                m_Manual = manual;
                Reload();
            }

            protected override bool CanMultiSelect(TreeViewItem item) => false;

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                if (selectedIds.Count == 0 || selectedIds[0] == -1)
                    onPageChanged?.Invoke(null);
                else
                    onPageChanged?.Invoke(s_Pages[selectedIds[0]]);
            }

            

            protected override TreeViewItem BuildRoot()
            {
                if (s_Pages == null)
                    s_Pages = new Dictionary<int, ManualPage>();
                else
                    s_Pages.Clear();

                var root = new TreeViewItem()
                {
                    depth = -1
                };

                if (m_Manual == null)
                {
                    root.AddChild(new TreeViewItem()
                    {
                        displayName = "No Manuals Selected"
                    });
                }
                else
                {
                    if(m_Manual.pages.Length == 0)
                    {
                        root.AddChild(new TreeViewItem()
                        {
                            displayName = "No Pages found in Manual",
                            id = -1
                        });
                    }
                    else
                    {
                        int id = 0;
                        foreach(var page in m_Manual.pages)
                        {
                            if (page != null)
                            {
                                root.AddChild(GetItem(page, ref id));
                            }
                        }
                    }
                }

                return root;
            }

            static TreeViewItem GetItem(ManualPage page, ref int id)
            {
                s_Pages.Add(id, page);

                TreeViewItem tvi = new TreeViewItem()
                {
                    displayName = page.pageName,
                    id = id++
                };

                if(page.subPages != null)
                {
                    foreach (var p in page.subPages)
                    {
                        if (p != null)
                        {
                            tvi.AddChild(GetItem(p, ref id));
                            id++;
                        }
                    }
                }

                return tvi;
            }
        }



        #endregion

        #region SPLITTER
        class Splitter
        {
            public enum SplitLockMode
            {
                None = 0,
                BothMinSize = 1,
                LeftMinMax = 2,
                RightMinMax = 3
            }

            public float value
            {
                get { return m_SplitterValue; }
                set { SetSplitterValue(value); }
            }

            public delegate void SplitViewOnGUIDelegate(Rect drawRect);

            private SplitViewOnGUIDelegate m_onDrawLeftDelegate;
            private SplitViewOnGUIDelegate m_onDrawRightDelegate;

            private float m_SplitterValue;
            private bool m_Resize;
            private SplitLockMode m_LockMode;
            private Vector2 m_LockValues;

            public Splitter(float initialLeftWidth, SplitViewOnGUIDelegate onDrawLeftDelegate, SplitViewOnGUIDelegate onDrawRightDelegate, SplitLockMode lockMode, Vector2 lockValues)
            {
                m_SplitterValue = initialLeftWidth;
                m_onDrawLeftDelegate = onDrawLeftDelegate;
                m_onDrawRightDelegate = onDrawRightDelegate;
                m_LockMode = lockMode;

                if (((int)lockMode > 1) && (lockValues.y < lockValues.x))
                    m_LockValues = new Vector2(lockValues.y, lockValues.x);
                else
                    m_LockValues = lockValues;

            }

            public bool DoSplitter(Rect rect)
            {
                if (m_onDrawLeftDelegate != null)
                {
                    m_onDrawLeftDelegate(new Rect(rect.x, rect.y, m_SplitterValue, rect.height));
                }

                if (m_onDrawRightDelegate != null)
                {
                    m_onDrawRightDelegate(new Rect(rect.x + m_SplitterValue, rect.y, rect.width - m_SplitterValue, rect.height));
                }

                HandlePanelResize(rect);

                return m_Resize;
            }

            private void SetSplitterValue(float Value)
            {
                m_SplitterValue = Value;
            }

            private void HandlePanelResize(Rect rect)
            {
                Rect resizeActiveArea = new Rect(rect.x + m_SplitterValue - 8, rect.y, 16, rect.height);

                EditorGUIUtility.AddCursorRect(resizeActiveArea, MouseCursor.ResizeHorizontal);

                if (Event.current.type == EventType.MouseDown && resizeActiveArea.Contains(Event.current.mousePosition))
                    m_Resize = true;

                if (m_Resize)
                {
                    value = Event.current.mousePosition.x;
                }

                switch (m_LockMode)
                {
                    case SplitLockMode.BothMinSize:
                        m_SplitterValue = Mathf.Clamp(m_SplitterValue, m_LockValues.x, rect.width - m_LockValues.y);
                        break;
                    case SplitLockMode.LeftMinMax:
                        m_SplitterValue = Mathf.Clamp(m_SplitterValue, m_LockValues.x, m_LockValues.y);
                        break;
                    case SplitLockMode.RightMinMax:
                        m_SplitterValue = Mathf.Clamp(m_SplitterValue, rect.width - m_LockValues.y, rect.width - m_LockValues.x);
                        break;
                    default:
                        break;
                }

                RectOffset o = new RectOffset(7, 8, 0, 0);
                EditorGUI.DrawRect(o.Remove(resizeActiveArea), new Color(0, 0, 0, 0.5f));
                if (Event.current.type == EventType.MouseUp)
                    m_Resize = false;
            }

        }
        #endregion
    }
}
