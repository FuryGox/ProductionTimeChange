using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace ProductionTimeChange
{
    public enum CardCategory
    {
        All,
        Blueprint,
        Harvestable,
        Animal,
        Other
    }

    public class CardTimeEntry
    {
        public string Key { get; set; } = "";
        public string CardId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Details { get; set; } = "";
        public CardCategory Category { get; set; }
        public float OriginalTime { get; set; }
        public float CustomTime { get; set; }
        public string InputBuffer { get; set; } = "";
        public Action<float>? ApplyAction { get; set; }
    }

    public static class CardTimeManager
    {
        public static List<CardTimeEntry> Entries = new List<CardTimeEntry>();
        public static Dictionary<string, float> SavedTimes = new Dictionary<string, float>();
        public static bool IsInitialized { get; private set; } = false;
        private static string configPath = "";

        public static void Initialize(string modPath)
        {
            if (IsInitialized) return;

            if (GameDataLoader.instance == null || GameDataLoader.instance.CardDataPrefabs == null || GameDataLoader.instance.CardDataPrefabs.Count == 0)
            {
                return;
            }

            configPath = Path.Combine(modPath, "production_times.json");
            LoadConfigFile();

            Entries.Clear();

            // 1. Quét các công thức Blueprint / Subprint
            if (GameDataLoader.instance.BlueprintPrefabs != null)
            {
                foreach (Blueprint bp in GameDataLoader.instance.BlueprintPrefabs)
                {
                    if (bp == null || bp.Subprints == null) continue;

                    for (int i = 0; i < bp.Subprints.Count; i++)
                    {
                        Subprint sub = bp.Subprints[i];
                        if (sub == null) continue;

                        string key = $"bp_{bp.Id}_{i}";
                        string resultCardId = sub.ResultCard;
                        string resultName = "";

                        if (!string.IsNullOrEmpty(resultCardId))
                        {
                            CardData? resultPrefab = GameDataLoader.instance.GetCardFromId(resultCardId, false);
                            resultName = resultPrefab != null ? resultPrefab.Name : resultCardId;
                        }

                        string bpName = !string.IsNullOrEmpty(bp.Name) ? bp.Name : bp.Id;
                        string title = !string.IsNullOrEmpty(resultName) ? $"{resultName} ({bpName})" : bpName;
                        string recipeDetails = sub.RequiredCards != null && sub.RequiredCards.Length > 0 
                            ? string.Join(" + ", sub.RequiredCards) 
                            : "";

                        float origTime = sub.Time;
                        float curTime = SavedTimes.ContainsKey(key) ? SavedTimes[key] : origTime;

                        var entry = new CardTimeEntry
                        {
                            Key = key,
                            CardId = !string.IsNullOrEmpty(resultCardId) ? resultCardId : bp.Id,
                            DisplayName = title,
                            Details = recipeDetails,
                            Category = CardCategory.Blueprint,
                            OriginalTime = origTime,
                            CustomTime = curTime,
                            InputBuffer = curTime.ToString("0.##"),
                            ApplyAction = (newVal) => { sub.Time = newVal; }
                        };

                        // Áp dụng ngay nếu có cấu hình lưu trước đó
                        if (SavedTimes.ContainsKey(key))
                        {
                            sub.Time = curTime;
                        }

                        Entries.Add(entry);
                    }
                }
            }

            // 2. Quét các thẻ thu hoạch, động vật, nông nghiệp...
            foreach (CardData card in GameDataLoader.instance.CardDataPrefabs)
            {
                if (card == null) continue;

                string cardName = !string.IsNullOrEmpty(card.Name) ? card.Name : card.Id;

                // A. Harvestables (Cây, đá, mỏ...)
                if (card is Harvestable harvestable)
                {
                    string key = $"harvest_{card.Id}";
                    float origTime = harvestable.HarvestTime;
                    float curTime = SavedTimes.ContainsKey(key) ? SavedTimes[key] : origTime;

                    var entry = new CardTimeEntry
                    {
                        Key = key,
                        CardId = card.Id,
                        DisplayName = cardName,
                        Details = $"Loại: Thu hoạch (Harvestable) | ID: {card.Id}",
                        Category = CardCategory.Harvestable,
                        OriginalTime = origTime,
                        CustomTime = curTime,
                        InputBuffer = curTime.ToString("0.##"),
                        ApplyAction = (newVal) =>
                        {
                            harvestable.HarvestTime = newVal;
                            UpdateActiveHarvestables(card.Id, newVal);
                        }
                    };

                    if (SavedTimes.ContainsKey(key))
                    {
                        harvestable.HarvestTime = curTime;
                    }

                    Entries.Add(entry);
                }
                // B. Animals (Gà, bò, cừu...)
                else if (card is Animal animal)
                {
                    string key = $"animal_{card.Id}";
                    float origTime = animal.CreateTime;
                    float curTime = SavedTimes.ContainsKey(key) ? SavedTimes[key] : origTime;

                    var entry = new CardTimeEntry
                    {
                        Key = key,
                        CardId = card.Id,
                        DisplayName = cardName,
                        Details = $"Loại: Động vật (Animal) | ID: {card.Id}",
                        Category = CardCategory.Animal,
                        OriginalTime = origTime,
                        CustomTime = curTime,
                        InputBuffer = curTime.ToString("0.##"),
                        ApplyAction = (newVal) =>
                        {
                            animal.CreateTime = newVal;
                            UpdateActiveAnimals(card.Id, newVal);
                        }
                    };

                    if (SavedTimes.ContainsKey(key))
                    {
                        animal.CreateTime = curTime;
                    }

                    Entries.Add(entry);
                }
                // C. Farmland (Đất trồng)
                else if (card is Farmland farmland)
                {
                    string key = $"farm_{card.Id}";
                    float origTime = farmland.HarvestTime;
                    float curTime = SavedTimes.ContainsKey(key) ? SavedTimes[key] : origTime;

                    var entry = new CardTimeEntry
                    {
                        Key = key,
                        CardId = card.Id,
                        DisplayName = cardName,
                        Details = $"Loại: Đất trồng (Farmland) | ID: {card.Id}",
                        Category = CardCategory.Other,
                        OriginalTime = origTime,
                        CustomTime = curTime,
                        InputBuffer = curTime.ToString("0.##"),
                        ApplyAction = (newVal) =>
                        {
                            farmland.HarvestTime = newVal;
                            UpdateActiveFarmlands(card.Id, newVal);
                        }
                    };

                    if (SavedTimes.ContainsKey(key))
                    {
                        farmland.HarvestTime = curTime;
                    }

                    Entries.Add(entry);
                }
                // D. FishTrap (Bẫy cá)
                else if (card is FishTrap fishTrap)
                {
                    string key = $"fishtrap_{card.Id}";
                    float origTime = fishTrap.FishTime;
                    float curTime = SavedTimes.ContainsKey(key) ? SavedTimes[key] : origTime;

                    var entry = new CardTimeEntry
                    {
                        Key = key,
                        CardId = card.Id,
                        DisplayName = cardName,
                        Details = $"Loại: Bẫy cá (FishTrap) | ID: {card.Id}",
                        Category = CardCategory.Other,
                        OriginalTime = origTime,
                        CustomTime = curTime,
                        InputBuffer = curTime.ToString("0.##"),
                        ApplyAction = (newVal) =>
                        {
                            fishTrap.FishTime = newVal;
                            UpdateActiveFishTraps(card.Id, newVal);
                        }
                    };

                    if (SavedTimes.ContainsKey(key))
                    {
                        fishTrap.FishTime = curTime;
                    }

                    Entries.Add(entry);
                }
            }

            IsInitialized = true;
        }

        public static void LoadConfigFile()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, float>>(json);
                    if (dict != null)
                    {
                        SavedTimes = dict;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProductionTimeChange] Lỗi đọc file config: {ex.Message}");
            }
        }

        public static void SaveConfig()
        {
            try
            {
                SavedTimes.Clear();
                foreach (var entry in Entries)
                {
                    // Cập nhật giá trị tuỳ chỉnh nếu khác giá trị gốc
                    if (Math.Abs(entry.CustomTime - entry.OriginalTime) > 0.001f)
                    {
                        SavedTimes[entry.Key] = entry.CustomTime;
                    }
                    entry.ApplyAction?.Invoke(entry.CustomTime);
                }

                string? dir = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonConvert.SerializeObject(SavedTimes, Formatting.Indented);
                File.WriteAllText(configPath, json);

                // Đồng bộ cập nhật lên tất cả các thẻ đang tồn tại trên bàn chơi
                UpdateAllActiveWorldCards();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProductionTimeChange] Lỗi lưu file config: {ex.Message}");
            }
        }

        public static void ResetEntryToDefault(CardTimeEntry entry)
        {
            entry.CustomTime = entry.OriginalTime;
            entry.InputBuffer = entry.OriginalTime.ToString("0.##");
            entry.ApplyAction?.Invoke(entry.OriginalTime);
        }

        public static void ResetAllToDefault()
        {
            foreach (var entry in Entries)
            {
                ResetEntryToDefault(entry);
            }
            SaveConfig();
        }

        public static void UpdateAllActiveWorldCards()
        {
            if (WorldManager.instance == null || WorldManager.instance.AllCards == null) return;

            foreach (GameCard gc in WorldManager.instance.AllCards)
            {
                if (gc == null || gc.CardData == null) continue;
                string id = gc.CardData.Id;

                if (gc.CardData is Harvestable h && SavedTimes.TryGetValue($"harvest_{id}", out float ht))
                {
                    h.HarvestTime = ht;
                }
                else if (gc.CardData is Animal a && SavedTimes.TryGetValue($"animal_{id}", out float at))
                {
                    a.CreateTime = at;
                }
                else if (gc.CardData is Farmland f && SavedTimes.TryGetValue($"farm_{id}", out float ft))
                {
                    f.HarvestTime = ft;
                }
                else if (gc.CardData is FishTrap ftrap && SavedTimes.TryGetValue($"fishtrap_{id}", out float ftt))
                {
                    ftrap.FishTime = ftt;
                }
            }
        }

        private static void UpdateActiveHarvestables(string cardId, float newTime)
        {
            if (WorldManager.instance == null || WorldManager.instance.AllCards == null) return;
            foreach (var gc in WorldManager.instance.AllCards)
            {
                if (gc != null && gc.CardData is Harvestable h && h.Id == cardId)
                {
                    h.HarvestTime = newTime;
                }
            }
        }

        private static void UpdateActiveAnimals(string cardId, float newTime)
        {
            if (WorldManager.instance == null || WorldManager.instance.AllCards == null) return;
            foreach (var gc in WorldManager.instance.AllCards)
            {
                if (gc != null && gc.CardData is Animal a && a.Id == cardId)
                {
                    a.CreateTime = newTime;
                }
            }
        }

        private static void UpdateActiveFarmlands(string cardId, float newTime)
        {
            if (WorldManager.instance == null || WorldManager.instance.AllCards == null) return;
            foreach (var gc in WorldManager.instance.AllCards)
            {
                if (gc != null && gc.CardData is Farmland f && f.Id == cardId)
                {
                    f.HarvestTime = newTime;
                }
            }
        }

        private static void UpdateActiveFishTraps(string cardId, float newTime)
        {
            if (WorldManager.instance == null || WorldManager.instance.AllCards == null) return;
            foreach (var gc in WorldManager.instance.AllCards)
            {
                if (gc != null && gc.CardData is FishTrap f && f.Id == cardId)
                {
                    f.FishTime = newTime;
                }
            }
        }
    }
}
