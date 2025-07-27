using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CardDealer : MonoBehaviour
{
    [SerializeField] public Transform[] slots;
    [SerializeField] public int normalSlotCount = 15; // Số lượng slot bài bình thường
    [SerializeField] public float stackOffset = 2f; // Khoảng cách giữa các lá bài trong slot
    [SerializeField] public float dealDuration = 0.5f; // Thời gian bay của mỗi lá bài
    [SerializeField] public float dealDelay = 0.1f; // Delay giữa các lá bài
    [SerializeField] public Transform cardParent;

    [SerializeField] private GameObject[] cardPrefabs;

    private bool isDealing = false;
    public bool IsDealing => isDealing;

    public void StartDealing()
    {
        if (isDealing) return;
        if (SlotManager.Instance.HasSelectedCards()) return;

        isDealing = true;

        StartCoroutine(DealAllSlots());
    }

    private IEnumerator DealAllSlots()
    {
        AudioManager.Instance.PlayDealSound(); // Phát âm thanh deal
        yield return new WaitForSeconds(0.3f); // Đợi một chút trước khi bắt đầu deal

        for (int i = 0; i < normalSlotCount; i++)
        {
            if (SlotManager.Instance.unlockedSlots[i])
            {
                SlotConfig config = SlotManager.Instance.GenerateSlotConfig();
                yield return StartCoroutine(DealSlot(i, config));
            }
        }

        isDealing = false;
    }

    private IEnumerator DealSlot(int slotIndex, SlotConfig config)
    {
        Transform currentSlot = slots[slotIndex];

        int currentCardCount = currentSlot.childCount;

        // Lấy thông tin stack từ config
        StackInfo[] stacks = config.stacks;

        // Deal từng stack trong slot
        for (int stackIndex = 0; stackIndex < stacks.Length; stackIndex++)
        {
            int colorIndex = stacks[stackIndex].colorIndex;
            int cardCount = stacks[stackIndex].cardCount;

            // Deal từng lá bài trong stack
            for (int cardIndex = 0; cardIndex < cardCount; cardIndex++)
            {
                GameObject card = Instantiate(cardPrefabs[colorIndex], cardParent);

                // Tính vị trí đích với offset để tạo hiệu ứng xếp chồng
                Vector3 targetPosition = currentSlot.position + Vector3.up * (currentCardCount * stackOffset);

                currentCardCount++; // Tăng số lượng lá bài trong slot

                AudioManager.Instance.PlayCardMoveSound(); // Phát âm thanh di chuyển lá bài

                card.transform.DOMove(targetPosition, dealDuration)
                    .SetEase(Ease.OutQuart)
                    .OnComplete(() => card.transform.SetParent(currentSlot));

                yield return new WaitForSeconds(dealDelay);
            }
        }
    }
}
