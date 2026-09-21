using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProductionTimeChange
{
    public class ProductionTimeMod : Mod
    {
        public static ModLogger? ModLogger;

        public override void Ready()
        {
            ModLogger = Logger;
            PathHelper.ModDirectory = Path;

            try
            {
                Harmony.PatchAll();
                Logger.Log("Production Time Manager đã sẵn sàng!");
            }
            catch (Exception ex)
            {
                Logger.Log($"Lỗi khi patch Harmony: {ex.Message}");
            }
        }

        private void Update()
        {
            // Tự động khởi tạo dữ liệu khi GameDataLoader đã load đủ prefabs
            if (!CardTimeManager.IsInitialized)
            {
                if (GameDataLoader.instance != null &&
                    GameDataLoader.instance.CardDataPrefabs != null &&
                    GameDataLoader.instance.CardDataPrefabs.Count > 0)
                {
                    CardTimeManager.Initialize(PathHelper.GetModDirectory());
                }
            }

            // Bắt sự kiện phím P để bật/tắt Panel
            bool pPressed = false;
            try
            {
                if (Keyboard.current != null && Keyboard.current[Key.P].wasPressedThisFrame)
                {
                    pPressed = true;
                }
            }
            catch
            {
                // Fallback nếu new Input System chưa sẵn sàng
            }

            if (!pPressed && InputController.instance != null)
            {
                try
                {
                    if (InputController.instance.GetKeyDown(Key.P))
                    {
                        pPressed = true;
                    }
                }
                catch {}
            }

            if (pPressed)
            {
                ProductionTimeUI.Toggle();
            }

            ProductionTimeUI.Update();
        }

        private void OnGUI()
        {
            ProductionTimeUI.Draw();
        }
    }

    [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.CreateCard), new Type[] { typeof(Vector3), typeof(CardData), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    public static class Patch_CreateCard
    {
        public static void Postfix(CardData __result)
        {
            if (__result == null || !CardTimeManager.IsInitialized) return;

            string id = __result.Id;
            if (__result is Harvestable h && CardTimeManager.SavedTimes.TryGetValue($"harvest_{id}", out float ht))
            {
                h.HarvestTime = ht;
            }
            else if (__result is Animal a && CardTimeManager.SavedTimes.TryGetValue($"animal_{id}", out float at))
            {
                a.CreateTime = at;
            }
            else if (__result is Farmland f && CardTimeManager.SavedTimes.TryGetValue($"farm_{id}", out float ft))
            {
                f.HarvestTime = ft;
            }
            else if (__result is FishTrap ftrap && CardTimeManager.SavedTimes.TryGetValue($"fishtrap_{id}", out float ftt))
            {
                ftrap.FishTime = ftt;
            }
        }
    }
}