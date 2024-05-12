/*
----             DAVSTRIL COMPANY                ----   
*/

using System.Collections;
using UnityEngine;
using TMPro;
using System.Globalization;
using UnityEngine.UI;

public class ChangingHouses : MonoBehaviour
{
    [SerializeField] private Button leftBtn, nextBtn, clicker, buyLock, upgradeButton;
    [SerializeField] private TMP_Text moneyTxt, upgradeText;
    [SerializeField] private int money;
    [SerializeField] private int[] houseCosts, upgradeCosts, clickUpgrades;
    [SerializeField] private AudioClip clickSound;
    private AudioSource audioSource;
    private int clickValue = 1, upgradeLevel = 0, currentHouse;
    private bool[] housesPurchased;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        LoadProgress();
    }

    private void Start()
    {
        SelectHouse(0);
        UpdateUI();
        upgradeButton.onClick.AddListener(UpgradeClick);
        ClearUpgradeTextIfMaxed();
    }

    private void Update()
{
    if (Input.GetKeyDown(KeyCode.Space))
    {
        Btn_Click();
    }
}

    private void ClearUpgradeTextIfMaxed()
    {
        if (!upgradeButton.interactable)
            upgradeText.text = "";
    }

    public void UpgradeClick()
    {
        if (upgradeLevel >= upgradeCosts.Length)
        {
            upgradeText.text = "Вы приобрели все улучшения";
            return;
        }

        int cost = upgradeCosts[upgradeLevel];
        if (money < cost)
        {
            ShowInsufficientFunds(cost);
            return;
        }

        PurchaseUpgrade(cost);
    }

    private void PurchaseUpgrade(int cost)
    {
        money -= cost;
        clickValue = clickUpgrades[upgradeLevel++];
        StartCoroutine(ShowUpgrade(clickValue));
        SaveProgress();
        UpdateUI();

        if (upgradeLevel == upgradeCosts.Length)
        {
            upgradeText.text = "Вы приобрели все улучшения";
            upgradeButton.interactable = false;
            PlayerPrefs.SetInt("allUpgradesPurchased", 1);
        }
    }

private void ShowInsufficientFunds(int cost)
{
    int neededMoney = cost - money;
    if (neededMoney > 0)
    {
        upgradeText.text = $"Вам не хватает денег для улучшений, вам осталось {neededMoney}";
    }
    else
    {
        upgradeText.text = $"Улучшение на {clickUpgrades[upgradeLevel]}";
    }
    StartCoroutine(ClearUpgradeText());
}

    private IEnumerator ShowUpgrade(int upgradeAmount)
    {
        upgradeText.text = $"+{upgradeAmount}";
        yield return new WaitForSeconds(2);
        upgradeText.text = "";
    }

    private IEnumerator ClearUpgradeText()
    {
        yield return new WaitForSeconds(2);
        upgradeText.text = "";
    }

    public void Btn_Click()
    {
        money += clickValue;
        audioSource.PlayOneShot(clickSound); 
        SaveProgress();
        UpdateMoneyText();
    }

    public void ResetProgress()
    {
        PlayerPrefs.SetInt("money", 100);
        PlayerPrefs.SetInt("upgradeLevel", 0); 
        PlayerPrefs.SetInt("clickValue", 1); 
        for (int i = 0; i < housesPurchased.Length; i++)
        {
            PlayerPrefs.SetInt("house" + i, 0);
        }
        PlayerPrefs.Save();

        money = 100;
        upgradeLevel = 0; 
        clickValue = 1; 
        for (int i = 0; i < housesPurchased.Length; i++)
        {
            housesPurchased[i] = false;
        }
        upgradeButton.interactable = true; 
        UpdateUI(); 
    }

    public void Buy()
    {
        int cost = houseCosts[currentHouse];
        bool previousHousePurchased = currentHouse == 0 || housesPurchased[currentHouse - 1];
        if (money >= cost && !housesPurchased[currentHouse] && previousHousePurchased)
        {
            money -= cost;
            housesPurchased[currentHouse] = true;
            SaveProgress();
            UpdateUI(); 
        }
    }

    public void ChangeHouse(int change)
    {
        int newHouseIndex = currentHouse + change;
        if (newHouseIndex >= 0 && newHouseIndex < houseCosts.Length)
        {
            SelectHouse(newHouseIndex);
        }
    }

    private void UpdateUI()
    {
        UpdateMoneyText();
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool isPurchased = housesPurchased[currentHouse];
        buyLock.gameObject.SetActive(!isPurchased);
        clicker.gameObject.SetActive(isPurchased);
        buyLock.interactable = money >= houseCosts[currentHouse] && !isPurchased && (currentHouse == 0 || housesPurchased[currentHouse - 1]);

        Transform houseGroupTransform = transform.GetChild(currentHouse);
        if (houseGroupTransform != null)
        {
            Transform houseTransform = houseGroupTransform.Find("house" + (currentHouse + 1).ToString());
            if (houseTransform != null)
            {
                SpriteRenderer spriteRenderer = houseTransform.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    Color houseColor = isPurchased ? Color.white : Color.black;
                    spriteRenderer.color = houseColor;
                }
            }
        }
    }

    private void UpdateMoneyText()
    {
        moneyTxt.text = money.ToString("#,0", CultureInfo.InvariantCulture);
    }

    private void SelectHouse(int idx)
    {
        currentHouse = idx;
        leftBtn.interactable = (idx != 0);
        nextBtn.interactable = (idx != transform.childCount - 1);
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(i == idx);
        }
        UpdateButtons(); 
    }

    private void LoadProgress()
    {
        money = PlayerPrefs.GetInt("money", 100); 
        upgradeLevel = PlayerPrefs.GetInt("upgradeLevel", 0); 

        if (upgradeLevel >= 0 && upgradeLevel < clickUpgrades.Length)
        {
            clickValue = clickUpgrades[upgradeLevel]; 
        }
        else
        {
            upgradeLevel = clickUpgrades.Length - 1;
            clickValue = clickUpgrades[upgradeLevel];
        }

        if (PlayerPrefs.GetInt("allUpgradesPurchased", 0) == 1)
        {
            upgradeText.text = "Вы приобрели все улучшения";
            upgradeButton.interactable = false;
        }

        housesPurchased = new bool[houseCosts.Length];
        for (int i = 0; i < houseCosts.Length; i++)
        {
            housesPurchased[i] = PlayerPrefs.GetInt("house" + i, 0) == 1;
        }
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt("money", money);
        PlayerPrefs.SetInt("upgradeLevel", upgradeLevel);
        for (int i = 0; i < housesPurchased.Length; i++)
        {
            PlayerPrefs.SetInt("house" + i, housesPurchased[i] ? 1 : 0);
        }
        PlayerPrefs.Save(); 
    }
}
