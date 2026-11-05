using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace CleanLaboratory.Gameplay
{
    public class ActionInput : MonoBehaviour
    {
        [Header("Player Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;

        [Header("Commands")]

        public UnityEvent Fire1;
        public UnityEvent Drop;
        public UnityEvent Action1;

        public void OnFire1(InputValue input)
        {
            Fire1.Invoke();
        }

        public void OnDrop(InputValue input)
        {
            Drop.Invoke();
        }

        public void OnAction1(InputValue input)
        {
            Debug.Log("Actionning");
            Action1.Invoke();
        }

        public void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            LookInput(value.Get<Vector2>());
        }

        public void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }

        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
        }

        public void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
        }
    }
}