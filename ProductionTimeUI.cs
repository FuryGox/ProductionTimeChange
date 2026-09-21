using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProductionTimeChange
{
    public static class ProductionTimeUI
    {
        public static bool IsOpen = false;

        private static Rect windowRect = new Rect(80, 60, 820, 580);
        private static Vector2 scrollPosition = Vector2.zero;
        private static string searchQuery = "";
        private static CardCategory selectedCategory = CardCategory.All;

        private static string statusMessage = "";
        private static float statusTimer = 0f;

        private static GUIStyle? titleStyle;
        private static GUIStyle? headerStyle;
        private static GUIStyle? itemTitleStyle;
        private static GUIStyle? itemDetailStyle;
        private static GUIStyle? saveButtonStyle;
        private static GUIStyle? resetButtonStyle;
        private static GUIStyle? tabActiveStyle;
        private static GUIStyle? tabInactiveStyle;
        private static Texture2D? darkBackground;
        private static Texture2D? greenBackground;
        private static Texture2D? redBackground;
        private static Texture2D? tabActiveBackground;
        private static Texture2D? tabInactiveBackground;

        public static void Toggle()
        {
            IsOpen = !IsOpen;
            if (IsOpen && !CardTimeManager.IsInitialized)
            {
                // Thử khởi tạo lại nếu chưa xong
                CardTimeManager.Initialize(PathHelper.GetModDirectory());
            }
        }

        public static void Update()
        {
            if (statusTimer > 0f)
            {
                statusTimer -= Time.unscaledDeltaTime;
                if (statusTimer <= 0f)
                {
                    statusMessage = "";
                }
            }
        }

        public static void Draw()
        {
            if (!IsOpen) return;

            InitStyles();

            // Giới hạn cửa sổ trong màn hình
            windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width);
            windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height);

            GUI.backgroundColor = Color.white;
            windowRect = GUI.Window(987654, windowRect, DrawWindow, "⏱ QUẢN LÝ THỜI GIAN SẢN XUẤT (Production Time Manager)");
        }

        private static void InitStyles()
        {
            if (titleStyle != null) return;

            darkBackground = MakeTex(2, 2, new Color(0.12f, 0.13f, 0.15f, 0.98f));
            greenBackground = MakeTex(2, 2, new Color(0.18f, 0.65f, 0.32f, 1f));
            redBackground = MakeTex(2, 2, new Color(0.72f, 0.22f, 0.22f, 1f));
            tabActiveBackground = MakeTex(2, 2, new Color(0.24f, 0.44f, 0.85f, 1f));
            tabInactiveBackground = MakeTex(2, 2, new Color(0.2f, 0.22f, 0.25f, 1f));

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = Color.white;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            headerStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f, 1f);

            itemTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            itemTitleStyle.normal.textColor = Color.white;

            itemDetailStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic
            };
            itemDetailStyle.normal.textColor = new Color(0.7f, 0.75f, 0.8f, 1f);

            saveButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            saveButtonStyle.normal.background = greenBackground;
            saveButtonStyle.normal.textColor = Color.white;

            resetButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            resetButtonStyle.normal.background = redBackground;
            resetButtonStyle.normal.textColor = Color.white;

            tabActiveStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            tabActiveStyle.normal.background = tabActiveBackground;
            tabActiveStyle.normal.textColor = Color.white;

            tabInactiveStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal
            };
            tabInactiveStyle.normal.background = tabInactiveBackground;
            tabInactiveStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        }

        private static void DrawWindow(int windowID)
        {
            // Cho phép kéo thả cửa sổ bằng thanh tiêu đề
            GUI.DragWindow(new Rect(0, 0, windowRect.width - 40, 28));

            // Nút đóng nhanh [X] ở góc trên
            if (GUI.Button(new Rect(windowRect.width - 32, 4, 26, 22), "X"))
            {
                IsOpen = false;
            }

            GUILayout.Space(12);

            // 1. THANH TÌM KIẾM
            GUILayout.BeginHorizontal();
            GUILayout.Label("🔍 Tìm kiếm:", GUILayout.Width(75));
            searchQuery = GUILayout.TextField(searchQuery, GUILayout.Height(24));
            if (GUILayout.Button("Xoá", GUILayout.Width(50), GUILayout.Height(24)))
            {
                searchQuery = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // 2. TABS DANH MỤC
            GUILayout.BeginHorizontal();
            DrawCategoryTab("Tất cả", CardCategory.All);
            DrawCategoryTab("Chế tạo (Blueprint)", CardCategory.Blueprint);
            DrawCategoryTab("Thu hoạch (Harvest)", CardCategory.Harvestable);
            DrawCategoryTab("Động vật (Animal)", CardCategory.Animal);
            DrawCategoryTab("Khác", CardCategory.Other);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // 3. TIÊU ĐỀ CỘT
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Thẻ / Công thức", headerStyle, GUILayout.Width(380));
            GUILayout.Label("Gốc", headerStyle, GUILayout.Width(70));
            GUILayout.Label("Thời gian (giây)", headerStyle, GUILayout.Width(130));
            GUILayout.Label("Thao tác", headerStyle, GUILayout.Width(90));
            GUILayout.EndHorizontal();

            // 4. DANH SÁCH THẺ (SCROLLVIEW)
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, "box");

            var filtered = CardTimeManager.Entries.Where(e =>
            {
                if (selectedCategory != CardCategory.All && e.Category != selectedCategory)
                    return false;

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    string q = searchQuery.ToLower();
                    bool matchName = !string.IsNullOrEmpty(e.DisplayName) && e.DisplayName.ToLower().Contains(q);
                    bool matchId = !string.IsNullOrEmpty(e.CardId) && e.CardId.ToLower().Contains(q);
                    bool matchDetails = !string.IsNullOrEmpty(e.Details) && e.Details.ToLower().Contains(q);
                    return matchName || matchId || matchDetails;
                }
                return true;
            }).ToList();

            if (filtered.Count == 0)
            {
                GUILayout.Label("Không tìm thấy thẻ nào phù hợp.", GUILayout.Height(50));
            }
            else
            {
                foreach (var entry in filtered)
                {
                    GUILayout.BeginHorizontal("box", GUILayout.Height(36));

                    // Cột 1: Tên & thông tin
                    GUILayout.BeginVertical(GUILayout.Width(380));
                    GUILayout.Label(entry.DisplayName, itemTitleStyle);
                    if (!string.IsNullOrEmpty(entry.Details))
                    {
                        GUILayout.Label(entry.Details, itemDetailStyle);
                    }
                    GUILayout.EndVertical();

                    // Cột 2: Thời gian gốc
                    GUILayout.Label($"{entry.OriginalTime:0.#}s", GUILayout.Width(70), GUILayout.Height(28));

                    // Cột 3: Ô nhập thời gian mới
                    string newInput = GUILayout.TextField(entry.InputBuffer, GUILayout.Width(100), GUILayout.Height(24));
                    if (newInput != entry.InputBuffer)
                    {
                        entry.InputBuffer = newInput;
                        if (float.TryParse(newInput, out float parsedVal) && parsedVal >= 0.1f)
                        {
                            entry.CustomTime = parsedVal;
                        }
                    }
                    GUILayout.Label("s", GUILayout.Width(20));

                    // Cột 4: Nút Đặt lại về gốc
                    if (GUILayout.Button("↺ Gốc", GUILayout.Width(80), GUILayout.Height(24)))
                    {
                        CardTimeManager.ResetEntryToDefault(entry);
                        SetStatus($"Đã khôi phục mặc định: {entry.DisplayName}");
                    }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(8);

            // 5. THANH ĐIỀU KHIỂN DƯỚI CÙNG & THÔNG BÁO
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("💾 LƯU & ÁP DỤNG", saveButtonStyle, GUILayout.Width(160), GUILayout.Height(34)))
            {
                CardTimeManager.SaveConfig();
                SetStatus("✔ Đã lưu cấu hình và áp dụng thay đổi thành công!");
            }

            if (GUILayout.Button("🔄 Đặt lại tất cả về gốc", resetButtonStyle, GUILayout.Width(170), GUILayout.Height(34)))
            {
                CardTimeManager.ResetAllToDefault();
                SetStatus("✔ Đã khôi phục toàn bộ thẻ về thời gian mặc định!");
            }

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUI.color = Color.yellow;
                GUILayout.Label(statusMessage, GUILayout.Height(34));
                GUI.color = Color.white;
            }

            if (GUILayout.Button("Đóng (P)", GUILayout.Width(90), GUILayout.Height(34)))
            {
                IsOpen = false;
            }

            GUILayout.EndHorizontal();
        }

        private static void DrawCategoryTab(string label, CardCategory cat)
        {
            GUIStyle style = (selectedCategory == cat) ? tabActiveStyle! : tabInactiveStyle!;
            if (GUILayout.Button(label, style, GUILayout.Height(26)))
            {
                selectedCategory = cat;
            }
        }

        private static void SetStatus(string msg)
        {
            statusMessage = msg;
            statusTimer = 3.5f;
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }

    public static class PathHelper
    {
        public static string ModDirectory = "";

        public static string GetModDirectory()
        {
            if (!string.IsNullOrEmpty(ModDirectory)) return ModDirectory;
            return Application.persistentDataPath;
        }
    }
}
