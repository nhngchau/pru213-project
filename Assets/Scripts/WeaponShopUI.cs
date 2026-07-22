using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class WeaponShopUI : MonoBehaviour
{
    [SerializeField] private TMP_Text dataPackText;
    [SerializeField] private Button closeButton;
    
    [Header("Weapon Item Template")]
    [SerializeField] private RectTransform contentRoot;

    private List<GameObject> spawnedItems = new List<GameObject>();

    public static GameObject CreateRuntimePanel(Transform parent)
    {
        GameObject panel = new GameObject("RuntimeWeaponShopPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image backdrop = panel.GetComponent<Image>();
        backdrop.color = new Color(0.02f, 0.04f, 0.08f, 0.92f);

        panel.AddComponent<WeaponShopUI>();
        return panel;
    }

    private void Awake()
    {
        EnsureRuntimeLayout();
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnDataPackChanged += HandleDataPackChanged;
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnDataPackChanged -= HandleDataPackChanged;
    }

    // DidPushEnter removed because we no longer inherit from Modal

    private void HandleDataPackChanged(int _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (dataPackText != null)
        {
            dataPackText.text = $"DataPack: {RunProgress.DataPack}";
        }

        foreach (var item in spawnedItems)
        {
            Destroy(item);
        }
        spawnedItems.Clear();

        if (WeaponManager.Instance == null) return;
        var weapons = WeaponManager.Instance.AllWeapons;
        if (weapons == null || weapons.Count == 0) return;

        float itemHeight = 80f;
        float startY = -60f;
        float spacing = 90f;

        for (int i = 0; i < weapons.Count; i++)
        {
            var weapon = weapons[i];
            bool isUnlocked = RunProgress.UnlockedWeapons.Contains(weapon.weaponID);
            bool isEquipped = RunProgress.EquippedWeaponID == weapon.weaponID;

            GameObject rowObj = new GameObject($"WeaponRow_{weapon.weaponID}", typeof(RectTransform), typeof(Image));
            rowObj.transform.SetParent(contentRoot, false);
            RectTransform rowRect = rowObj.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(-40f, itemHeight);
            rowRect.anchoredPosition = new Vector2(0f, startY - i * spacing);

            Image rowBg = rowObj.GetComponent<Image>();
            rowBg.color = isEquipped ? new Color(0.15f, 0.3f, 0.15f, 0.95f) : new Color(0.1f, 0.14f, 0.22f, 0.95f);

            // Icon
            if (weapon.weaponSprite != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(rowObj.transform, false);
                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.sizeDelta = new Vector2(60f, 60f);
                iconRect.anchoredPosition = new Vector2(40f, 0f);
                iconObj.GetComponent<Image>().sprite = weapon.weaponSprite;
                iconObj.GetComponent<Image>().preserveAspect = true;
            }

            // Title
            TMP_Text titleTxt = CreateText("Title", rowRect, weapon.weaponName, 24f, TextAlignmentOptions.Left);
            SetRect(titleTxt.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.6f, 1f), new Vector2(80f, -4f), new Vector2(-8f, -4f));

            // Stats
            string stats = $"DMG: {weapon.bulletDamage} | SPD: {weapon.fireRate} | SHT: {weapon.bulletsPerShot}";
            TMP_Text statsTxt = CreateText("Stats", rowRect, stats, 16f, TextAlignmentOptions.Left);
            statsTxt.color = new Color(0.7f, 0.7f, 0.7f);
            SetRect(statsTxt.rectTransform, new Vector2(0f, 0f), new Vector2(0.6f, 0.5f), new Vector2(80f, 4f), new Vector2(-8f, 4f));

            // Price / Status
            string statusStr = isEquipped ? "EQUIPPED" : (isUnlocked ? "OWNED" : $"{weapon.price} DP");
            TMP_Text statusTxt = CreateText("Status", rowRect, statusStr, 20f, TextAlignmentOptions.Center);
            SetRect(statusTxt.rectTransform, new Vector2(0.6f, 0f), new Vector2(0.75f, 1f), new Vector2(4f, 4f), new Vector2(-4f, -4f));

            // Action Button
            Button actionBtn = CreateButton("ActionButton", rowRect, isEquipped ? "UNEQUIP" : (isUnlocked ? "EQUIP" : "BUY"));
            SetRect((RectTransform)actionBtn.transform, new Vector2(0.76f, 0.2f), new Vector2(0.98f, 0.8f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            
            if (isEquipped)
            {
                actionBtn.interactable = false; // Cannot unequip, must equip another
            }
            else if (isUnlocked)
            {
                actionBtn.onClick.AddListener(() => OnEquipClicked(weapon.weaponID));
                actionBtn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f, 1f);
            }
            else
            {
                bool canBuy = RunProgress.DataPack >= weapon.price;
                actionBtn.interactable = canBuy;
                actionBtn.onClick.AddListener(() => OnBuyClicked(weapon.weaponID, weapon.price));
                actionBtn.GetComponent<Image>().color = canBuy ? new Color(0.13f, 0.42f, 0.9f, 1f) : new Color(0.42f, 0.44f, 0.5f, 1f);
            }

            spawnedItems.Add(rowObj);
        }
        
        // Adjust content size
        contentRoot.sizeDelta = new Vector2(contentRoot.sizeDelta.x, Mathf.Max(560f, startY * -1f + weapons.Count * spacing + 40f));
    }

    private void OnBuyClicked(string weaponID, int price)
    {
        if (RunProgress.SpendDataPack(price))
        {
            RunProgress.UnlockWeapon(weaponID);
            RunProgress.EquipWeapon(weaponID); // Auto equip on buy
            Refresh();
        }
    }

    private void OnEquipClicked(string weaponID)
    {
        RunProgress.EquipWeapon(weaponID);
        Refresh();
    }

    private void OnCloseClicked()
    {
        Destroy(gameObject);
    }

    private void EnsureRuntimeLayout()
    {
        RectTransform root = GetComponent<RectTransform>();
        if (root == null)
        {
            root = gameObject.AddComponent<RectTransform>();
        }

        if (contentRoot != null) return; // already setup

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        // Create main panel
        RectTransform panel = CreateRect("Panel", root);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(820f, 600f);
        panel.anchoredPosition = Vector2.zero;

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.09f, 0.16f, 0.98f);

        // Title
        TMP_Text title = CreateText("Title", panel, "WEAPON SHOP", 38f, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-48f, 60f));

        // DataPack
        dataPackText = CreateText("DataPackText", panel, string.Empty, 24f, TextAlignmentOptions.Left);
        SetRect(dataPackText.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(34f, -90f), new Vector2(-20f, 48f));

        // Scroll View Setup (Simplistic for now, using a container)
        contentRoot = CreateRect("Content", panel);
        SetRect(contentRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 80f), new Vector2(-20f, -100f));
        // Add mask? Just plain is okay for now if not overflowing massively, but we could add ScrollRect.
        // Actually, let's keep it simple.

        // Close Button
        closeButton = CreateButton("CloseButton", panel, "CLOSE");
        SetRect((RectTransform)closeButton.transform, new Vector2(0.35f, 0f), new Vector2(0.65f, 0f), new Vector2(0f, 20f), new Vector2(0f, 60f));
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = size;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.13f, 0.42f, 0.9f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText("Text", rect, label, 18f, TextAlignmentOptions.Center);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
