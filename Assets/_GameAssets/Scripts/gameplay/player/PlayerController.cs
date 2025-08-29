using System;
using UnityEngine; //Unity’nin temel kütüphanesini ekler. (GameObject, Rigidbody, Transform vb. için gerekli).

public class PlayerController : MonoBehaviour //MonoBehaviour türetilmiş bir sınıf. Unity sahnesine bir bileşen (component) olarak eklenebilir.PlayerController: Oyuncunun kontrolünü yöneten script.
{
    public event Action OnPlayerJumped; //Oyuncu zıpladığında tetiklenecek olay. Diğer scriptler bu olayı dinleyebilir.
    public event Action<PlayerState> OnPlayerStateChanged; //Oyuncu durumu değiştiğinde tetiklenecek olay. Diğer scriptler bu olayı dinleyebilir.

    [Header("References")]
    [SerializeField] private Transform _orientationTransform;//_orientationTransform: Karakterin yönünü belirleyen Transform. (Genelde kamera yönüyle aynı olur → WASD yönleri kamera yönüne göre hesaplanır.)
    [Header("Movement Settings")]
    [SerializeField] private KeyCode _movementKey;//normal ilerlerken ve kaydığımızda haraket farklılığı olacağı için movemente keycode atadık.yane kayma ve movemenet arasındaki geçişi sağlamak için
    [SerializeField] private float _movementSpeed;//Oyuncunun hareket hızı. Rigidbody’ye uygulanacak kuvvetin büyüklüğü.
    [Header("Jump Settings")]
    [SerializeField] private KeyCode _jumpKey;//keycode = hangi tuşa bastığımızda zıplasın? yane tuş seçme için kullanılıyo
    [SerializeField] private float _jumpForce;//Zıplama miktarını belirlemek için._jumpForce: Zıplama kuvveti (Impulse ile yukarı doğru ek kuvvet).
    [SerializeField] private float _jumpCooldown;//_jumpCooldown: Zıplama sonrası bekleme süresi. (Spam zıplamayı engeller.)
    [SerializeField] private float _airMultiplier;
    [SerializeField] private float _airDrag;
    [SerializeField] private bool _canJump;//_canJump: Şu an zıplayabilir mi? (Cooldown + zemin kontrolüyle ayarlanır.)
    [Header("Sliding Settings")]
    [SerializeField] private KeyCode _slideKey;//keycode = hangi tuşa bastığımızda kaysın? yane tuş seçme için kullanılıyo
    [SerializeField] private float _slideMultiplier;  //kayma hızımızın çarpılma miktarı gibi düşünülebilir.normal hızımızdan daha hızlı bi değer olacak.kayarken biraz daha hızlı olmam lazım normal hızımdan ne kadar daha hızlı olacağını belirleyen bi değişken lazım.
    [SerializeField] private float _slideDrag;


    [Header("Ground Check Settings")]
    [SerializeField] private float _playerHeight;//_playerHeight: Oyuncu yüksekliği. Raycast mesafesini hesaplamak için kullanılır.
    [SerializeField] private LayerMask _groundLayer;//_groundLayer: Zeminin katmanı (Layer). Raycast sadece bu katmanlardaki objelere çarpar.unityde floor ve playerın layerını ground yapıp buna atadık ground layer kısmında
    [SerializeField] private float _groundDrag;
    private StateController _stateController;

    private Rigidbody _playerRigidbody;//Rigidbody referansı. Hareket kuvvetleri buna uygulanır.

    private float _startingMovementSpeed, _startingJumpForce;//_startingMovementSpeed: Başlangıç hareket hızı._startingJumpForce: Başlangıç zıplama kuvveti. (Zıplama ve hareket hızını başlangıç değerlerine döndürmek için kullanılabilir.)

    private float _horizontalInput, _verticalInput;//_horizontalInput: X ekseni (A/D veya sol/sağ ok tuşları)._verticalInput: Z ekseni (W/S veya yukarı/aşağı ok tuşları).
    private Vector3 _movementDirection;//_movementDirection: Karakterin yön vektörü (kamera + giriş birleşimi).

    private bool _isSliding;//      isSliding=false ile isslidingi böyle boş bırakmak aynı şey.

    private void Awake()//Awake(): Script yüklendiğinde ilk çalışan fonksiyon.
    {
        _stateController = GetComponent<StateController>();
        _playerRigidbody = GetComponent<Rigidbody>();//GetComponent<Rigidbody>(): Aynı GameObject’teki Rigidbody bileşenini alır.
        _playerRigidbody.freezeRotation = true;//freezeRotation = true: Fizik çarpışmalarında karakterin devrilmesini engeller.

        _startingMovementSpeed = _movementSpeed;//Başlangıç hareket hızı.
        _startingJumpForce = _jumpForce;//Başlangıç zıplama kuvveti.

    }

    private void Update()//Update(): Her karede çalışır. Girdi (input) almak için kullanılır.
    {
        if (GameManager.Instance.GetCurrentGameState() != GameState.Play && GameManager.Instance.GetCurrentGameState() != GameState.Resume)
        {
            return;
        }

        SetInputs();
        SetStates();
        SetPlayerDrag();
        LimitPlayerSpeed();
    }

    private void FixedUpdate()//FixedUpdate(): Fizik hesaplamaları için kullanılan sabit zaman adımlı fonksiyon.
    {
        if (GameManager.Instance.GetCurrentGameState() != GameState.Play && GameManager.Instance.GetCurrentGameState() != GameState.Resume)
        {
            return;
        }

        SetPlayerMovement();//SetPlayerMovement(): Rigidbody’ye kuvvet uygulayarak hareket ettirir.
    }

    private void SetInputs()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");//Input.GetAxisRaw("Horizontal"): A/D veya sol/sağ tuşlarından ham değer (-1, 0, 1).
        _verticalInput = Input.GetAxisRaw("Vertical");//Input.GetAxisRaw("Vertical"): W/S veya yukarı/aşağı tuşları.

        if (Input.GetKeyDown(_slideKey))
        {
            _isSliding = true;
        }
        else if (Input.GetKeyDown(_movementKey))
        {
            _isSliding = false;
        }

        else if (Input.GetKey(_jumpKey) && _canJump && IsGrounded())//Input.GetKey(_jumpKey): Belirlenen tuşa basıldığında._canJump && IsGrounded(): Zıplama için iki koşul → yerde olmak + cooldown bitmiş.
        {
            //zıplama işlemleri gerçekleşecek
            _canJump = false;
            SetPlayerJumping();//SetPlayerJumping(): Zıplama kuvvetini uygular.
            Invoke(nameof(ResetJumping), _jumpCooldown);//Invoke(nameof(ResetJumping), _jumpCooldown): Belirli süre sonra tekrar zıplayabilir hale getirir.cooldown kadar bekle sonra canjump true çalıştır
            AudioManager.Instance.Play(SoundType.JumpSound);//Zıplama sesi çalar.
        }
    }

    private void SetStates()
    {
        var movementDirection = GetMovementDirection();
        var isGrounded = IsGrounded();
        var isSliding = IsSliding();
        var currentState = _stateController.GetCurrentState();
        var newState = currentState switch //switch expression yapısı
        {
            _ when movementDirection == Vector3.zero && isGrounded && !isSliding => PlayerState.Idle,//ne zaman benim movementdirecitonum eşittir sıfırsa yani haraket etmiyorsam yerdeysem ve kaymıyorsam
            _ when movementDirection != Vector3.zero && isGrounded && !isSliding => PlayerState.Move,
            _ when movementDirection != Vector3.zero && isGrounded && isSliding => PlayerState.Slide,
            _ when movementDirection == Vector3.zero && isGrounded && isSliding => PlayerState.SlideIdle,
            _ when !_canJump && !isGrounded => PlayerState.Jump,
            _ => currentState
        };

        if (newState != currentState)
        {
            _stateController.ChangeState(newState);
            OnPlayerStateChanged?.Invoke(newState); //Durum değiştiğinde olay tetiklenir. Diğer scriptler bu olayı dinleyebilir.
        }
    }

    private void SetPlayerMovement()
    {
        _movementDirection = _orientationTransform.forward * _verticalInput + _orientationTransform.right * _horizontalInput;//_movementDirection: Kamera yönü (forward/right) ile input birleşimi.forward ön demek

        float forceMultiplier = _stateController.GetCurrentState() switch
        {
            PlayerState.Move => 1f,
            PlayerState.Slide => _slideMultiplier,
            PlayerState.Jump => _airMultiplier,
            _ => 1f,
        };
        _playerRigidbody.AddForce(_movementDirection.normalized * _movementSpeed * forceMultiplier, ForceMode.Force);

        //if (_isSliding)//      issliding == true ile boş bırakmak aynı boşda true oluyo.false yapmak istiyosanda: !issliding başına ünlem koy{ _playerRigidbody.AddForce(_movementDirection.normalized * _movementSpeed * _slideMultiplier, ForceMode.Force); }
        //_playerRigidbody.AddForce(_movementDirection.normalized * _movementSpeed, ForceMode.Force);//normalized ile w ve d ye aynı anda basıldığında da 1 birim gitsin diye ayarlıyor.normalized: Diagonal hareketlerde (W+D) hızın artmaması için.AddForce(..., ForceMode.Force): Sürekli bir kuvvet uygular.else{_playerRigidbody.AddForce(_movementDirection.normalized * _movementSpeed, ForceMode.Force);}
    }

    private void SetPlayerDrag()
    {
        _playerRigidbody.linearDamping = _stateController.GetCurrentState() switch
        {
            PlayerState.Move => _groundDrag,
            PlayerState.Slide => _slideDrag,
            PlayerState.Jump => _airDrag,
            _ => _playerRigidbody.linearDamping
        };
        //if (_isSliding){_playerRigidbody.linearDamping = _slideDrag;}else{_playerRigidbody.linearDamping = _groundDrag;}
    }

    private void LimitPlayerSpeed()//hilecileri önlemeye çalışmak için
    {
        Vector3 flatVelocity = new Vector3(_playerRigidbody.linearVelocity.x, 0f, _playerRigidbody.linearVelocity.z);
        if (flatVelocity.magnitude > _movementSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * _movementSpeed;
            _playerRigidbody.linearVelocity = new Vector3(limitedVelocity.x, _playerRigidbody.linearVelocity.y, limitedVelocity.z);
        }
    }

    private void SetPlayerJumping()
    {
        OnPlayerJumped?.Invoke(); //Zıplama olayını tetikler. Diğer scriptler bu olayı dinleyebilir.
        _playerRigidbody.linearVelocity = new Vector3(_playerRigidbody.linearVelocity.x, 0f, _playerRigidbody.linearVelocity.z);// Zıplarken önce mevcut düşey hız sıfırlanıyor (daha tutarlı zıplama).linearvelocity doğrusal hız demek saçma yerlere zıplamasın diye.
        _playerRigidbody.AddForce(transform.up * _jumpForce, ForceMode.Impulse);//AddForce(..., ForceMode.Impulse): Anlık kuvvet → patlama/zıplama gibi hareketlerde kullanılır.AddForce(..., ForceMode.Impulse): Anlık kuvvet → patlama/zıplama gibi hareketlerde kullanılır.
    }
    private void ResetJumping()
    {
        _canJump = true;//Zıplama cooldown süresi dolduğunda yeniden zıplayabilmeyi sağlar.
    }

    #region Helpers Functions

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, _playerHeight * 0.5f + 0.2f, _groundLayer);//Raycast: Oyuncunun altına ışın gönderir._playerHeight * 0.5f + 0.2f: Yüksekliğin yarısı + küçük ek mesafe → yere biraz yakınsa bile "yerde" kabul edilir._groundLayer: Sadece zemine çarpıp çarpmadığını kontrol eder.yere doğru ışın atarak yere değip değmediğimizi kontrol ediyoruz. havadayken zıplamasını engelliyo
    }

    private Vector3 GetMovementDirection()
    {
        return _movementDirection.normalized;
    }

    private bool IsSliding()
    {
        return _isSliding;
    }

    public void SetMovementSpeed(float speed, float duration)
    {
        _movementSpeed += speed;
        Invoke(nameof(ResetMovementSpeed), duration);//Belirli bir süre sonra hareket hızını başlangıç değerine döndürür.
    }

    public void ResetMovementSpeed()
    {
        _movementSpeed = _startingMovementSpeed;
    }

    public void SetJumpForce(float force, float duration)
    {
        _jumpForce += force;
        Invoke(nameof(ResetJumpForce), duration);//Belirli bir süre sonra zıplama kuvvetini başlangıç değerine döndürür.
    }
    public void ResetJumpForce()
    {
        _jumpForce = _startingJumpForce;
    }
    public Rigidbody GetPlayerRigidbody()
    {
        return _playerRigidbody;
    }

    public bool CanCatChase()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, _playerHeight * 0.5f + 0.2f, _groundLayer))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer(Consts.Layers.FLOOR_LAYER))
            {
                return true;
            }
            else if (hit.collider.gameObject.layer == LayerMask.NameToLayer(Consts.Layers.GROUND_LAYER))
            {
                return false;
            }
        }
        return false;

    }
    #endregion
}
