using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;

    [Header("物件")]
    public Transform playerTransform; // 角色的 Transform
    public Transform groundCheck;     // 在腳下放一個空的 GameObject

    [Header("偵測設定")]
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;    // 設定地板的 Layer

    private Rigidbody2D rb;
    private bool isGrounded;
    private float moveInput;

    // 如果你有動畫組件，取消註解下面這行
    // private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 偵測是否在地面
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // 左右移動輸入
        moveInput = Input.GetAxisRaw("Horizontal");

        // 跳躍輸入 (按下 Space)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 更新動畫參數 (若有設定 Animator)
        /*
        anim.SetFloat("Speed", Mathf.Abs(moveInput));
        anim.SetBool("isGrounded", isGrounded);
        */
    }

    void FixedUpdate()
    {
        // 物理移動處理
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

}
