using InventoryFramework;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryUI
{
    [RegisterTypeInIl2Cpp]
    public class HotbarBehaviour : MonoBehaviour
    {
        public HotbarBehaviour(IntPtr ptr) : base(ptr) { }

        public void Update()
        {
            if (Hotbar.Instance == null) return;
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    Hotbar.Instance.SelectSlot(i);
            }

            if (Input.GetMouseButtonDown(0))
                InventoryManager.Instance.UseItem();
            if (Input.GetMouseButtonDown(1))
                InventoryManager.Instance.AltUseItem();
            if (Input.anyKeyDown)
                InventoryManager.Instance.KeyDown();
        }
    }
}
