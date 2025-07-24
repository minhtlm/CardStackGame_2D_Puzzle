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

    public void StartDealing()
    {
        StartCoroutine(DealAllSlots());
    }

    private IEnumerator DealAllSlots()
    {
        for (int i = 0; i < normalSlotCount; i++)
        {
            SlotConfig config = SlotManager.Instance.GenerateSlotConfig();
            yield return StartCoroutine(DealSlot(i, config));
        }
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

                card.transform.DOMove(targetPosition, dealDuration)
                    .SetEase(Ease.OutQuart)
                    .OnComplete(() => card.transform.SetParent(currentSlot));

                yield return new WaitForSeconds(dealDelay);
            }
        }
    }
}
