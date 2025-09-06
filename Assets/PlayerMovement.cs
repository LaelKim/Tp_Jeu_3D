using UnityEngine;
using UnityEngine.InputSystem; // Nouveau Input System

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Mouvement")]
    public float moveSpeed = 6f;
    public float acceleration = 20f;

    [Header("Saut")]
    public float jumpForce = 6.5f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundMask;

    [Header("Référence caméra")]
    public Transform cameraTransform; // assigne la Main Camera (ou auto si null)

    [Header("Rotation du perso (optionnel)")]
    public bool rotateToMovement = false; // laisse false pour ne PAS tourner avec clic gauche
    public float turnSpeed = 15f;         // vitesse de rotation si rotateToMovement = true

    [Header("Animation (optionnel)")]
    public Animator animator;

    Rigidbody rb;
    bool isGrounded;
    Vector3 targetVel;
    Vector3 lastMoveDir = Vector3.forward;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // 1) Lire les inputs
        float x = 0f, z = 0f;
        if (kb.aKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed) x += 1f;
        if (kb.sKey.isPressed) z -= 1f;
        if (kb.wKey.isPressed) z += 1f;

        // 2) Calculer la direction dans l'espace **caméra**
        Vector3 moveDir;
        if (cameraTransform)
        {
            Vector3 camF = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 camR = Vector3.ProjectOnPlane(cameraTransform.right,   Vector3.up).normalized;
            moveDir = (camF * z + camR * x);
        }
        else
        {
            // fallback monde si pas de caméra
            moveDir = new Vector3(x, 0f, z);
        }
        if (moveDir.sqrMagnitude > 1e-4f) lastMoveDir = moveDir.normalized;

        targetVel = moveDir.normalized * moveSpeed;

        // 3) Saut
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
        if (kb.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // 4) (Option) faire tourner le perso vers sa direction de déplacement
        // Garder rotateToMovement = false pour respecter "clic gauche = ne pas tourner le Player"
        if (rotateToMovement && moveDir.sqrMagnitude > 1e-4f)
        {
            Quaternion look = Quaternion.LookRotation(lastMoveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        // 5) Animation (optionnel)
        if (animator)
        {
            float speed = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
            animator.SetFloat("Speed", speed);
            animator.SetBool("Grounded", isGrounded);
        }
    }

    void FixedUpdate()
    {
        Vector3 vel = rb.linearVelocity;
        Vector3 horizVel = new Vector3(vel.x, 0f, vel.z);
        Vector3 newHoriz = Vector3.MoveTowards(horizVel, targetVel, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(newHoriz.x, vel.y, newHoriz.z);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
