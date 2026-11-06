using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace TimeTravellerRunner
{
    /// <summary>
    /// Next-Gen Cyberpunk / Subway Surfers-Grade Arcade UI/UX.
    /// Features:
    /// - Sleek translucent obsidian glass cards with glowing neon borders
    /// - Tactile 3D pill-shaped buttons with bounce/squish feedback
    /// - Dynamic top-corner HUD with animated coin punch & score multiplier
    /// - Power-up countdown gauges with flashing warnings
    /// - High-impact Game Over screen with New Record celebrations
    /// </summary>
    public sealed class RunnerUI : MonoBehaviour
    {
        private RunnerGame game;
        private Canvas canvas;
        private GameObject menu, garage, hud, pause, end;
        private Text hudScore, hudCoins, hudDistance, hudMultiplier;
        private RectTransform hudCoinsContainer;
        private Coroutine coinPunchRoutine;
        
        // Power-up & Robot HUD
        private GameObject powerGauge;
        private Text powerTitle, powerDetail;
        private Image powerFill;
        private GameObject robotAlert;
        private Text robotAlertText;
        private GameObject starterTechBtn;

        // Results screen
        private Text scoreEnd, bestEnd, coinsEnd, newRecordBanner;
        private Button reviveBtn;

        // Menu & Garage
        private Text qualityLabel, soundLabel;
        private Text garageName, garageBio;
        private readonly Text[] charLabels = new Text[3];
        private readonly Text[] gadgetLabels = new Text[3];

        private GameMode lastMode = (GameMode)(-1);
        private int lastChar = -1;
        private int lastGadget = -1;
        private static Font uiFont;

        // Vibrant Future Cyber Arcade Palette
        private static Color C(string hex) { return RunnerArt.Hex(hex); }
        private static readonly Color NeonCyan = C("#00E5FF");
        private static readonly Color NeonPink = C("#FF007F");
        private static readonly Color SolarGold = C("#FFD600");
        private static readonly Color CyberLime = C("#00E676");
        private static readonly Color DeepDark = new Color(0.08f, 0.12f, 0.18f, 0.94f);
        private static readonly Color CardGlass = new Color(0.10f, 0.15f, 0.24f, 0.92f);
        private static readonly Color White = Color.white;
        private static readonly Color MutedText = C("#94A3B8");

        private void Awake()
        {
            game = GetComponent<RunnerGame>();
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject events = new GameObject("UI EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                var module = events.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
#else
                events.AddComponent<StandaloneInputModule>();
#endif
            }

            GameObject root = new GameObject("CYBER SURFERS / MODERN ARCADE UI",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            MakeMenu();
            MakeGarage();
            MakeHud();
            MakePause();
            MakeResults();
        }

        private static Font GetFont()
        {
            if (uiFont != null) return uiFont;
            try { uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (uiFont == null) uiFont = UnityEngine.Font.CreateDynamicFontFromOSFont("Arial", 28);
            return uiFont;
        }

        // --- Procedural UI Sprite Generators ---
        private static Sprite MakePillSprite(int width, int height, Color border, Color fill)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float r = height * 0.5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float cx = x < r ? r : x > (width - r) ? (width - r) : x;
                    float cy = r;
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(r - dist);
                    
                    if (dist > r - 2.5f)
                        tex.SetPixel(x, y, new Color(border.r, border.g, border.b, border.a * alpha));
                    else
                    {
                        // Subtle glossy gradient top-to-bottom
                        float grad = Mathf.Lerp(0.85f, 1.15f, (float)y / height);
                        tex.SetPixel(x, y, new Color(fill.r * grad, fill.g * grad, fill.b * grad, fill.a * alpha));
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(height * 0.45f, height * 0.45f, height * 0.45f, height * 0.45f));
        }

        private static RectTransform MakeRect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static Image AddBox(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            RectTransform rt = MakeRect(parent, name, anchor, pivot, pos, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        private static Text AddText(Transform parent, string name, string content, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, int fontSize, Color color, bool bold = true, TextAnchor align = TextAnchor.MiddleCenter)
        {
            RectTransform rt = MakeRect(parent, name, anchor, pivot, pos, size);
            Text t = rt.gameObject.AddComponent<Text>();
            t.text = content;
            t.font = GetFont();
            t.fontSize = fontSize;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;

            if (bold && fontSize >= 22)
            {
                Shadow s = t.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0, 0, 0, 0.7f);
                s.effectDistance = new Vector2(1.5f, -2f);
            }
            return t;
        }

        private static Button AddButton(Transform parent, string name, string title, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color fill, Color textColor, int fontSize, Action onClick)
        {
            Sprite pill = MakePillSprite((int)size.x, (int)size.y, new Color(fill.r * 1.3f, fill.g * 1.3f, fill.b * 1.3f, 1f), fill);
            
            // Drop shadow
            AddBox(parent, name + "_Shadow", anchor, pivot, pos + new Vector2(0, -5), size, new Color(0, 0, 0, 0.45f), pill);
            
            Image img = AddBox(parent, name, anchor, pivot, pos, size, Color.white, pill);
            img.raycastTarget = true;
            Button btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            AddText(img.transform, "Label", title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, fontSize, textColor, true, TextAnchor.MiddleCenter);

            // Tactile squish feedback
            img.gameObject.AddComponent<JuicyButtonFeedback>();

            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        // --- Screen 1: Main Menu ---
        private void MakeMenu()
        {
            menu = MakeRect(canvas.transform, "01_MainMenu", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero).gameObject;
            RectTransform menuRt = menu.GetComponent<RectTransform>();
            menuRt.anchorMin = Vector2.zero;
            menuRt.anchorMax = Vector2.one;

            // Top Header Bar
            Sprite pillBg = MakePillSprite(260, 56, NeonCyan, DeepDark);
            Image trophyCard = AddBox(menu.transform, "TrophyBadge", new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -35), new Vector2(260, 56), Color.white, pillBg);
            AddText(trophyCard.transform, "Trophy", "🏆", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(40, 40), 24, SolarGold);
            AddText(trophyCard.transform, "Best", "BEST: " + game.BestScore.ToString("N0"), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(58, 0), new Vector2(185, 40), 20, White, true, TextAnchor.MiddleLeft);

            // Sound Toggle (Top Right)
            Button soundBtn = AddButton(menu.transform, "SoundToggle", "🔊 SOUND ON", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -35), new Vector2(170, 52), C("#1E2D4A"), White, 16, () =>
            {
                game.ToggleSound();
                if (soundLabel != null) soundLabel.text = game.SoundEnabled ? "🔊 SOUND ON" : "🔇 SOUND OFF";
            });
            soundLabel = soundBtn.GetComponentInChildren<Text>();

            // Title Banner
            AddText(menu.transform, "PreTitle", "FUTURE CITY // NEO TRANSIT", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(700, 35), 24, NeonCyan);
            AddText(menu.transform, "MainTitle", "CYBER RUNNER", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -145), new Vector2(850, 85), 68, White);
            AddBox(menu.transform, "TitleLine", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -225), new Vector2(360, 6), NeonCyan);

            // Center Call To Action: TAP TO PLAY
            Button playBtn = AddButton(menu.transform, "PlayButton", "TAP TO PLAY  ▶", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 135), new Vector2(420, 86), SolarGold, C("#3E2723"), 34, () =>
            {
                game.StartRun(RunSelection.CyberCity);
            });
            playBtn.gameObject.AddComponent<PulseIdleAnimation>();

            // Quick Select / Garage Button
            AddButton(menu.transform, "GarageBtn", "CHARACTERS & BOARDS  ★", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 52), new Vector2(380, 58), NeonCyan, White, 20, () =>
            {
                game.OpenCyberCharacterSelect();
            });

            // Separate Worlds / Themes Section (Bottom Left)
            AddText(menu.transform, "WorldsLabel", "OTHER WORLDS", new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 100), new Vector2(370, 22), 12, MutedText, true, TextAnchor.MiddleLeft);

            AddButton(menu.transform, "DemoBtn", "EARTH DEMO", new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 40), new Vector2(175, 52), C("#1C2C45"), MutedText, 16, () =>
            {
                game.StartRun(RunSelection.Demo);
            });

            AddButton(menu.transform, "DesertBtn", "DESERT THEME", new Vector2(0, 0), new Vector2(0, 0), new Vector2(225, 40), new Vector2(175, 52), C("#4A2E16"), C("#FFD54F"), 16, () =>
            {
                game.StartRun(RunSelection.AncientEgypt);
            });

            Button qualBtn = AddButton(menu.transform, "QualityBtn", "GRAPHICS: HIGH", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 40), new Vector2(210, 52), C("#1C2C45"), NeonCyan, 16, () =>
            {
                game.ToggleCyberQuality();
                if (qualityLabel != null) qualityLabel.text = game.CyberHighDetail ? "GRAPHICS: HIGH" : "GRAPHICS: FAST";
            });
            qualityLabel = qualBtn.GetComponentInChildren<Text>();
        }

        // --- Screen 2: Character & Gadget Garage ---
        private void MakeGarage()
        {
            garage = MakeRect(canvas.transform, "02_Garage", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero).gameObject;
            RectTransform gRt = garage.GetComponent<RectTransform>();
            gRt.anchorMin = Vector2.zero; gRt.anchorMax = Vector2.one;

            // Translucent Left Panel for Character Selection
            Sprite cardSprite = MakePillSprite(640, 780, NeonCyan, CardGlass);
            Image panel = AddBox(garage.transform, "GaragePanel", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(640, 780), Color.white, cardSprite);

            AddText(panel.transform, "GarageTitle", "SELECT RUNNER", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -35), new Vector2(560, 45), 32, SolarGold);

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Color cardAccent = i == 0 ? NeonCyan : i == 1 ? SolarGold : NeonPink;
                Sprite rowSprite = MakePillSprite(560, 85, cardAccent, new Color(0.08f, 0.15f, 0.25f, 0.9f));
                Image row = AddBox(panel.transform, "CharRow_" + i, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100 - i * 105), new Vector2(560, 85), Color.white, rowSprite);

                AddText(row.transform, "Icon", (i + 1).ToString("00"), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(50, 50), 24, cardAccent);
                AddText(row.transform, "Name", CyberCharacterCatalog.Name(i), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(85, 12), new Vector2(280, 32), 24, White, true, TextAnchor.MiddleLeft);
                AddText(row.transform, "Desc", CyberCharacterCatalog.Description(i), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(85, -16), new Vector2(300, 24), 14, MutedText, false, TextAnchor.MiddleLeft);

                Button btn = row.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => game.ChooseCyberCharacter(idx));
                charLabels[i] = AddText(row.transform, "State", "PICK", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-25, 0), new Vector2(100, 35), 18, CyberLime);
            }

            // Tech Loadout section
            AddText(panel.transform, "TechTitle", "STARTING TECH", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 255), new Vector2(560, 35), 22, NeonCyan);
            CyberGadget[] gadgets = { CyberGadget.Jetpack, CyberGadget.Hoverboard, CyberGadget.Robot };
            string[] gNames = { "SKY JET", "HOVER BOARD", "MECH ROBOT" };
            for (int j = 0; j < 3; j++)
            {
                int gIdx = j;
                CyberGadget gad = gadgets[j];
                Button gBtn = AddButton(panel.transform, "GadgetBtn_" + j, gNames[j], new Vector2(0, 0), new Vector2(0, 0), new Vector2(40 + j * 190, 160), new Vector2(180, 72), C("#182A45"), White, 17, () =>
                {
                    game.ChooseCyberGadget(gad);
                });
                gadgetLabels[j] = AddText(gBtn.transform, "Status", "SELECT", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(160, 20), 13, CyberLime);
            }

            // Ready & Back Buttons
            AddButton(panel.transform, "LaunchBtn", "START RUN  ▶", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(540, 68), CyberLime, C("#06131D"), 28, () =>
            {
                game.StartRun(RunSelection.CyberCity);
            });

            AddButton(garage.transform, "BackBtn", "← MENU", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -35), new Vector2(150, 52), C("#1C2C45"), White, 18, () =>
            {
                game.ResetToMenu();
            });
        }

        // --- Screen 3: In-Game HUD ---
        private void MakeHud()
        {
            hud = MakeRect(canvas.transform, "03_GameplayHUD", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero).gameObject;
            RectTransform hRt = hud.GetComponent<RectTransform>();
            hRt.anchorMin = Vector2.zero; hRt.anchorMax = Vector2.one;

            // 1. Top-Left: Score & Multiplier Pill
            Sprite scorePill = MakePillSprite(300, 64, NeonCyan, DeepDark);
            Image scoreCard = AddBox(hud.transform, "ScorePill", new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -30), new Vector2(300, 64), Color.white, scorePill);
            AddText(scoreCard.transform, "ScoreLabel", "SCORE", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(75, 40), 16, MutedText, true, TextAnchor.MiddleLeft);
            hudScore = AddText(scoreCard.transform, "ScoreValue", "0", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(95, 0), new Vector2(140, 40), 28, White, true, TextAnchor.MiddleLeft);
            
            // Multiplier Badge
            Sprite multPill = MakePillSprite(60, 36, SolarGold, SolarGold);
            Image multCard = AddBox(scoreCard.transform, "MultBadge", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(60, 36), Color.white, multPill);
            hudMultiplier = AddText(multCard.transform, "MultValue", "x1", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 36), 18, C("#06131D"), true, TextAnchor.MiddleCenter);

            // 2. Top-Right: Coins Pill
            Sprite coinPill = MakePillSprite(190, 64, SolarGold, DeepDark);
            Image coinCard = AddBox(hud.transform, "CoinsPill", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-120, -30), new Vector2(190, 64), Color.white, coinPill);
            hudCoinsContainer = coinCard.rectTransform;
            AddText(coinCard.transform, "CoinIcon", "★", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(35, 35), 24, SolarGold);
            hudCoins = AddText(coinCard.transform, "CoinsValue", "0", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(55, 0), new Vector2(115, 40), 28, SolarGold, true, TextAnchor.MiddleLeft);

            // Pause Button (Top Far-Right)
            AddButton(hud.transform, "PauseBtn", "Ⅱ", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -30), new Vector2(64, 64), C("#1C2C45"), White, 26, () =>
            {
                game.TogglePause();
            });

            // 3. Top-Center: Distance Tracker
            Sprite distPill = MakePillSprite(180, 44, C("#334E68"), DeepDark);
            Image distCard = AddBox(hud.transform, "DistancePill", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(180, 44), Color.white, distPill);
            hudDistance = AddText(distCard.transform, "DistValue", "0 m", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 44), 20, NeonCyan, true, TextAnchor.MiddleCenter);

            // 4. Power-Up Active Gauge (Top Center, below distance)
            Sprite powerBg = MakePillSprite(380, 54, NeonPink, DeepDark);
            Image pGauge = AddBox(hud.transform, "PowerGauge", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -88), new Vector2(380, 54), Color.white, powerBg);
            powerGauge = pGauge.gameObject;
            powerTitle = AddText(pGauge.transform, "PowerTitle", "JETPACK BOOST", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(240, 36), 18, NeonPink, true, TextAnchor.MiddleLeft);
            powerDetail = AddText(pGauge.transform, "PowerTime", "8s", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(80, 36), 18, White, true, TextAnchor.MiddleRight);
            
            // Power meter fill line
            Image fillBg = AddBox(pGauge.transform, "FillBg", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 3), new Vector2(350, 6), new Color(0.2f, 0.2f, 0.3f, 0.6f));
            Image fillBar = AddBox(fillBg.transform, "Fill", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(350, 6), NeonPink);
            powerFill = fillBar;
            powerGauge.SetActive(false);

            // 5. Guardian Robot Alert Banner
            Sprite robotPill = MakePillSprite(520, 68, CyberLime, DeepDark);
            Image rAlert = AddBox(hud.transform, "RobotBanner", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -155), new Vector2(520, 68), Color.white, robotPill);
            robotAlert = rAlert.gameObject;
            robotAlertText = AddText(rAlert.transform, "RobotText", "GUARDIAN AUTOPILOT ACTIVE", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 60), 22, CyberLime, true, TextAnchor.MiddleCenter);
            robotAlert.SetActive(false);

            // Starter Tech Activation Button (Bottom Right)
            starterTechBtn = AddButton(hud.transform, "StarterTech", "ACTIVATE TECH [G]", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 40), new Vector2(280, 72), SolarGold, C("#06131D"), 22, () =>
            {
                game.ActivateStartingCyberGadget();
            }).gameObject;
            starterTechBtn.SetActive(false);
        }

        // --- Screen 4: Pause Menu ---
        private void MakePause()
        {
            pause = MakeRect(canvas.transform, "04_PauseMenu", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero).gameObject;
            RectTransform pRt = pause.GetComponent<RectTransform>();
            pRt.anchorMin = Vector2.zero; pRt.anchorMax = Vector2.one;

            // Dark semi-transparent backdrop
            AddBox(pause.transform, "DimBackdrop", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2000, 1200), new Color(0, 0, 0, 0.75f));

            // Dialog Card
            Sprite dlgSprite = MakePillSprite(560, 520, NeonCyan, DeepDark);
            Image card = AddBox(pause.transform, "PauseCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 520), Color.white, dlgSprite);

            AddText(card.transform, "Title", "GAME PAUSED", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -45), new Vector2(480, 50), 38, SolarGold);

            AddButton(card.transform, "Resume", "RESUME RUN  ▶", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 260), new Vector2(440, 68), CyberLime, C("#06131D"), 26, () =>
            {
                game.TogglePause();
            });

            AddButton(card.transform, "Restart", "RESTART", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 170), new Vector2(440, 64), NeonCyan, C("#06131D"), 22, () =>
            {
                game.StartRun();
            });

            AddButton(card.transform, "Quit", "MAIN MENU", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(440, 64), C("#1C2C45"), White, 20, () =>
            {
                game.ResetToMenu();
            });
        }

        // --- Screen 5: Game Over Results ---
        private void MakeResults()
        {
            end = MakeRect(canvas.transform, "05_GameOver", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero).gameObject;
            RectTransform eRt = end.GetComponent<RectTransform>();
            eRt.anchorMin = Vector2.zero; eRt.anchorMax = Vector2.one;

            AddBox(end.transform, "DimBackdrop", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2000, 1200), new Color(0.04f, 0.02f, 0.06f, 0.88f));

            Sprite cardSprite = MakePillSprite(640, 680, NeonPink, DeepDark);
            Image card = AddBox(end.transform, "ResultsCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 680), Color.white, cardSprite);

            AddText(card.transform, "CrashTitle", "RUN OVER!", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(560, 50), 44, NeonPink);
            newRecordBanner = AddText(card.transform, "NewRecord", "🏆 NEW BEST RECORD! 🏆", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(560, 35), 22, SolarGold);

            // Stats Block
            Sprite statPill = MakePillSprite(520, 160, C("#1E2D4A"), new Color(0.08f, 0.13f, 0.22f, 0.9f));
            Image statCard = AddBox(card.transform, "StatsBlock", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(520, 160), Color.white, statPill);

            AddText(statCard.transform, "ScoreLbl", "FINAL SCORE", new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -20), new Vector2(220, 30), 16, MutedText, true, TextAnchor.MiddleLeft);
            scoreEnd = AddText(statCard.transform, "ScoreVal", "0", new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -50), new Vector2(240, 55), 44, SolarGold, true, TextAnchor.MiddleLeft);

            AddText(statCard.transform, "BestLbl", "HIGH SCORE", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -20), new Vector2(200, 30), 16, MutedText, true, TextAnchor.MiddleRight);
            bestEnd = AddText(statCard.transform, "BestVal", "0", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -50), new Vector2(200, 55), 44, CyberLime, true, TextAnchor.MiddleRight);

            coinsEnd = AddText(statCard.transform, "CoinsVal", "★  0 COINS EARNED", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(460, 30), 20, White);

            // Action Buttons
            reviveBtn = AddButton(card.transform, "ReviveBtn", "REVIVE  •  40 COINS", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 240), new Vector2(500, 68), CyberLime, C("#06131D"), 24, () =>
            {
                game.Revive();
            });

            AddButton(card.transform, "RetryBtn", "PLAY AGAIN  ▶", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 155), new Vector2(500, 68), SolarGold, C("#06131D"), 28, () =>
            {
                game.StartRun();
            });

            AddButton(card.transform, "MenuBtn", "MAIN MENU", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 75), new Vector2(500, 60), C("#1C2C45"), White, 20, () =>
            {
                game.ResetToMenu();
            });
        }

        // --- Coin Punch Animation (Subway Surfers feel) ---
        public void OnCoinCollected()
        {
            if (hudCoinsContainer == null) return;
            if (coinPunchRoutine != null) StopCoroutine(coinPunchRoutine);
            coinPunchRoutine = StartCoroutine(PunchScale(hudCoinsContainer, 1.35f, 0.16f));
        }

        private IEnumerator PunchScale(RectTransform rt, float maxScale, float duration)
        {
            float half = duration * 0.5f;
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1.0f, maxScale, t / half);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(maxScale, 1.0f, t / half);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        private void LateUpdate()
        {
            if (game == null) return;
            GameMode mode = game.Mode;

            if (mode != lastMode)
            {
                menu.SetActive(mode == GameMode.MainMenu);
                garage.SetActive(mode == GameMode.CharacterSelect);
                hud.SetActive(mode == GameMode.Playing);
                pause.SetActive(mode == GameMode.Paused);
                end.SetActive(mode == GameMode.GameOver);
                lastMode = mode;
            }

            if (mode == GameMode.CharacterSelect)
            {
                int curChar = game.CyberCharacter;
                if (curChar != lastChar)
                {
                    for (int i = 0; i < charLabels.Length; i++)
                    {
                        if (charLabels[i] != null)
                        {
                            charLabels[i].text = curChar == i ? "EQUIPPED" : "SELECT";
                            charLabels[i].color = curChar == i ? CyberLime : MutedText;
                        }
                    }
                    lastChar = curChar;
                }

                int curGadget = (int)game.SelectedCyberGadget;
                if (curGadget != lastGadget)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (gadgetLabels[j] != null)
                        {
                            gadgetLabels[j].text = curGadget == (j + 1) ? "EQUIPPED" : "SELECT";
                            gadgetLabels[j].color = curGadget == (j + 1) ? SolarGold : MutedText;
                        }
                    }
                    lastGadget = curGadget;
                }
            }
            else if (mode == GameMode.Playing)
            {
                hudScore.text = game.Score.ToString("N0");
                hudCoins.text = game.Coins.ToString("N0");
                hudDistance.text = Mathf.FloorToInt(game.Distance) + " m";

                // Multiplier based on speed / distance
                int mult = 1 + Mathf.FloorToInt(game.Distance / 400f);
                if (game.ActivePower == "SPEED BOOST") mult *= 2;
                hudMultiplier.text = "x" + mult;

                // Power-up Gauge
                bool hasPower = game.PowerTime > 0f || (game.CyberRun && game.VisibleCyberGadget != CyberGadget.None);
                powerGauge.SetActive(hasPower);
                if (hasPower)
                {
                    string pName = game.PowerTime > 0f ? game.ActivePower : CyberGadgetLibrary.Name(game.VisibleCyberGadget);
                    float timeLeft = game.PowerTime > 0f ? game.PowerTime : game.CyberGadgetTime;
                    float totalDuration = game.PowerTime > 0f ? 7f : Mathf.Max(1f, game.CyberGadgetDuration);

                    powerTitle.text = pName;
                    powerDetail.text = Mathf.CeilToInt(timeLeft) + "s";
                    if (powerFill != null)
                    {
                        float frac = Mathf.Clamp01(timeLeft / totalDuration);
                        powerFill.rectTransform.sizeDelta = new Vector2(350f * frac, 6f);
                        powerFill.color = timeLeft <= 3f ? (Time.time % 0.3f < 0.15f ? NeonPink : SolarGold) : NeonCyan;
                    }
                }

                // Guardian Robot Alert
                bool robotDriving = game.CyberRun && game.RobotAutopilotActive;
                bool robotRecovery = game.CyberRun && !robotDriving && game.RobotRecoveryTime > 0f;
                robotAlert.SetActive(robotDriving || robotRecovery);
                if (robotDriving)
                {
                    robotAlertText.text = "GUARDIAN ROBOT IN CONTROL (" + Mathf.CeilToInt(game.CyberGadgetTime) + "s)";
                    robotAlertText.color = CyberLime;
                }
                else if (robotRecovery)
                {
                    robotAlertText.text = "YOU HAVE CONTROL • SAFE (" + Mathf.CeilToInt(game.RobotRecoveryTime) + "s)";
                    robotAlertText.color = SolarGold;
                }

                // Starter tech button
                if (starterTechBtn != null)
                    starterTechBtn.SetActive(game.CyberStarterReady);
            }
            else if (mode == GameMode.GameOver)
            {
                scoreEnd.text = game.Score.ToString("N0");
                bestEnd.text = game.BestScore.ToString("N0");
                coinsEnd.text = "★  " + game.Coins.ToString("N0") + " COINS EARNED";
                if (newRecordBanner != null)
                    newRecordBanner.gameObject.SetActive(game.Score >= game.BestScore && game.Score > 0);
                if (reviveBtn != null)
                    reviveBtn.interactable = game.CanRevive;
            }
        }
    }

    /// <summary>Subtle tactile button squish feedback.</summary>
    public sealed class JuicyButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 originalScale;
        private void Awake() { originalScale = transform.localScale; }
        public void OnPointerDown(PointerEventData eventData) { transform.localScale = originalScale * 0.93f; }
        public void OnPointerUp(PointerEventData eventData) { transform.localScale = originalScale; }
    }

    /// <summary>Inviting idle breathing pulse for primary call-to-action buttons.</summary>
    public sealed class PulseIdleAnimation : MonoBehaviour
    {
        private Vector3 baseScale;
        private void Awake() { baseScale = transform.localScale; }
        private void Update()
        {
            float s = 1.0f + Mathf.Sin(Time.unscaledTime * 4.2f) * 0.045f;
            transform.localScale = baseScale * s;
        }
    }
}
