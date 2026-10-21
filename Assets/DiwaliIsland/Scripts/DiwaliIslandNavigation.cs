using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace DiwaliIsland
{
    public sealed class DiwaliIslandNavigation : MonoBehaviour
    {
        public TeleportationProvider provider;
        public Transform[] destinations;
        public GameObject controllerMenu;
        InputAction toggleMenu;

        void OnEnable()
        {
            toggleMenu = new InputAction("Diwali travel menu", InputActionType.Button,
                "<XRController>{LeftHand}/secondaryButton");
            toggleMenu.performed += ToggleMenu;
            toggleMenu.Enable();
        }

        void OnDisable()
        {
            if (toggleMenu == null) return;
            toggleMenu.performed -= ToggleMenu;
            toggleMenu.Dispose();
            toggleMenu = null;
        }

        void ToggleMenu(InputAction.CallbackContext context)
        {
            if (controllerMenu != null) controllerMenu.SetActive(!controllerMenu.activeSelf);
        }

        public void VisitVenue(int index)
        {
            if (provider == null || destinations == null || index < 0 || index >= destinations.Length)
                return;
            var target = destinations[index];
            if (target == null) return;
            provider.QueueTeleportRequest(new TeleportRequest
            {
                destinationPosition = target.position,
                destinationRotation = target.rotation,
                matchOrientation = MatchOrientation.TargetUpAndForward,
                requestTime = Time.time
            });
            if (controllerMenu != null) controllerMenu.SetActive(false);
        }
    }
}
