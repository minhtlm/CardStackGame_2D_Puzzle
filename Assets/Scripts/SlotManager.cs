using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct StackInfo
{
    public int colorIndex;     // Index màu (0-5)
    public int cardCount;      // Số lá trong stack này (2-4)
}

[System.Serializable] 
public struct SlotConfig
{
    public StackInfo[] stacks; // Tối đa 2 stacks
}

public class SlotManager : MonoBehaviour
{
    [SerializeField] private CardDealer cardDealer;
    [SerializeField] private float liftHeight = 20f;

    private int collectionTargetCount = 10; // Số lượng lá bài tối thiểu cần thu thập trong mỗi collection slot
    private int initialLockedSlots = 12; // Số lượng slot bị khóa ban đầu
    private int slotPriceStep = 5; // Bước tăng giá mở khóa slot
    private bool isMovingCards = false; // Biến để kiểm tra xem có đang di chuyển lá bài hay không
    private Tween shakeTween; // Tween lắc lá bài khi không hợp lệ
    private Transform selectedSlot = null;
    private List<GameObject> selectedCards = new List<GameObject>();

    public bool[] unlockedSlots = new bool[18]; // Mảng để theo dõi các slot đã mở khóa
    public static SlotManager Instance { get; private set; }

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
        // Mặc định mở khóa các slot từ initialLockedSlots đến hết
        for (int i = initialLockedSlots; i < unlockedSlots.Length; i++)
        {
            unlockedSlots[i] = true;
        }
    }

    public SlotConfig GenerateSlotConfig()
    {
        SlotConfig config = new SlotConfig();

        // Chọn ngẫu nhiên số lượng stacks (1-2)
        int stackCount = Random.Range(1, 3);
        config.stacks = new StackInfo[stackCount];

        for (int i = 0; i < stackCount; i++)
        {
            config.stacks[i] = new StackInfo
            {
                colorIndex = Random.Range(0, 6), // Chọn màu ngẫu nhiên từ 0-5
                cardCount = Random.Range(2, 5) // Số lá trong stack từ 2-4
            };
        }

        return config;
    }

    public void OnSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= cardDealer.slots.Length) return; // Kiểm tra chỉ số hợp lệ
        if (isMovingCards || cardDealer.IsDealing) return; // Nếu đang di chuyển lá bài hoặc đang deal, không cho phép chọn slot mới

        if (!unlockedSlots[slotIndex])
        {
            TryUnlockSlot(slotIndex); // Thử mở khóa slot nếu chưa mở
            return;
        }

        Transform slot = cardDealer.slots[slotIndex];

        if (selectedSlot == slot) // Nếu đã chọn slot này, bỏ chọn
        {
            DeselectSlot();
        }
        else if (selectedSlot != null && selectedCards.Count > 0) // Nếu có lá bài đang được chọn
        {
            if (CanMoveToSlot(slot))
            {
                // Di chuyển chúng đến slot mới nếu hợp lệ
                StartCoroutine(MoveCardsToSlot(slot, slotIndex));
            }
            else
            {
                ShakeSelectedCards(); // Lắc các lá bài đã chọn để báo lỗi
            }
        }
        else
        {
            if (selectedSlot != null) DeselectSlot(); // Bỏ chọn slot trước đó (nếu có)

            SelectTopCards(slot);
        }
    }

    public List<GameObject> GetCardsInSlot(Transform slot)
    {
        List<GameObject> cards = new List<GameObject>(slot.childCount); // Pre-allocate capacity
        for (int i = 0; i < slot.childCount; i++)
        {
            cards.Add(slot.GetChild(i).gameObject);
        }
        return cards;
    }

    public bool HasSelectedCards()
    {
        return selectedCards.Count > 0;
    }

    public void UpdateCurrentAffordablePair()
    {
        int currentUnlockableSlot = -1;
        for (int i = initialLockedSlots; i >= 0; i--)
        {
            if (!unlockedSlots[i])
            {
                currentUnlockableSlot = i;
                break;
            }
        }

        if (currentUnlockableSlot == -1) return; // Tất cả slots đã mở khóa

        int currentPairStart = (currentUnlockableSlot / 2) * 2; // Tính chỉ số đầu của cặp slot hiện tại)
        if (!unlockedSlots[currentPairStart]) UpdateAffordableSlotUI(currentPairStart);
        if (!unlockedSlots[currentPairStart + 1]) UpdateAffordableSlotUI(currentPairStart + 1);
    }

    void UpdateAffordableSlotUI(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= initialLockedSlots) return; // Kiểm tra chỉ số hợp lệ

        Transform slot = cardDealer.slots[slotIndex];
        if (slot == null || slot.childCount == 0) return;

        Transform lockOverlay = slot.GetChild(0);
        Image overlay = lockOverlay.GetComponent<Image>();
        if (overlay == null) return;

        int cost = (initialLockedSlots - slotIndex) * slotPriceStep; // Tính chi phí mở khóa

        if (MoneyManager.Instance.CanAfford(cost))
            overlay.color = new Color(0, 1, 0, 0.8f); // Đặt màu xanh cho overlay
        else
            overlay.color = new Color(0, 0, 0, 0.8f); // Đặt màu xám cho overlay nếu không đủ tiền
    }

    void SelectTopCards(Transform slot)
    {
        List<GameObject> cards = GetCardsInSlot(slot);
        if (cards.Count == 0) return;

        selectedCards = GetTopCardsOfSameColor(cards);

        if (selectedCards.Count > 0)
        {
            selectedSlot = slot;

            foreach (GameObject card in selectedCards)
            {
                // Nâng lá bài lên cao hơn
                LiftHeight(card);
            }
        }
        else
        {
            Debug.Log("No top cards of the same color found.");
        }
    }

    IEnumerator MoveCardsToSlot(Transform targetSlot, int slotIndex)
    {
        if (selectedSlot == null || selectedCards.Count == 0 || targetSlot == null) yield break;

        isMovingCards = true; // Đánh dấu là đang di chuyển lá bài

        int targetSlotCardCount = targetSlot.childCount;
        Transform capturedTargetSlot = targetSlot; // Lưu lại slot đích để tránh lỗi closure

        for (int i = 0; i < selectedCards.Count; i++)
        {
            GameObject card = selectedCards[i];
            card.transform.SetParent(cardDealer.cardParent, true); // Set parent tạm thời để card hiển thị trên cùng

            Vector3 targetPosition = targetSlot.position + Vector3.up * ((targetSlotCardCount + i) * cardDealer.stackOffset); // Tính vị trí đích

            GameObject capturedCard = card; // Lưu lại card để tránh lỗi closure

            AudioManager.Instance.PlayCardMoveSound(); // Phát âm thanh di chuyển lá bài

            card.transform.DOMove(targetPosition, cardDealer.dealDuration)
                .SetEase(Ease.OutQuart)
                .OnComplete(() => capturedCard.transform.SetParent(capturedTargetSlot));

            yield return new WaitForSeconds(cardDealer.dealDelay); // Delay giữa các lá bài
        }

        if (IsCollectionSlot(slotIndex))
        {
            yield return new WaitForSeconds(0.5f); // Chờ một chút để đảm bảo tất cả SetParent hoàn tất

            if (targetSlot.childCount >= collectionTargetCount)
            {
                yield return StartCoroutine(CheckAndCompleteCollection(targetSlot));
            }
        }

        ResetSelection(); // Reset trạng thái sau khi di chuyển
        isMovingCards = false; // Kết thúc quá trình di chuyển lá bài
    }

    IEnumerator CheckAndCompleteCollection(Transform collectionSlot)
    {
        List<GameObject> cards = GetCardsInSlot(collectionSlot);

        if (cards.Count >= collectionTargetCount)
        {
            // Hiển thị hiệu ứng hoàn thành
            foreach (GameObject card in cards)
            {
                card.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
            }

            yield return new WaitForSeconds(0.3f); // Chờ hiệu ứng biến mất hoàn thành

            MoneyManager.Instance.AddMoneyForCards(cards.Count); // Thêm tiền cho số lượng lá bài đã thu thập
            MoneyManager.Instance.AddingChipsEffect(); // Hiệu ứng thêm chip

            foreach (GameObject card in cards)
            {
                Destroy(card); // Xóa tất cả lá bài trong slot
            }
        }
    }

    bool CanMoveToSlot(Transform targetSlot)
    {
        // Kiểm tra xem có thể di chuyển lá bài đến slot này không
        if (selectedCards.Count == 0) return false;

        List<GameObject> targetCards = GetCardsInSlot(targetSlot);

        if (targetCards.Count == 0) return true; // Slot trống, có thể di chuyển

        int targetColor = GetCardColor(targetCards[targetCards.Count - 1]); // Lấy màu của lá bài trên cùng trong slot đích
        int selectedColor = GetCardColor(selectedCards[0]); // Lấy màu của lá bài đầu tiên trong danh sách đã chọn

        return targetColor == selectedColor; // Chỉ cho phép di chuyển nếu màu giống nhau
    }

    List<GameObject> GetTopCardsOfSameColor(List<GameObject> cards)
    {
        List<GameObject> topCards = new List<GameObject>();

        if (cards == null || cards.Count == 0) return topCards;

        // Lấy màu của lá bài trên cùng
        int topCardColor = GetCardColor(cards[cards.Count - 1]);
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            GameObject card = cards[i];

            if (GetCardColor(card) == topCardColor)
            {
                topCards.Insert(0, card); // Thêm vào đầu danh sách để giữ thứ tự từ trên xuống dưới
            }
            else
            {
                break; // Dừng lại khi gặp lá bài khác màu
            }
        }

        return topCards;
    }

    int GetCardColor(GameObject card)
    {
        string cardName = card.name;

        if (cardName.Contains("blue")) return 0;
        if (cardName.Contains("green")) return 1;
        if (cardName.Contains("orange")) return 2;
        if (cardName.Contains("purple")) return 3;
        if (cardName.Contains("red")) return 4;
        if (cardName.Contains("yellow")) return 5;

        return -1; // Không xác định màu
    }

    bool IsCollectionSlot(int slotIndex)
    {
        // Kiểm tra xem slot có phải là slot thu thập hay không
        return slotIndex >= cardDealer.normalSlotCount; // Slot 15, 16, 17 là slot thu thập
    }

    bool CanUnlockSlot(int slotIndex)
    {
        int higherPairStart = ((slotIndex / 2) + 1) * 2; // Tính chỉ số của cặp slot cao hơn
        return unlockedSlots[higherPairStart] && unlockedSlots[higherPairStart + 1]; // Kiểm tra xem slot trước đó đã mở khóa
    }

    void DeselectSlot()
    {
        if (selectedSlot != null && selectedCards.Count > 0)
        {
            foreach (GameObject card in selectedCards)
            {
                // Hạ lá bài xuống vị trí ban đầu
                LowerHeight(card);
            }

            ResetSelection();
        }
    }

    void ResetSelection()
    {
        selectedCards.Clear();
        selectedSlot = null;
    }

    void LiftHeight(GameObject card)
    {
        card.transform.position += Vector3.up * liftHeight;
    }

    void LowerHeight(GameObject card)
    {
        card.transform.position -= Vector3.up * liftHeight;
    }

    void ShakeSelectedCards()
    {
        if (selectedCards.Count == 0) return;

        if (shakeTween != null && shakeTween.IsActive())
        {
            return; // Nếu đang lắc lá bài, không làm gì thêm
        }

        isMovingCards = true; // Đánh dấu ngăn thao tác khi đang lắc lá bài

        Sequence shakeSequence = DOTween.Sequence();

        foreach (GameObject card in selectedCards)
        {
            shakeSequence.Join(card.transform.DOShakePosition(0.5f, 15f, 10, 90, false, true));
        }

        shakeTween = shakeSequence; // Lưu tham chiếu đến sequence

        shakeTween.OnComplete(() =>
        {
            shakeTween = null; // Đặt lại tween sau khi hoàn thành
            isMovingCards = false; // Kết thúc quá trình lắc lá bài
        });
    }

    void TryUnlockSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= initialLockedSlots) return; // Kiểm tra chỉ số hợp lệ 0-12

        if (cardDealer.IsDealing)
        {
            Debug.LogWarning("Cannot unlock slot while dealing cards.");
            return; // Không thể mở khóa khi đang deal
        }

        if (!unlockedSlots[slotIndex])
        {
            if (!CanUnlockSlot(slotIndex))
            {
                Debug.Log($"Cannot unlock slot {slotIndex}. Must unlock previous slots first.");
                return; // Không thể mở khóa nếu các slot trước chưa mở
            }

            int cost = (initialLockedSlots - slotIndex) * slotPriceStep; // Chi phí mở khóa tăng dần 5 cho mỗi slot thêm

            if (MoneyManager.Instance.SpendMoney(cost)) // Nêu có đủ tiền, mở khóa slot
            {
                Transform slot = cardDealer.slots[slotIndex];
                if (slot != null && slot.childCount > 0)
                {
                    AudioManager.Instance.PlayUnlockSlotSound(); // Phát âm thanh mở khóa slot

                    unlockedSlots[slotIndex] = true;
                    Destroy(slot.GetChild(0).gameObject); // Xóa LockOverlay

                    int currentPairStart = (slotIndex / 2) * 2; // Tính chỉ số của cặp slot hiện tại
                    if (unlockedSlots[currentPairStart] && unlockedSlots[currentPairStart + 1]) // Nếu cả hai slot trong cặp đã mở khóa
                    {
                        // Cập nhật UI cho cặp slot tiếp theo
                        UpdateUnlockableSlotsUI(currentPairStart - 1);
                        UpdateUnlockableSlotsUI(currentPairStart - 2);
                        Debug.Log("Update next pair slots UI");
                    }
                    else if (unlockedSlots[currentPairStart + 1]) // Nếu chỉ có slot bên phải đã mở khóa
                    {
                        UpdateAffordableSlotUI(currentPairStart);
                        Debug.Log("Update left slot UI");
                    }
                    else // Nếu chỉ có slot bên trái đã mở khóa
                    {
                        UpdateAffordableSlotUI(currentPairStart + 1);
                        Debug.Log("Update right slot UI");
                    }
                }
            }
            else
            {
                Debug.Log("Not enough money to unlock slot " + slotIndex);
            }
        }
        else
        {
            Debug.Log($"Slot {slotIndex} is already unlocked.");
        }
    }

    void UpdateUnlockableSlotsUI(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= initialLockedSlots) return; // Kiểm tra chỉ số hợp lệ

        Transform slot = cardDealer.slots[slotIndex];
        if (slot == null || slot.childCount == 0) return;

        Transform lockOverlay = slot.GetChild(0);
        TMPro.TextMeshProUGUI costText = lockOverlay.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();

        if (costText != null) costText.text = ((initialLockedSlots - slotIndex) * slotPriceStep).ToString(); // Cập nhật giá tiền
        UpdateAffordableSlotUI(slotIndex); // Cập nhật UI overlay cho slot này
    }
}
