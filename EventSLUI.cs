using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EventSL
{
    public static class EventSLUI
    {
        private const string ContainerName = "ModSLButtonsContainer";

        public static bool IsChineseLanguage()
        {
            try
            {
                if (Launcher.languageDic != null && Launcher.languageDic.TryGetValue(Define.systemValue.languageIndex, out string lang))
                {
                    return lang.Contains("Chinese") || lang.Contains("中文");
                }
                return Application.systemLanguage == SystemLanguage.Chinese ||
                       Application.systemLanguage == SystemLanguage.ChineseSimplified ||
                       Application.systemLanguage == SystemLanguage.ChineseTraditional;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Ensure the SL button container is attached under the pilot portrait on BannerPanel.
        /// Width matches the portrait width, positioned directly beneath it.
        /// </summary>
        public static void EnsureBannerButtons(BannerPanel banner)
        {
            if (banner == null || EventSLMod.Config == null || !EventSLMod.Config.ShowInGameButtons)
            {
                return;
            }

            try
            {
                // If an old container exists, destroy it so we recreate with updated design
                Transform existing = banner.transform.Find(ContainerName);
                if (existing != null)
                {
                    UnityEngine.Object.Destroy(existing.gameObject);
                }

                if (banner.head == null)
                {
                    return;
                }

                // Create container directly under banner.transform
                GameObject containerObj = new GameObject(ContainerName, typeof(RectTransform));
                containerObj.transform.SetParent(banner.transform, false);
                containerObj.transform.SetAsLastSibling();

                RectTransform containerRect = containerObj.GetComponent<RectTransform>();
                containerRect.pivot = new Vector2(0.5f, 1f); // top-center pivot
                containerRect.anchorMin = new Vector2(0.5f, 0.5f);
                containerRect.anchorMax = new Vector2(0.5f, 0.5f);

                // Language detection
                bool isChinese = IsChineseLanguage();
                string vacationText = isChinese ? "整体 SL" : "Vacation SL";

                // Load native Lonestar button sprites (9-slice sci-fi rectangular frame)
                Sprite orangeNormal = null;
                Sprite orangeHover = null;

                try
                {
                    var rm = Singleton<ResourcesManager>.Instance();
                    if (rm != null)
                    {
                        orangeNormal = rm.Load<Sprite>(FilePath.spriteUI + "UI_treasure_panel_orange");
                        orangeHover = rm.Load<Sprite>(FilePath.spriteUI + "UI_treasure_panel_orange_hover");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[EventSL] Failed to load UI sprites: " + e);
                }

                // Fallbacks if sprites failed to load
                if (orangeNormal == null)
                {
                    try
                    {
                        orangeNormal = Singleton<ResourcesManager>.Instance()?.Load<Sprite>(FilePath.hover);
                        orangeHover = orangeNormal;
                    }
                    catch { }
                }

                // Single SL Button: 整体 SL (重置整轮度假) - Amber / Orange
                Color amberColor = new Color(1.0f, 0.72f, 0.28f, 1.0f);
                GameObject btnObj = CreateSLButton(
                    parent: containerObj.transform,
                    name: "ModVacationSLBtn",
                    text: vacationText,
                    normalSprite: orangeNormal,
                    hoverSprite: orangeHover,
                    textColor: amberColor,
                    banner: banner,
                    onClick: () =>
                    {
                        EventSLMod.PerformFullVacationSL();
                    }
                );

                // Attach dynamic positioning controller
                var controller = containerObj.AddComponent<ModSLButtonsController>();
                controller.Init(
                    b: banner,
                    cRect: containerRect,
                    bRect: btnObj.GetComponent<RectTransform>()
                );

                Debug.Log("[EventSL] Successfully attached portrait SL button to BannerPanel.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[EventSL] EnsureBannerButtons error: " + ex);
            }
        }

        private static GameObject CreateSLButton(
            Transform parent,
            string name,
            string text,
            Sprite normalSprite,
            Sprite hoverSprite,
            Color textColor,
            BannerPanel banner,
            Action onClick)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(SLButtonHoverHandler));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 1f);

            // Sliced rectangular background
            Image img = btnObj.GetComponent<Image>();
            if (normalSprite != null)
            {
                img.sprite = normalSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(0.08f, 0.14f, 0.22f, 0.95f);
            }

            // Text component
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(4f, 2f);
            textRt.offsetMax = new Vector2(-4f, -2f);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (banner != null && banner.hpText != null)
            {
                tmp.font = banner.hpText.font;
                tmp.fontSharedMaterial = banner.hpText.fontSharedMaterial;
            }
            tmp.text = text;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 9f;
            tmp.fontSizeMax = 13f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;

            // Button click & sprite swap on hover
            Button btn = btnObj.GetComponent<Button>();
            btn.transition = Selectable.Transition.SpriteSwap;
            if (hoverSprite != null)
            {
                SpriteState ss = new SpriteState();
                ss.highlightedSprite = hoverSprite;
                ss.pressedSprite = hoverSprite;
                ss.selectedSprite = hoverSprite;
                ss.disabledSprite = normalSprite;
                btn.spriteState = ss;
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                try
                {
                    Singleton<AudioManager>.Instance()?.PlaySound(FilePath.sound + "Button/Button_Select_3");
                }
                catch { }
                onClick?.Invoke();
            });

            btn.interactable = true;
            btnObj.SetActive(true);
            return btnObj;
        }
    }

    /// <summary>
    /// Plays native hover sound when pointer enters the button.
    /// </summary>
    public class SLButtonHoverHandler : MonoBehaviour, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            try
            {
                Singleton<AudioManager>.Instance()?.PlaySound(FilePath.sound + "Button/Button_Hover_Short");
            }
            catch { }
        }
    }

    /// <summary>
    /// Manages dynamic alignment directly beneath the portrait frame and automatic visibility.
    /// </summary>
    public class ModSLButtonsController : MonoBehaviour
    {
        private BannerPanel banner;
        private RectTransform containerRect;
        private RectTransform btnRect;

        private readonly Vector3[] corners = new Vector3[4];

        public void Init(
            BannerPanel b,
            RectTransform cRect,
            RectTransform bRect)
        {
            banner = b;
            containerRect = cRect;
            btnRect = bRect;
        }

        private void LateUpdate()
        {
            if (banner == null || containerRect == null) return;

            bool inVacation = EventSLMod.IsInVacation();
            bool inEvent = EventSLMod.IsInEvent();
            bool configShow = EventSLMod.Config != null && EventSLMod.Config.ShowInGameButtons;
            bool shouldShow = configShow && (inVacation || inEvent);

            if (!shouldShow)
            {
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }
                return;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            // Synchronize position and width directly beneath portrait / frame
            if (banner.head != null && banner.head.gameObject.activeInHierarchy)
            {
                // Use head frame parent if available and valid
                RectTransform targetRect = banner.head.rectTransform;
                if (banner.head.transform.parent is RectTransform parentRt &&
                    parentRt != banner.transform &&
                    parentRt != banner.left)
                {
                    targetRect = parentRt;
                }

                targetRect.GetWorldCorners(corners);
                Vector3 bl = banner.transform.InverseTransformPoint(corners[0]);
                Vector3 br = banner.transform.InverseTransformPoint(corners[3]);
                float headWidth = Mathf.Abs(br.x - bl.x);
                float headCenterX = (bl.x + br.x) * 0.5f;
                float headBottomY = bl.y;

                if (headWidth > 20f)
                {
                    float spacing = 6f;        // Distance below the portrait bottom line
                    float btnHeight = 26f;     // Standard button height

                    containerRect.pivot = new Vector2(0.5f, 1f);
                    containerRect.anchorMin = new Vector2(0.5f, 0.5f);
                    containerRect.anchorMax = new Vector2(0.5f, 0.5f);
                    containerRect.localPosition = new Vector3(headCenterX, headBottomY - spacing, 0f);
                    containerRect.sizeDelta = new Vector2(headWidth, btnHeight);

                    // Single Vacation SL Button
                    if (btnRect != null)
                    {
                        btnRect.pivot = new Vector2(0.5f, 1f);
                        btnRect.anchorMin = new Vector2(0.5f, 1f);
                        btnRect.anchorMax = new Vector2(0.5f, 1f);
                        btnRect.anchoredPosition = Vector2.zero;
                        btnRect.sizeDelta = new Vector2(headWidth, btnHeight);
                    }
                }
            }
        }
    }
}
