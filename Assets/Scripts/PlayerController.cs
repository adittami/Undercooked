using System.Collections;
using Lean.Transition;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Undercooked
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Physics")] [SerializeField] private Rigidbody playerRigidbody;

        private InteractableController _interactableController;

        [Header("Animation")] [SerializeField] private Animator playerAnimator;
        private readonly int _playerMovementID = Animator.StringToHash("Movement");
        private readonly int _playerPickID = Animator.StringToHash("Pick");

        [Header("Input")] [SerializeField] private PlayerInput playerInput;

        private const string ActionMapGameplay = "PlayerControls";
        private const string ActionMapMenu = "MenuControls";

        private Vector3 _inputDirection;

        // Dashing
        [SerializeField] private float dashForce = 400f;
        private bool _isDashing = false;
        private bool _isDashingPossible = true;
        private readonly WaitForSeconds _dashDuration = new WaitForSeconds(0.17f);
        private readonly WaitForSeconds _dashCooldown = new WaitForSeconds(0.07f);

        [Header("Movement Settings")] [SerializeField]
        private float movementSpeed = 5f;

        //TODO: should this be handled by InteractableController?
        private IPickable _currentPickable;
        //TODO: how to populate this automatically and/or feed from InteractableController
        [SerializeField] private Transform interactableHolder;
        
        private InputAction _moveAction;
        private InputAction _dashAction;
        private InputAction _pickUpAction;
        private InputAction _interactAction;

        [SerializeField] private ParticleSystem dashParticle;
        [SerializeField] private AudioClip dashAudio;

        private void Awake()
        {
            _interactableController = GetComponentInChildren<InteractableController>();
        }

        private void Start()
        {
            _moveAction = playerInput.currentActionMap["Move"];
            _dashAction = playerInput.currentActionMap["Dash"];
            _pickUpAction = playerInput.currentActionMap["PickUp"];
            _interactAction = playerInput.currentActionMap["Interact"];

            EnableGameplayControls();
            _moveAction.performed += HandleMove;
            _dashAction.performed += HandleDash;
            _pickUpAction.performed += HandlePickUp;
            _interactAction.performed += HandleInteract;
            playerInput.currentActionMap.Enable();
        }

        private void HandleDash(InputAction.CallbackContext context)
        {
            if (!_isDashingPossible) return;
            StartCoroutine(Dash());
        }

        private IEnumerator Dash()
        {
            _isDashingPossible = false;
            playerRigidbody.AddRelativeForce(dashForce * Vector3.forward);
            dashParticle.Play();
            dashParticle.PlaySoundTransition(dashAudio);
            
            yield return new WaitForFixedUpdate();
            _isDashing = true;
            yield return _dashDuration;
            _isDashing = false;
            yield return _dashCooldown;
            _isDashingPossible = true;
        }

        private void HandlePickUp(InputAction.CallbackContext context)
        {
            var interactable = _interactableController.CurrentInteractable;
            
            // empty hands, try to pick
            if (_currentPickable == null)
            {
                _currentPickable = interactable as IPickable;
                if (_currentPickable != null)
                {
                    _currentPickable.Pick();
                    _interactableController.Remove(_currentPickable as Interactable);
                    _currentPickable.gameObject.transform.SetPositionAndRotation(interactableHolder.transform.position, Quaternion.identity);
                    _currentPickable.gameObject.transform.SetParent(interactableHolder);
                    return;
                }

                // Interactable only (not a IPickable)
                _currentPickable = interactable?.TryToPickUpFromSlot(_currentPickable);
                _currentPickable?.gameObject.transform.SetPositionAndRotation(
                    interactableHolder.position, Quaternion.identity);
                _currentPickable?.gameObject.transform.SetParent(interactableHolder);
                return;
            }
            
            // we carry a pickable, let's try to drop it (we may fail)
            
            // no interactable in range or at most a Pickable in range (we ignore it)
            if (interactable == null || interactable is IPickable)
            {
                _currentPickable.Drop();
                _currentPickable = null;
                return;
            }
            
            // we carry a pickable and we have an interactable in range
            // we may drop into the interactable
            
            // Try to drop on the interactable. It may refuse it, e.g. dropping a plate into the CuttingBoard,
            // or simply it already have something on it
            //Debug.Log($"[PlayerController] {_currentPickable.gameObject.name} trying to drop into {interactable.gameObject.name} ");

            bool dropSuccess = interactable.TryToDropIntoSlot(_currentPickable);
            if (dropSuccess)
            {
                // clean pickable references
                //Debug.Log($"[PlayerController] Successfully dropped {_currentPickable.gameObject.name} into {interactable.gameObject.name}");
                _currentPickable = null;
            }
            else
            {
                //Debug.Log("[PlayerController] Interactable refuse dropped pickable");
            }
        }
    
        private void HandleMove(InputAction.CallbackContext context)
        {
            // TODO: Processors on input binding not working for analogical stick. Investigate it.
            Vector2 inputMovement = context.ReadValue<Vector2>();
            if (inputMovement.x > 0.3f)
            {
                inputMovement.x = 1f;
            }
            else if (inputMovement.x < -0.3)
            {
                inputMovement.x = -1f;
            }
            else
            {
                inputMovement.x = 0f;
            }

            if (inputMovement.y > 0.3f)
            {
                inputMovement.y = 1f;
            }
            else if (inputMovement.y < -0.3f)
            {
                inputMovement.y = -1f;
            }
            else
            {
                inputMovement.y = 0f;
            }
            
            _inputDirection = new Vector3(inputMovement.x, 0, inputMovement.y);
        }

        private void HandleInteract(InputAction.CallbackContext context)
        {
            _interactableController.CurrentInteractable?.Interact();
        }

        private void Update()
        {
            CalculateInputDirection();
        }

        private void FixedUpdate()
        {
            
            MoveThePlayer();
            //AnimatePlayerMovement();
            TurnThePlayer();
        }
        
        private void MoveThePlayer()
        {
            if (_isDashing)
            {
                var currentVelocity = playerRigidbody.velocity.magnitude;

                var inputNormalized = _inputDirection.normalized;
                if (inputNormalized == Vector3.zero)
                {
                    inputNormalized = transform.forward;
                }
                playerRigidbody.velocity = inputNormalized * currentVelocity;
            }
            else
            {
                playerRigidbody.velocity = _inputDirection.normalized * movementSpeed;
            }
        }

        private void CalculateInputDirection()
        {
            var inputMovement = _moveAction.ReadValue<Vector2>();
            if (inputMovement.x > 0.3f)
            {
                inputMovement.x = 1f;
            }
            else if (inputMovement.x < -0.3)
            {
                inputMovement.x = -1f;
            }
            else
            {
                inputMovement.x = 0f;
            }

            if (inputMovement.y > 0.3f)
            {
                inputMovement.y = 1f;
            }
            else if (inputMovement.y < -0.3f)
            {
                inputMovement.y = -1f;
            }
            else
            {
                inputMovement.y = 0f;
            }

            _inputDirection = new Vector3(inputMovement.x, 0f, inputMovement.y);
        }

        private void TurnThePlayer()
        {
            if (!(playerRigidbody.velocity.magnitude > 0.1f) || _inputDirection == Vector3.zero) return;
            
            Quaternion newRotation = Quaternion.LookRotation(_inputDirection);
            transform.rotation = Quaternion.Slerp (transform.rotation, newRotation, Time.deltaTime * 15f);
        }

        private void AnimatePlayerMovement()
        {
            playerAnimator.SetFloat(_playerMovementID, _inputDirection.sqrMagnitude);
        }
        
        //Switching Action Maps ----

        public void EnableGameplayControls()
        {
            playerInput.SwitchCurrentActionMap(ActionMapGameplay);  
        }

        public void EnablePauseMenuControls()
        {
            playerInput.SwitchCurrentActionMap(ActionMapMenu);
        }
        
        public PlayerInput GetPlayerInput()
        {
            return playerInput;
        } 
    }
}
