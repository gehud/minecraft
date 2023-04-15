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

		private IInputProvider inputProvider;

		private int inputHash;
		private int miningHash;

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
		}

		private void OnDisable() {
			inputProvider.OnLeftMouseButtonDown -= OnEnterMining;
			inputProvider.OnLeftMouseButton -= OnEnterMining;
			inputProvider.OnLeftMouseButtonUp -= OnExitMining;
		}
	}
}
