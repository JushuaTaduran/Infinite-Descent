using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float attackLunge = 1f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int health = 2;
    
    private Transform player;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isAttacking = false;
    private bool isDead = false;
    private Collider2D enemyCollider;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        // Since you mentioned sprite is a child, make sure to get components properly
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();
        
        // Start with Peek animation
        animator.SetTrigger("Peek");
    }

    private void Update()
    {
        if (player == null || isDead) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius && !isAttacking)
        {
            if (distanceToPlayer > attackDistance)
            {
                MoveTowardsPlayer();
            }
            else
            {
                StartCoroutine(AttackPlayer());
            }
        }
        else if (distanceToPlayer > detectionRadius)
        {
            // If player moves out of detection range
            animator.SetTrigger("Idle");
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
        
        // Flip sprite based on direction
        Transform spriteTransform = spriteRenderer.transform;
        if (direction.x > 0)
            spriteTransform.localScale = new Vector3(1, 1, 1);
        else if (direction.x < 0)
            spriteTransform.localScale = new Vector3(-1, 1, 1);
            
        // Set movement animation
        animator.SetBool("isMoving", true);
    }

    private IEnumerator AttackPlayer()
    {
        isAttacking = true;
        animator.SetBool("isMoving", false);
        Vector2 originalPosition = transform.position;
        Vector2 playerDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
        
        // Fix: Cast both vectors to Vector2 to avoid ambiguity
        Vector2 attackPosition = playerDirection * attackLunge + (Vector2)transform.position;

        // Face player before attacking
        Transform spriteTransform = spriteRenderer.transform;
        if (playerDirection.x > 0)
            spriteTransform.localScale = new Vector3(1, 1, 1);
        else if (playerDirection.x < 0)
            spriteTransform.localScale = new Vector3(-1, 1, 1);

        animator.SetTrigger("Pop");
        yield return new WaitForSeconds(0.5f); // Short delay before lunging

        float elapsedTime = 0;
        while (elapsedTime < 0.2f) // Lunge forward
        {
            transform.position = Vector2.Lerp(originalPosition, attackPosition, elapsedTime / 0.2f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Check if we've hit the player during attack
        Collider2D playerCollider = Physics2D.OverlapCircle((Vector2)transform.position, 0.5f, LayerMask.GetMask("Player"));
        if (playerCollider != null)
        {
            PlayerMovement playerMovement = playerCollider.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.TakeDamage();
            }
        }

        yield return new WaitForSeconds(0.3f); // Pause after attack

        elapsedTime = 0;
        while (elapsedTime < 0.2f) // Move back to original position
        {
            transform.position = Vector2.Lerp(attackPosition, originalPosition, elapsedTime / 0.2f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        animator.SetTrigger("Idle");
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isDead)
        {
            PlayerMovement playerMovement = collision.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.TakeDamage();
            }
        }
    }

    public void TakeDamage()
    {
        if (isDead) return;

        health--;
        animator.SetTrigger("Hurt"); // Play hurt animation

        StartCoroutine(FlashWhite());
        if (health <= 0)
        {
            isDead = true;
            animator.SetBool("isDead", true);
            
            // Disable collider and movement
            if (enemyCollider != null)
                enemyCollider.enabled = false;
            
            // Add a small death animation effect
            StartCoroutine(DeathEffect());
        }
    }
    
    private IEnumerator DeathEffect()
    {
        // Optional fade out effect
        for (float i = 1; i > 0; i -= 0.1f)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, i);
            yield return new WaitForSeconds(0.05f);
        }
        
        Destroy(gameObject);
    }

    IEnumerator FlashWhite()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.2f);
        spriteRenderer.color = originalColor;
    }
    
    // Show detection radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }

    public bool IsDead()
{
    return isDead;
}
}