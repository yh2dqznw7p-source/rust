// SPDX-License-Identifier: MIT
// RustLike — first-person controller for the Phase 0 playable demo.
//
// Why this exists:
//   - We need *something* the player can walk around with so the project is
//     immediately playable when opened in Unity.
//   - It is intentionally minimal: CharacterController + legacy Input.
//   - Phase 1+ replaces this with a server-authoritative, predicted controller.
//
// Controls:
//   WASD      — walk
//   Shift     — sprint
//   Space     — jump
//   LeftCtrl  — crouch
//   Mouse     — look
//   Esc       — release cursor (Editor convenience)

using RustLike.Core.Bootstrap;
using RustLike.Gameplay.Survival;
using UnityEngine;

namespace RustLike.Presentation.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _sprintSpeed = 7f;
        [SerializeField] private float _crouchSpeed = 2f;
        [SerializeField] private float _jumpHeight = 1.2f;
        [SerializeField] private float _gravity = -19.62f;

        [Header("Look")]
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _maxLookUp = 85f;
        [SerializeField] private float _maxLookDown = -85f;

        // Networked entity id this body represents (server-side concept; here we
        // use it to look up Vitals on the locally-running VitalsService).
        public int EntityId { get; set; } = 1;

        private CharacterController _cc;
        private Transform _camera;
        private Vector3 _velocity;
        private float _pitch;
        private bool _cursorLocked;

        private VitalsService _vitals;
        private float _hungerTickAcc;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            // Camera is parented under the head transform, set up by ClientBootstrap.
            if (transform.childCount > 0)
                _camera = transform.GetChild(0); // [Head]
            LockCursor(true);

            ServiceLocator.TryGet(out _vitals);
        }

        private void Update()
        {
            HandleCursor();
            HandleLook();
            HandleMove();
        }

        // ---- input ----

        private void HandleCursor()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
            if (!_cursorLocked && Input.GetMouseButtonDown(0)) LockCursor(true);
        }

        private void LockCursor(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleLook()
        {
            if (!_cursorLocked || _camera == null) return;
            float mx = Input.GetAxisRaw("Mouse X") * _mouseSensitivity;
            float my = Input.GetAxisRaw("Mouse Y") * _mouseSensitivity;

            transform.Rotate(0f, mx, 0f, Space.Self);

            _pitch -= my;
            if (_pitch > _maxLookUp)   _pitch = _maxLookUp;
            if (_pitch < _maxLookDown) _pitch = _maxLookDown;
            _camera.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            bool sprint = Input.GetKey(KeyCode.LeftShift);
            bool crouch = Input.GetKey(KeyCode.LeftControl);

            float speed = crouch ? _crouchSpeed : (sprint ? _sprintSpeed : _walkSpeed);

            Vector3 wish = transform.right * h + transform.forward * v;
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            // grounded check: CharacterController.isGrounded is unreliable on slopes
            // but fine for the playable demo on a flat plane.
            if (_cc.isGrounded && _velocity.y < 0f) _velocity.y = -2f;

            if (Input.GetKeyDown(KeyCode.Space) && _cc.isGrounded)
                _velocity.y = Mathf.Sqrt(-2f * _gravity * _jumpHeight);

            _velocity.y += _gravity * Time.deltaTime;

            Vector3 motion = wish * speed + Vector3.up * _velocity.y;
            _cc.Move(motion * Time.deltaTime);

            // Sprinting drains hunger faster (cosmetic for the demo).
            if (_vitals != null && sprint && wish.sqrMagnitude > 0.01f)
            {
                _hungerTickAcc += Time.deltaTime;
                if (_hungerTickAcc >= 1f)
                {
                    _hungerTickAcc -= 1f;
                    // No public setter on Vitals — we only nudge by applying tiny
                    // hunger DamageType. Real implementation in Phase 2.
                    _vitals.ApplyDamage(new DamageInfo
                    {
                        TargetEntityId = EntityId,
                        AttackerEntityId = 0,
                        Type = DamageType.Hunger,
                        Part = BodyPart.Generic,
                        Amount = 0.1f, // tiny passive bleed of HP for sprint demo
                    });
                }
            }
        }
    }
}
