using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Visual controller for a command action button.
/// Configures the button appearance, availability state,
/// icon and click callback based on a CommandAction.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class UIActionButton : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI hotkeyText;

    [Header("Background Textures (Square)")]
    [SerializeField] private Sprite squareStandardSprite;
    [SerializeField] private Sprite squareSecondarySprite;
    [SerializeField] private Sprite squareWarningSprite;
    [SerializeField] private Sprite squareDisabledSprite;

    [Header("Background Textures (Arrow / Rectangular)")]
    [SerializeField] private Sprite arrowNormalSprite;
    [SerializeField] private Sprite arrowDisabledSprite;

    private Button button;
    private Image buttonImage;
    private TextMeshProUGUI fallbackLabel;

    /// <summary>
    /// Shows <paramref name="text"/> centered on the button when the action has no icon
    /// (so icon-less commands stay distinguishable). Pass null/empty to hide it.
    /// The label is created lazily the first time text is needed.
    /// </summary>
    private void UpdateFallbackLabel(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            if (fallbackLabel != null) fallbackLabel.gameObject.SetActive(false);
            return;
        }

        if (fallbackLabel == null)
        {
            var labelObject = new GameObject("FallbackLabel", typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);
            var rect = (RectTransform)labelObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);

            fallbackLabel = labelObject.AddComponent<TextMeshProUGUI>();
            fallbackLabel.alignment = TextAlignmentOptions.Center;
            fallbackLabel.enableAutoSizing = true;
            fallbackLabel.fontSizeMin = 8f;
            fallbackLabel.fontSizeMax = 18f;
            fallbackLabel.color = Color.white;
            fallbackLabel.raycastTarget = false;
        }

        fallbackLabel.text = text;
        fallbackLabel.gameObject.SetActive(true);
    }

    /// <summary>
    /// Caches required component references.
    /// </summary>
    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Configures the button based on the supplied action.
    /// Updates icon, background sprite, interactability
    /// and click callback.
    /// </summary>
    /// <param name="action">Command action data.</param>
    public void Setup(CommandAction action)
    {
        bool isAvailable =
            action.isInteractable?.Invoke() ?? true;

        // Update action icon
        if (iconImage != null)
        {
            iconImage.sprite = action.icon;
            iconImage.gameObject.SetActive(action.icon != null);
        }

        UpdateFallbackLabel(action.icon == null ? action.displayName : null);

        // Update background sprite
        buttonImage.color = Color.white;
        buttonImage.sprite =
            GetBackgroundSprite(action.buttonType, isAvailable);

        // Update interactability
        button.interactable = isAvailable;

        // Configure click callback
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            action.actionCallback?.Invoke();
        });
    }

    /// <summary>
    /// Returns the appropriate background sprite
    /// according to button type and availability state.
    /// </summary>
    /// <param name="type">Action button category.</param>
    /// <param name="isAvailable">
    /// Indicates whether the action can currently be used.
    /// </param>
    /// <returns>The sprite that should be displayed.</returns>
    private Sprite GetBackgroundSprite(
        EActionButtonType type,
        bool isAvailable)
    {
        // Return disabled sprite when the action is unavailable
        if (!isAvailable)
        {
            return type == EActionButtonType.Navigation
                ? arrowDisabledSprite
                : squareDisabledSprite;
        }

        // Return sprite matching the action type
        return type switch
        {
            EActionButtonType.Secondary => squareSecondarySprite,
            EActionButtonType.Warning => squareWarningSprite,
            EActionButtonType.Navigation => arrowNormalSprite,
            _ => squareStandardSprite
        };
    }

    /// <summary>
    /// Clears and empties this slot without disabling the GameObject, maintaining Grid layout structure.
    /// </summary>
    public void Clear()
    {
        //gameObject.SetActive(true); // Must stay active so GridLayoutGroup preserves space!

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.gameObject.SetActive(false);
        }

        UpdateFallbackLabel(null);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = false;
            buttonImage.color = Color.clear;
        }
    }
}