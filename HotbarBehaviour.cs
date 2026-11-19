using MelonLoader;
using UnityEngine;

namespace InventoryUI
{
    public class HotbarBehaviour : MonoBehaviour
    {
        public void Update()
        {
            if (Hotbar.Instance == null) return;
            if (ItemPicker.Instance?.IsVisible == true) return;
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    Hotbar.Instance.SelectSlot(i);
            }
        }

        public void LateUpdate()
        {
            Hotbar.Instance?.HandleOutsideClick();
            Hotbar.Instance?.UpdateHeldItem();
        }
    }
}
