using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private FloatingJoystickController joystick;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private float rotateSpeed = 720f;

    private NavMeshAgent agent;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        agent = GetComponent<NavMeshAgent>();

        agent.updateRotation = false;
        agent.updateUpAxis = true;

        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentInputMode != InputMode.Gameplay)
        {
            return;
        }

        if (joystick == null)
            return;

        Vector2 input = joystick.MoveInput;

        Vector3 move = GetCameraRelativeMove(input);

        if (move.sqrMagnitude > 0.0001f)
        {
            if (animator != null)
            {
                animator.SetFloat("Velocity", agent.speed);
            }

            Vector3 delta = move.normalized * agent.speed * Time.deltaTime;
            agent.Move(delta);

            Quaternion targetRot = Quaternion.LookRotation(move.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );
        }
        else
        {
            if (animator != null)
            {
                animator.SetFloat("Velocity", 0);
            }
        }
    }

    private Vector3 GetCameraRelativeMove(Vector2 input)
    {
        if (gameplayCamera == null)
            return new Vector3(input.x, 0f, input.y);

        Transform cam = gameplayCamera.transform;

        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camRight * input.x + camForward * input.y;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        return move;
    }
}