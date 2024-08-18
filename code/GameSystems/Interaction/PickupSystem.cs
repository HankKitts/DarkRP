using Sandbox;
using GameSystems.Player;

namespace GameSystems.Interaction
{
    public class PickupSystem : Component
    {

		/// <summary>
		/// Properties for physics holding props
		/// </summary>

		private GameObject holdingArea;

        private readonly float interactRange;
		private readonly float throwForce = 500000f;
		private GameObject heldObject;
		private Rigidbody heldObjectRigidbody;
		private Vector3 heldObjectCenter;
		private Player.MovementController playerController;

        public PickupSystem(float interactRange, Player.MovementController playerController, GameObject holdingArea)
        {
            this.interactRange = interactRange;
			this.playerController = playerController;
			this.holdingArea = holdingArea;
		}
		public bool IsHoldingObject(){
			return (heldObject != null && heldObjectRigidbody != null);
		}

		public void HandlePickup(GameObject pickedUpObject)
		{
			Rigidbody rb = pickedUpObject.Components.Get<Rigidbody>();
			if (rb != null)
			{
				// remove the earlier parent of the pickedUpObject 
				// i.e if someone else is holding it, remove it from their hands
				if (pickedUpObject.Parent != null)
				{
					pickedUpObject.SetParent(null);
					rb.GameObject.SetParent(null);
				}
				// maybe some other logic is preferred. Maybe not being able to steal items from others

				heldObjectRigidbody = rb;
				heldObjectRigidbody.Gravity = false;
				heldObjectRigidbody.ClearForces();

				// Get the bounds of the object
				var bounds = pickedUpObject.GetBounds();
				heldObjectCenter = bounds.Center;

				heldObjectRigidbody.GameObject.SetParent(holdingArea);
				heldObject = pickedUpObject;
			}
		}
		public void DropPickup(float throwingForce = 0)
		{
			heldObjectRigidbody.Gravity = true;

			UnlockHeldObject();
			heldObjectRigidbody.GameObject.SetParent(null);
			heldObjectRigidbody.PhysicsBody.ApplyForce(playerController.EyeAngles.Forward * throwingForce);
			heldObject = null;
		}

		// This function is called in the player's update loop
		public void UpdateHeldObject()
		{
			if (heldObject != null)
			{
				SetHoldingArea();
				// Check if the object is too far away from the holding area
				float dist = Vector3.DistanceBetween(heldObject.Transform.Position, holdingArea.Transform.Position);
				if (dist > 1.5f*interactRange) { DropPickup(); return; }

				// Move the object to the holding area
				if (dist > 1f)
				{
					heldObjectRigidbody.Transform.Position = holdingArea.Transform.Position;
				}
				// Rotate the object if the player is holding down the rotate button
				if ( Input.Down( "attack3" ) ) {
					RotateHeldObject();
				} else if (Input.Released("attack3")) {
					UnlockHeldObject();
				} else if ( Input.Down( "reload" ) ) {
					ResetRotationHeldObject();
				} else if (Input.Down("attack1")) {
					DropPickup(throwForce);
				}
			}
		}
		public void RotateHeldObject()
		{
			playerController.EyesLocked = true;
			heldObject.Transform.Local = heldObject.Transform.Local.RotateAround(heldObjectCenter, Input.AnalogLook);
		}
		public void ResetRotationHeldObject()
		{
			heldObject.Transform.Rotation = Rotation.Identity;
		}
		public void UnlockHeldObject()
		{
			playerController.EyesLocked = false;
		}
		private void SetHoldingArea()
		{
			var eyePos = playerController.Transform.Position + new Vector3(0, 0, playerController.EyeHeight);
			var eyeAngles = playerController.EyeAngles.Forward;
			// rotate the target target vector according to the camera rotation from the eye position
			var targetVec = eyePos + eyeAngles * (interactRange / 2);

			holdingArea.Transform.Position = holdingArea.Transform.Position.LerpTo(targetVec, 0.15f);
		}
    }
}
