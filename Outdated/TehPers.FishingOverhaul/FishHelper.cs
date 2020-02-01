﻿using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;
using TehPers.CoreMod.Api.Environment;
using TehPers.CoreMod.Api.Extensions;
using TehPers.CoreMod.Api.Structs;
using TehPers.CoreMod.Api.Weighted;
using TehPers.FishingOverhaul.Api;
using TehPers.FishingOverhaul.Configs;
using SFarmer = StardewValley.Farmer;

namespace TehPers.FishingOverhaul
{
    internal class FishHelper
    {
        public static int? GetRandomFish(Farmer who)
        {
            return FishHelper.GetRandomFish(ModFishing.Instance.Api.GetPossibleFish(who));
        }

        public static int? GetRandomFish(IEnumerable<IWeightedElement<int?>> possibleFish)
        {
            possibleFish = possibleFish.ToArray();

            // No possible fish
            if (!possibleFish.Any())
                return null;

            // Select a fish
            return possibleFish.Choose(Game1.random);
        }

        public static int? GetRandomTrash(Farmer who)
        {
            return FishHelper.GetRandomTrash(ModFishing.Instance.Api.GetTrashData(who));
        }

        public static int? GetRandomTrash(Farmer who, string locationName, WaterTypes waterTypes, SDateTime dateTime, Weather weather, int fishingLevel, int? mineLevel)
        {
            return FishHelper.GetRandomTrash(ModFishing.Instance.Api.GetTrashData(who, locationName, waterTypes, dateTime, weather, fishingLevel, mineLevel));
        }

        public static int? GetRandomTrash(IEnumerable<ITrashData> trashData)
        {
            trashData = trashData.ToArray();

            // No possible trash
            if (!trashData.Any())
                return null;

            // Select a trash data
            var data = trashData.Choose(Game1.random);
            var ids = data.PossibleIds.ToArray();

            // Select a trash ID
            return !ids.Any() ? (int?)null : ids.ToWeighted(id => 1D).Choose(Game1.random);
        }

        public static bool IsTrash(int id)
        {
            return ModFishing.Instance.Api.GetTrashData().Any(t => t.PossibleIds.Contains(id));
        }

        public static float GetRawFishChance(SFarmer who)
        {
            var config = ModFishing.Instance.MainConfig.GlobalFishSettings;

            // Calculate chance
            var chance = config.FishBaseChance;
            // float chance2 = chance + who.FishingLevel * config.FishLevelEffect;
            // float chance3 = chance2 + who.LuckLevel * config.FishLuckLevelEffect;
            // float chance4 = chance3 + (float) Game1.dailyLuck * config.FishDailyLuckEffect;
            // float actualChance = chance4 + ModFishing.Instance.Api.GetStreak(who) * config.FishStreakEffect;

            chance += who.FishingLevel * config.FishLevelEffect;
            chance += who.LuckLevel * config.FishLuckLevelEffect;
            chance += (float)Game1.player.DailyLuck * config.FishDailyLuckEffect;
            chance += ModFishing.Instance.Api.GetStreak(who) * config.FishStreakEffect;

            return chance;
        }

        public static float GetRawTreasureChance(SFarmer who, FishingRod rod)
        {
            var config = ModFishing.Instance.MainConfig.GlobalTreasureSettings;

            // Calculate chance
            var chance = config.TreasureChance;
            chance += who.LuckLevel * config.TreasureLuckLevelEffect;
            chance += (float)Game1.player.DailyLuck * config.TreasureDailyLuckEffect;
            chance += config.TreasureStreakEffect * ModFishing.Instance.Api.GetStreak(who);
            if (rod.getBaitAttachmentIndex() == 703)
                chance += config.TreasureBaitEffect;
            if (rod.getBobberAttachmentIndex() == 693)
                chance += config.TreasureBobberEffect;
            if (who.professions.Contains(9))
                chance += config.TreasureChance;

            return Math.Min(chance, config.MaxTreasureChance);
        }

        public static float GetRawUnawareChance(SFarmer who)
        {
            var config = ModFishing.Instance.MainConfig.UnawareSettings;

            // Calculate chance
            var chance = config.UnawareChance;
            chance += who.LuckLevel * config.UnawareLuckLevelEffect;
            chance += (float)Game1.player.DailyLuck * config.UnawareDailyLuckEffect;

            return chance;
        }
    }
}