using UnityEngine;
using UnityEngine.UI;
using MelonLoader;

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
                Hotbar.Instance?.Use();
            if (Input.GetMouseButtonDown(1))
                Hotbar.Instance?.AltUse();

            if (Input.anyKeyDown)
                Hotbar.Instance?.KeyDown();
        }
    }
}
