using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MoneyManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText; // Text hiển thị số tiền
    [SerializeField] private Transform chipParent; // Nơi spawn chips
    [SerializeField] private Transform chipPosition; // Target vị trí chip bay đến
    [SerializeField] private GameObject chipPrefab;
    private float textTweenDuration = 1.5f; // Thời gian cập nhật tiền
    private int moneyPerCard = 1; // Số tiền nhận được cho mỗi lá bài
    private int totalMoney = 0; // Tổng số tiền hiện có
    private int chipNum = 10; // Số lượng chip mỗi lần spawn

    public static MoneyManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        moneyText.text = totalMoney.ToString();
    }

    private void UpdateMoneyUI(int startValue)
    {
        if (moneyText != null)
        {
            DOTween.To(
                getter: () => startValue,
                setter: value => moneyText.text = value.ToString(),
                endValue: totalMoney,
                duration: textTweenDuration
            ).SetEase(Ease.InOutQuad);

            moneyText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 2, 0.5f);
        }
    }

    private IEnumerator SpawnChips()
    {
        for (int i = 0; i < chipNum; i++)
        {
            GameObject chip = Instantiate(chipPrefab, chipParent);

            // Spawn chip ở vị trí ngẫu nhiên trong phạm vi quanh chipParent
            Vector2 randomOffset = Random.insideUnitCircle * 50f; // Vùng spawn ngẫu nhiên
            chip.transform.position = (Vector2)chipParent.transform.position + randomOffset;

            AudioManager.Instance.PlayChipSpawnSound(); // Phát âm thanh spawn chip

            chip.transform.DOMove(chipPosition.position, 0.5f)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => Destroy(chip));
                
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void AddingChipsEffect()
    {
        StartCoroutine(SpawnChips());
    }

    public void AddMoneyForCards(int cardCount)
    {
        int startValue = totalMoney;
        int totalEarned = cardCount * moneyPerCard;
        totalMoney += totalEarned;

        UpdateMoneyUI(startValue);

        SlotManager.Instance.UpdateCurrentAffordablePair();
    }

    public bool SpendMoney(int amount)
    {
        if (CanAfford(amount))
        {
            int startValue = totalMoney;
            totalMoney -= amount;
            UpdateMoneyUI(startValue);
            return true;
        }
        else
        {
            Debug.Log("Not enough money.");
            return false;
        }
    }

    public bool CanAfford(int amount)
    {
        return totalMoney >= amount;
    }
}
