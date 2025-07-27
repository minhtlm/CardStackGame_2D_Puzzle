using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip dealClip;
    [SerializeField] private AudioClip cardMoveClip;
    [SerializeField] private AudioClip chipSpawnClip;
    [SerializeField] private AudioClip unlockSlotClip;

    public static AudioManager Instance { get; private set; }

    void Awake()
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

    public void PlayDealSound() => PlaySFX(dealClip);
    public void PlayCardMoveSound() => PlaySFX(cardMoveClip, 0.25f);
    public void PlayChipSpawnSound() => PlaySFX(chipSpawnClip, 0.5f);
    public void PlayUnlockSlotSound() => PlaySFX(unlockSlotClip, 0.7f);

    private void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f); // Thêm hiệu ứng pitch ngẫu nhiên
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
