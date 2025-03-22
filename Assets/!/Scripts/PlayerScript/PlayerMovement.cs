using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Animator anim;
    private SpriteRenderer spriteRenderer;
    
    public LayerMask wallLayer;
    public LayerMask enemyLayer; // Layer for enemies

    public int maxHealth = 3;
    private int currentHealth;
    private bool isInvulnerable = false;
    public HealthUI healthUI;
    
    public float attackRange = 1f;
    public float attackOffset = 0.5f; // Forward offset for attack
    public float invulnerableTime = 0.5f; // Player flashes white when damaged
    public Transform attackPoint; // Reference point for attack position

    private bool canAttack = true; // Prevent spamming attack
    
    // Camera shake parameters
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.1f;
    private Vector3 originalCameraPosition;
    private Camera mainCamera;
    private Transform cameraParent;
    
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        currentHealth = maxHealth;
        mainCamera = Camera.main;
        originalCameraPosition = mainCamera.transform.position;
        cameraParent = mainCamera.transform.parent;
        healthUI.InitializeHealth(maxHealth);
    }

    void Update()
    {
        HandleMovement();
        HandleAttack();
        healthUI.UpdateHealth(currentHealth);
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector3 moveDirection = new Vector3(moveX, moveY, 0).normalized;

        if (moveDirection != Vector3.zero)
        {
            // Check for wall collision before moving
            Vector2 targetPosition = (Vector2)transform.position + (Vector2)moveDirection * moveSpeed * Time.deltaTime;
            if (!Physics2D.OverlapCircle(targetPosition, 0.2f, wallLayer))
            {
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
            }
            
            FlipSprite(moveX);
        }

        anim.SetBool("isMoving", moveDirection != Vector3.zero);
    }

    void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0) && canAttack) // Left mouse button
        {
            StartCoroutine(Attack());
        }
    }

    IEnumerator Attack()
    {
        canAttack = false;
        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.2f); // Short delay before damage is applied
        PerformAttack();

        yield return new WaitForSeconds(0.3f); // Cooldown before next attack
        canAttack = true;
    }

    void PerformAttack()
    {
        // Calculate attack position based on player's facing direction
        Vector2 attackCenter;
        if (attackPoint != null)
        {
            // Use the attack point if available
            attackPoint.localPosition = new Vector3(spriteRenderer.flipX ? -Mathf.Abs(attackPoint.localPosition.x) : Mathf.Abs(attackPoint.localPosition.x), attackPoint.localPosition.y, 0);
            attackCenter = attackPoint.position;
        }
        else
        {
            // Fallback to calculate attack position
            attackCenter = (Vector2)transform.position + new Vector2(spriteRenderer.flipX ? -attackOffset : attackOffset, 0);
        }
        
        Vector2 boxSize = new Vector2(attackRange, attackRange * 1.5f); // Wider attack area

        Collider2D[] enemiesHit = Physics2D.OverlapBoxAll(attackCenter, boxSize, 0, enemyLayer);

        foreach (Collider2D enemy in enemiesHit)
        {
            Debug.Log("Hit: " + enemy.name);
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            SkellyController skellyController = enemy.GetComponent<SkellyController>();
            if (skellyController != null)
            {
                skellyController.TakeDamage();
            }
            if (enemyController != null)
            {
                enemyController.TakeDamage();
            }
        }
    }

    void FlipSprite(float moveDirection)
    {
        if (moveDirection > 0)
            spriteRenderer.flipX = false; // Face right
        else if (moveDirection < 0)
            spriteRenderer.flipX = true;  // Face left
    }

    public void TakeDamage()
    {
        if (isInvulnerable) return;

        currentHealth--;
        anim.SetTrigger("Hurt"); // Play hurt animation
        healthUI.UpdateHealth(currentHealth);
        StartCoroutine(CameraShake()); // Add camera shake effect

        if (currentHealth <= 0)
        {
            anim.SetBool("isDead", true);
            StartCoroutine(PlayerDeath());
        }
        else
        {
            StartCoroutine(FlashWhite());
        }
    }

    IEnumerator PlayerDeath()
    {
        // Disable movement and attacks
        this.enabled = false;
        
        yield return new WaitForSeconds(1f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }

    IEnumerator FlashWhite()
    {
        isInvulnerable = true;
        Color originalColor = spriteRenderer.color;
        
        for (float i = 0; i < invulnerableTime; i += 0.1f)
        {
            // Toggle between white and original color for flashing effect
            spriteRenderer.color = spriteRenderer.color == Color.white ? originalColor : Color.white;
            yield return new WaitForSeconds(0.1f);
        }
        
        spriteRenderer.color = originalColor;
        isInvulnerable = false;
    }

    IEnumerator CameraShake()
    {
        Transform camTransform = mainCamera.transform;
        Vector3 originalPosition = camTransform.position;
        camTransform.SetParent(null);

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            
            camTransform.position = originalPosition + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        camTransform.position = originalPosition;
        camTransform.SetParent(cameraParent);
    }

    void OnDrawGizmosSelected()
    {
        // Draw attack range gizmo
        Gizmos.color = Color.red;
        Vector2 attackCenter;
        
        if (attackPoint != null)
        {
            attackCenter = attackPoint.position;
        }
        else
        {
            // If we don't have the component references yet (in Edit mode)
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            bool flipX = sr != null ? sr.flipX : false;
            
            attackCenter = (Vector2)transform.position + new Vector2(flipX ? -attackOffset : attackOffset, 0);
        }
        
        Gizmos.DrawWireCube(attackCenter, new Vector2(attackRange, attackRange * 1.5f));
    }
}