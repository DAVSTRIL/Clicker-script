using System.Collections;
using UnityEngine;
using TMPro;
using System.Globalization;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using YG;

public class ChangingHouses : MonoBehaviour
{
    [SerializeField] private Button leftBtn, nextBtn, clicker, buyLock, upgradeButton, adsButton;
    [SerializeField] private TMP_Text moneyTxt, upgradeText, AdText;
    [SerializeField] private int[] houseCosts, upgradeCosts, clickUpgrades;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private ParticleSystem clickParticles;
    [SerializeField] private Sprite[] clickSprites;
    private const int rewardAmount = 5000;
    private AudioSource audioSource;
    private int clickValue = 1, upgradeLevel = 0, currentHouse;
    private bool[] housesPurchased;
    [SerializeField] private int money;
    public int language;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        InitializeSavesData();
        LoadProgress();
    }
    
    private void InitializeSavesData()
    {
        if (YandexGame.savesData == null)
        {
            YandexGame.savesData = new YG.SavesYG();
            YandexGame.savesData.housesPurchased = new bool[houseCosts.Length];
        }
    }

    public void RussianLanguage()
    {
        language = 0;
        PlayerPrefs.SetInt("language", language);
        SceneManager.LoadScene("SampleScene");
    }

    public void EnglishLanguage()
    {
        language = 1;
        PlayerPrefs.SetInt("language", language);
        SceneManager.LoadScene("SampleScene");
    }

    private void Start()
    {
        language = PlayerPrefs.GetInt("language", language); // загружаем язык
        SelectHouse(0);
        UpdateUI();
        upgradeButton.onClick.AddListener(UpgradeClick);
        adsButton.onClick.AddListener(() => ShowAd(1));
        ClearUpgradeTextIfMaxed();
    }

    public void ShowAd(int id)
    {
        YandexGame.RewardVideoEvent += OnAdReward;
        YandexGame.RewardVideoShow(id);
    }

    private void OnAdReward(int id, bool adShownSuccessfully)
    {
        if (id == 1)
        {
            if (adShownSuccessfully)
            {
                RewardPlayer();
            }
            else
            {
                if (language == 0)
                {
                    AdText.text = "Деньги будут зачислены только после просмотра рекламы!";
                }
                else if (language == 1)
                {
                    AdText.text = "The money will be credited only after viewing the advertisement!";
                }
                StartCoroutine(ClearTextAfterDelay(AdText, 3));
            }
        }
        YandexGame.RewardVideoEvent -= OnAdReward;
    }

    public void RewardPlayer()
    {
        money += rewardAmount;
        UpdateMoneyText();
        SaveProgress();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && housesPurchased[currentHouse])
        {
            Btn_Click();
        }
    }

    private void ClearUpgradeTextIfMaxed()
    {
        if (!upgradeButton.interactable)
            upgradeText.text = "";
    }

    private bool isUpgradeInProgress = false;

    public void UpgradeClick()
    {
        if (isUpgradeInProgress)
        {
            return;
        }

        Debug.Log("Upgrade button clicked");

        if (upgradeLevel >= upgradeCosts.Length)
        {
            if (language == 0)
            {
                upgradeText.text = "Вы приобрели все улучшения";
            }
            else if (language == 1)
            {
                upgradeText.text = "You have purchased all the improvements";
            }
            upgradeButton.interactable = false;
            Debug.Log("All upgrades purchased");
            return;
        }

        int cost = upgradeCosts[upgradeLevel];
        Debug.Log($"Current upgrade level: {upgradeLevel}, Upgrade cost: {cost}, Money available: {money}");

        if (money >= cost)
        {
            money -= cost;
            clickValue = clickUpgrades[upgradeLevel++];
            Debug.Log($"Upgrade successful. New click value: {clickValue}, New upgrade level: {upgradeLevel}");

            // Сразу показываем текст об улучшении
            ShowUpgrade(clickValue);

            // Сохраняем прогресс и обновляем интерфейс
            SaveProgress();
            UpdateUI();

            // Блокируем кнопку улучшения на 5 секунд
            isUpgradeInProgress = true;
            StartCoroutine(BlockUpgradeButton(5));

            // Проверка, если достигли максимального уровня улучшений
            if (upgradeLevel >= upgradeCosts.Length)
            {
                if (language == 0)
                {
                    upgradeText.text = "Вы приобрели все улучшения";
                }
                else if (language == 1)
                {
                    upgradeText.text = "You have purchased all the improvements";
                }
                upgradeButton.interactable = false;
                Debug.Log("Reached maximum upgrade level");
            }
        }
        else
        {
            ShowInsufficientFunds(cost);
        }
    }

    private void ShowInsufficientFunds(int cost)
    {
        int neededMoney = cost - money;
        if (language == 0)
        {
            upgradeText.text = $"У вас недостаточно денег на улучшения, вам нужно ещё {neededMoney}";
        }
        else if (language == 1)
        {
            upgradeText.text = $"You don't have enough money for improvements, you need {neededMoney} more";
        }
        Debug.Log($"Insufficient funds: need {neededMoney} more");
        StartCoroutine(ClearTextAfterDelay(upgradeText, 3));
    }

    private void ShowUpgrade(int upgradeAmount)
    {
        upgradeText.text = $"+{upgradeAmount}";
        Canvas.ForceUpdateCanvases();
        Debug.Log($"Upgrade text shown: +{upgradeAmount}");
        StartCoroutine(ClearTextAfterDelay(upgradeText, 3));
    }

    private IEnumerator BlockUpgradeButton(float delay)
    {
        upgradeButton.interactable = false;
        yield return new WaitForSeconds(delay);
        upgradeButton.interactable = true;
        isUpgradeInProgress = false;
    }

    private IEnumerator ClearTextAfterDelay(TMP_Text textComponent, float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Clearing text after delay");
        textComponent.text = "";
    }

    public void Btn_Click()
    {
        money += clickValue;
        audioSource.PlayOneShot(clickSound);
        EmitClickParticles();
        SaveProgress();
        UpdateMoneyText();
    }

    private void EmitClickParticles()
    {
        var particleRenderer = clickParticles.GetComponent<ParticleSystemRenderer>();

        if (upgradeLevel < clickSprites.Length && clickSprites[upgradeLevel] != null)
        {
            Texture2D texture = clickSprites[upgradeLevel].texture;
            if (texture != null)
            {
                Material material = new Material(particleRenderer.sharedMaterial)
                {
                    mainTexture = texture
                };
                particleRenderer.sharedMaterial = material;
                particleRenderer.sharedMaterial.color = Color.white;
                clickParticles.Emit(1);
            }
        }
    }

    public void ResetProgress()
    {
        money = 100;
        upgradeLevel = 0;
        clickValue = 1;
        housesPurchased = new bool[houseCosts.Length];

        SaveAllPrefs();
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
        if (housesPurchased == null)
        {
            Debug.LogError("housesPurchased array not initialized!");
            return;
        }

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
                    spriteRenderer.color = isPurchased ? Color.white : Color.black;
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
        if (PlayerPrefs.GetInt("firstLaunch", 1) == 1)
        {
            ResetProgress();
        }
        else
        {
            YandexGame.LoadProgress(houseCosts.Length);
            money = YandexGame.savesData.money;
            upgradeLevel = YandexGame.savesData.upgradeLevel;
            clickValue = YandexGame.savesData.clickValue;
            housesPurchased = YandexGame.savesData.housesPurchased;
            UpdateUI();
        }
    }

    private void SaveProgress()
    {
        SaveAllPrefs();
        YandexGame.savesData.money = money;
        YandexGame.savesData.upgradeLevel = upgradeLevel;
        YandexGame.savesData.clickValue = clickValue;
        YandexGame.savesData.housesPurchased = housesPurchased;
        YandexGame.SaveProgress();
    }

    private void SaveAllPrefs()
    {
        PlayerPrefs.SetInt("money", money);
        PlayerPrefs.SetInt("upgradeLevel", upgradeLevel);
        PlayerPrefs.SetInt("clickValue", clickValue);
        for (int i = 0; i < housesPurchased.Length; i++)
        {
            PlayerPrefs.SetInt("house" + i, housesPurchased[i] ? 1 : 0);
        }
        PlayerPrefs.SetInt("firstLaunch", 0);
        PlayerPrefs.Save();
    }
}
