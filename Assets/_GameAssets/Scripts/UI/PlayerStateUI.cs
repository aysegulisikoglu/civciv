using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class PlayerStateUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController _playerController; //PlayerController bileşeni. Oyuncunun kontrolünü yöneten script.
    [SerializeField] private RectTransform _playerWalkingTransform;
    [SerializeField] private RectTransform _playerSlidingTransform;
    [SerializeField] private RectTransform _boosterSpeedTransform; //Booster hızını gösteren UI transformu. (Kullanılmıyor gibi görünüyor, ama belki ileride kullanılabilir.)
    [SerializeField] private RectTransform _boosterJumpTransform; //Booster zıplamasını gösteren UI transformu. (Kullanılmıyor gibi görünüyor, ama belki ileride kullanılabilir.)
    [SerializeField] private RectTransform _boosterSlowTransform; //Booster yavaşlamasını gösteren UI transformu. (Kullanılmıyor gibi görünüyor, ama belki ileride kullanılabilir.)
    [SerializeField] private PlayableDirector _playableDirector; //TimelineManager'dan gelen PlayableDirector. Timeline'ı kontrol etmek için kullanılır.
    [Header("Images")]
    [SerializeField] private Image _goldBoosterWheatImage; //Altın booster buğday resmini gösteren UI resmi. (Kullanılmıyor gibi görünüyor, ama belki ileride kullanılabilir.)
    [SerializeField] private Image _hollyBoosterWheatImage;
    [SerializeField] private Image _rottenBoosterWheatImage;
    [Header("Sprites")]
    [SerializeField] private Sprite _playerWalkingActiveSprite;
    [SerializeField] private Sprite _playerWalkingPassiveSprite;
    [SerializeField] private Sprite _playerSlidingActiveSprite;
    [SerializeField] private Sprite _playerSlidingPassiveSprite;
    [Header("Settings")]
    [SerializeField] private float _moveDuration; //Animasyonun süresi.
    [SerializeField] private Ease _moveEase; //Animasyonun geçiş tipi.

    public RectTransform GetBoosterSpeedTransform() => _boosterSpeedTransform; //Booster hız transformunu döndürür.
    public RectTransform GetBoosterJumpTransform() => _boosterJumpTransform; //Booster zıplama transformunu döndürür.
    public RectTransform GetBoosterSlowTransform() => _boosterSlowTransform; //Booster yavaşlama transformunu döndürür.
    public Image GetGoldBoosterWheatImage() => _goldBoosterWheatImage; //Altın booster buğday resmini döndürür.
    public Image GetHollyBoosterWheatImage() => _hollyBoosterWheatImage; //Holly booster buğday resmini döndürür.
    public Image GetRottenBoosterWheatImage() => _rottenBoosterWheatImage; //Rotten booster buğday resmini döndürür.

    private Image _playerWalkingImage; //Oyuncunun yürüyüş durumunu gösteren UI resmi.
    private Image _playerSlidingImage; //Oyuncunun kayma durumunu gösteren UI resmi.
    private void Awake()
    {
        _playerWalkingImage = _playerWalkingTransform.GetComponent<Image>(); //_playerWalkingTransform'dan Image bileşeni alınır.
        _playerSlidingImage = _playerSlidingTransform.GetComponent<Image>(); //_playerSlidingTransform'dan Image bileşeni alınır.
    }

    private void Start()
    {
        _playerController.OnPlayerStateChanged += PlayerController_OnPlayerStateChanged; //PlayerController'daki OnPlayerStateChanged olayına abone olunur. Oyuncu durumu değiştiğinde tetiklenir.
        _playableDirector.stopped += OnTimelineFinished; //TimelineManager'daki timeline bittiğinde tetiklenir.



    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        SetStateUserInterface(_playerWalkingActiveSprite, _playerSlidingPassiveSprite, _playerWalkingTransform, _playerSlidingTransform); //Başlangıçta yürüyüş durumu ayarlanır.
    }

    private void PlayerController_OnPlayerStateChanged(PlayerState playerState)
    {
        switch (playerState)
        {
            case PlayerState.Idle:
            case PlayerState.Move:
                //üstteki kart açılacak
                SetStateUserInterface(_playerWalkingActiveSprite, _playerSlidingPassiveSprite, _playerWalkingTransform, _playerSlidingTransform); //Yürüyüş durumunu ayarlar.

                break;
            case PlayerState.SlideIdle:
            case PlayerState.Slide:
                //alttaki kart açılacak
                SetStateUserInterface(_playerWalkingPassiveSprite, _playerSlidingActiveSprite, _playerSlidingTransform, _playerWalkingTransform); //Kayma durumunu ayarlar.
                break;
        }


    }
    private void SetStateUserInterface(Sprite playerWalkingSprite, Sprite playerSlidingSprite, RectTransform activeTransform, RectTransform passiveTransform)
    {
        _playerWalkingImage.sprite = playerWalkingSprite; //Yürüyüş durumundaki sprite ayarlanır.
        _playerSlidingImage.sprite = playerSlidingSprite; //Kayma durumundaki sprite ayarlanır.

        activeTransform.DOAnchorPosX(-25f, _moveDuration).SetEase(_moveEase); //Aktif transformun X pozisyonu -25'e animasyonla taşınır.
        passiveTransform.DOAnchorPosX(-90f, _moveDuration).SetEase(_moveEase); //Pasif transformun X pozisyonu -90'a animasyonla taşınır.
    }

    private IEnumerator SetBoosterUserInterfaces(RectTransform activeTransform, Image boosterImage, Image wheatImage, Sprite activeSprite, Sprite passiveSprite, Sprite activeWheatSprite, Sprite passiveWheatSprite, float duration) // normalde bu kadar değer/parametre verilmemli bi kere kullanacağımız için çok değer verdik  normalde 3 ü geçmemek lazım. daha temiz yazılabilirdi
    {
        boosterImage.sprite = activeSprite; //Booster resminin sprite'ı aktif sprite olarak ayarlanır.
        wheatImage.sprite = activeWheatSprite; //Buğday resminin sprite'ı aktif buğday sprite olarak ayarlanır.

        activeTransform.DOAnchorPosX(25f, _moveDuration).SetEase(_moveEase); //Aktif transformun X pozisyonu 25'e animasyonla taşınır.

        yield return new WaitForSeconds(duration); //Belirtilen süre kadar beklenir.

        boosterImage.sprite = passiveSprite; //Booster resminin sprite'ı pasif sprite olarak ayarlanır.
        wheatImage.sprite = passiveWheatSprite; //Buğday resminin sprite'ı pasif buğday sprite olarak ayarlanır.
        activeTransform.DOAnchorPosX(90f, _moveDuration).SetEase(_moveEase); //Aktif transformun X pozisyonu 90'a animasyonla taşınır.
    }

    public void PlayBoosterUIAnimations(RectTransform activeTransform, Image boosterImage, Image wheatImage, Sprite activeSprite, Sprite passiveSprite, Sprite activeWheatSprite, Sprite passiveWheatSprite, float duration)
    {
        StartCoroutine(SetBoosterUserInterfaces(activeTransform, boosterImage, wheatImage, activeSprite, passiveSprite, activeWheatSprite, passiveWheatSprite, duration)); //Booster UI animasyonlarını başlatır.yazım sıralamasıda önemli.
    }


}
