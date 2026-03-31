using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private FloatingJoystickController joystick;
    [SerializeField] private float rotateSpeed = 720f;

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.updateRotation = false;
        agent.updateUpAxis = true;
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
        Vector3 move = new Vector3(input.x, 0f, input.y);

        if (move.sqrMagnitude > 0.0001f)
        {
            Vector3 delta = move.normalized * agent.speed * Time.deltaTime;
            agent.Move(delta);

            Quaternion targetRot = Quaternion.LookRotation(move.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );
        }
    }
}