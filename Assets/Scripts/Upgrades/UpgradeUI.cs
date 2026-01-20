using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUI : MonoBehaviour
{
    [Header("Upgrade Buttons")]
    [SerializeField] private Button upgrade1Button;
    [SerializeField] private Button upgrade2Button;
    [SerializeField] private Button upgrade3Button;

    [Header("Upgrade Info")]
    [SerializeField] private TMP_Text upgrade1Name;
    [SerializeField] private TMP_Text upgrade2Name;
    [SerializeField] private TMP_Text upgrade3Name;

    [SerializeField] private TMP_Text upgrade1Description;
    [SerializeField] private TMP_Text upgrade2Description;
    [SerializeField] private TMP_Text upgrade3Description;

    [SerializeField] private Image upgrade1Icon;
    [SerializeField] private Image upgrade2Icon;
    [SerializeField] private Image upgrade3Icon;

    [Header("Title")]
    [SerializeField] private TMP_Text titleText;

    private string[] currentUpgradeNames;

    private void Awake()
    {
        // Button Listeners
        upgrade1Button.onClick.AddListener(() => OnUpgradeSelected(0));
        upgrade2Button.onClick.AddListener(() => OnUpgradeSelected(1));
        upgrade3Button.onClick.AddListener(() => OnUpgradeSelected(2));
    }

    public void ShowUpgrades(string[] upgradeNames)
    {
        currentUpgradeNames = upgradeNames;

        if (titleText != null)
            titleText.text = "Upgrade Time!";

        // Lade Upgrade Daten aus Resources und zeige an
        for (int i = 0; i < upgradeNames.Length && i < 3; i++)
        {
            UpgradeData upgrade = Resources.Load<UpgradeData>($"Upgrades/{upgradeNames[i]}");

            if (upgrade != null)
            {
                SetUpgradeUI(i, upgrade);
            }
            else
            {
                Debug.LogWarning($"Upgrade '{upgradeNames[i]}' nicht in Resources/Upgrades/ gefunden!");
            }
        }
    }

    private void SetUpgradeUI(int index, UpgradeData upgrade)
    {
        TMP_Text nameText = null;
        TMP_Text descriptionText = null;
        Image iconImage = null;

        switch (index)
        {
            case 0:
                nameText = upgrade1Name;
                descriptionText = upgrade1Description;
                iconImage = upgrade1Icon;
                break;
            case 1:
                nameText = upgrade2Name;
                descriptionText = upgrade2Description;
                iconImage = upgrade2Icon;
                break;
            case 2:
                nameText = upgrade3Name;
                descriptionText = upgrade3Description;
                iconImage = upgrade3Icon;
                break;
        }

        if (nameText != null)
            nameText.text = upgrade.upgradeName;

        if (descriptionText != null)
            descriptionText.text = upgrade.description;

        if (iconImage != null && upgrade.icon != null)
        {
            iconImage.sprite = upgrade.icon;
            iconImage.enabled = true;
        }
        else if (iconImage != null)
        {
            iconImage.enabled = false;
        }
    }

    private void OnUpgradeSelected(int index)
    {
        Debug.Log($"Upgrade {index + 1} ausgewählt!");

        // Deaktiviere alle Buttons (verhindert mehrfache Auswahl)
        upgrade1Button.interactable = false;
        upgrade2Button.interactable = false;
        upgrade3Button.interactable = false;

        // Rufe UpgradeManager auf
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.SelectUpgrade(index);
        }
    }

    private void OnDisable()
    {
        // Re-enable Buttons für nächstes Mal
        upgrade1Button.interactable = true;
        upgrade2Button.interactable = true;
        upgrade3Button.interactable = true;
    }
}