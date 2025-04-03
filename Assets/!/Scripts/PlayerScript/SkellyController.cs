using System.Collections;
using UnityEngine;

public class SkellyController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int health = 2;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.25f;
    [SerializeField] private LayerMask playerLayer;

    [SerializeField] private SpriteRenderer spriteRenderer; // Assigned manually in Inspector
    public AnimationClip dieAnimationClip;

    private Transform player;
    private Animator animator;
    private bool isAttacking = false;
    public bool isDead = false;
    private Collider2D enemyCollider;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        animator = GetComponentInChildren<Animator>();
        enemyCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
        {
            Debug.LogWarning("SpriteRenderer is not assigned in SkellyController. Please assign it in the Inspector.");
        }
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
            animator.SetBool("isWalking", false);
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);

        FlipSprite(direction.x); // Flip in the direction of movement
        animator.SetBool("isMoving", true);
    }

    private IEnumerator AttackPlayer()
    {
        isAttacking = true;
        animator.SetBool("isWalking", false);

        Vector2 playerDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
        FlipSprite(playerDirection.x); // Ensure it faces the player before attacking

        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(0.5f); // Attack animation delay

        Collider2D playerCollider = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerLayer);
        if (playerCollider != null)
        {
            PlayerMovement playerMovement = playerCollider.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.TakeDamage();
            }
        }

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    private void FlipSprite(float directionX)
    {
        if (spriteRenderer != null)
        {
            if (directionX > 0)
                spriteRenderer.transform.localScale = new Vector3(1, 1, 1);
            else if (directionX < 0)
                spriteRenderer.transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }

    public void TakeDamage()
    {
        if (isDead) return;
        
        health--;
        Debug.Log($"Skelly took damage! Health left: {health}");

        animator.SetTrigger("Hit");
        StartCoroutine(FlashWhite());

        if (health <= 0)
        {
            StartCoroutine(HandleDeathAfterHit());
        }
    }

    private IEnumerator HandleDeathAfterHit()
    {
        float hitAnimationLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(hitAnimationLength);

        Die();
    }

    private void Die()
    {
        isDead = true;
        animator.SetTrigger("Die");
        enemyCollider.enabled = false;

        StartCoroutine(DeathEffect());
        StartCoroutine(HoldLastFrame());
    }

    private IEnumerator HoldLastFrame()
    {
        yield return new WaitForSeconds(dieAnimationClip.length);
        animator.speed = 0;
    }

    private IEnumerator DeathEffect()
    {
        yield return new WaitForSeconds(1.5f);
        LeanTween.alpha(spriteRenderer.gameObject, 0, 1f).setOnComplete(() => Destroy(gameObject));
    }

    private IEnumerator FlashWhite()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
}
