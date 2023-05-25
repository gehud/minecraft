using Minecraft.Extensions;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Minecraft.Player {
	public class AnimationController : NetworkBehaviour {
		[Inject]
		private readonly ISavePayload savePayload;

		[SerializeField]
		private Animator avatarAnimator;
		[SerializeField]
		private Animator handAnimator;
		[SerializeField]
		private new Camera camera;
		[SerializeField]
		private Transform head;
		[SerializeField]
		private Transform body;
		[SerializeField, Min(0.0f)]
		private float rotationTreshold = 45.0f;
		[SerializeField, Min(0.0f)]
		private float rotationSpeed = 10.0f;

		private IInputProvider inputProvider;

		private int inputHash;
		private int miningHash;

		private Vector3 bodyTargetRotation = Vector3.zero;

		private void Awake() {
			inputProvider = GetComponent<IInputProvider>();
			inputHash = Animator.StringToHash("Input");
			miningHash = Animator.StringToHash("Mining");
		}

		private void Update() {
			if (savePayload.Role != ConnectionRoles.None && !IsOwner)
				return;

			avatarAnimator.SetFloat(inputHash, inputProvider.Movement.NormalizeSmooth().magnitude);
			handAnimator.SetFloat(inputHash, inputProvider.Movement.NormalizeSmooth().magnitude);

			float angle = Quaternion.Angle(Quaternion.Euler(0.0f, camera.transform.eulerAngles.y, 0.0f), Quaternion.Euler(0.0f, body.eulerAngles.y, 0.0f));
			if (angle > rotationTreshold) {
				bodyTargetRotation = new Vector3(0.0f, camera.transform.eulerAngles.y, 0.0f);
			}

			body.rotation = Quaternion.RotateTowards(body.rotation, Quaternion.Euler(bodyTargetRotation), rotationSpeed * Time.deltaTime);
			head.rotation = camera.transform.rotation;
		}

		private void OnEnterMining() {
			if (savePayload.Role != ConnectionRoles.None && !IsOwner)
				return;

			avatarAnimator.SetBool(miningHash, true);
			handAnimator.SetBool(miningHash, true);
		}

		private void OnExitMining() {
			if (savePayload.Role != ConnectionRoles.None && !IsOwner)
				return;

			avatarAnimator.SetBool(miningHash, false);
			handAnimator.SetBool(miningHash, false);
		}

		private void OnEnable() {
			inputProvider.OnLeftMouseButtonDown += OnEnterMining;
			inputProvider.OnLeftMouseButton += OnEnterMining;
			inputProvider.OnLeftMouseButtonUp += OnExitMining;
			inputProvider.OnRightMouseButtonDown += OnEnterMining;
			inputProvider.OnRightMouseButton += OnEnterMining;
			inputProvider.OnRightMouseButtonUp += OnExitMining;
		}

		private void OnDisable() {
			inputProvider.OnLeftMouseButtonDown -= OnEnterMining;
			inputProvider.OnLeftMouseButton -= OnEnterMining;
			inputProvider.OnLeftMouseButtonUp -= OnExitMining;
			inputProvider.OnRightMouseButtonDown -= OnEnterMining;
			inputProvider.OnRightMouseButton -= OnEnterMining;
			inputProvider.OnRightMouseButtonUp -= OnExitMining;
		}
	}
}
