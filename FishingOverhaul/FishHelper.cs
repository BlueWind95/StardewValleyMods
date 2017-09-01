using StardewValley;
using StardewValley.Locations;
using System.Collections.Generic;
using System.Linq;
using TehPers.Stardew.Framework;
using SFarmer = StardewValley.Farmer;
using static TehPers.Stardew.FishingOverhaul.Configs.ConfigFish;
using TehPers.Stardew.FishingOverhaul.Configs;

namespace TehPers.Stardew.FishingOverhaul {
    public class FishHelper {
        public static int GetRandomFish(int depth) {
            Season s = Helpers.ToSeason(Game1.currentSeason) ?? Season.SPRINGSUMMERFALLWINTER;
            WaterType w = Helpers.ConvertWaterType(Game1.currentLocation.getFishingLocation(Game1.player.getTileLocation())) ?? WaterType.BOTH;
            return FishHelper.GetRandomFish(w, s, Game1.isRaining ? Weather.RAINY : Weather.SUNNY, Game1.timeOfDay, depth, Game1.player.FishingLevel);
        }

        public static int GetRandomFish(WaterType water, Season s, Weather w, int time, int depth, int fishLevel) {
            ConfigMain config = ModFishing.Instance.Config;

            IEnumerable<KeyValuePair<int, FishData>> tempLocFish = FishHelper.GetPossibleFish(Game1.currentLocation.name, water, s, w, time, depth, fishLevel, Game1.currentLocation is MineShaft ? ((Game1.currentLocation as MineShaft).mineLevel) : -1);

            if (!config.ConfigLegendaries)
                tempLocFish = tempLocFish.Where(e => !FishHelper.IsLegendary(e.Key));

            if (!tempLocFish.Any())
                return FishHelper.GetRandomTrash();

            return tempLocFish.Select(e => new KeyValuePair<int, double>(e.Key, e.Value.Chance)).Choose(Game1.random);
        }

        public static IEnumerable<KeyValuePair<int, FishData>> GetPossibleFish(int depth, int mineLevel = -1) {
            Season s = Helpers.ToSeason(Game1.currentSeason) ?? Season.SPRINGSUMMERFALLWINTER;
            WaterType w = Helpers.ConvertWaterType(Game1.currentLocation.getFishingLocation(Game1.player.getTileLocation())) ?? WaterType.BOTH;
            return FishHelper.GetPossibleFish(Game1.currentLocation.name, w, s, Game1.isRaining ? Weather.RAINY : Weather.SUNNY, Game1.timeOfDay, depth, Game1.player.FishingLevel, mineLevel);
        }

        public static IEnumerable<KeyValuePair<int, FishData>> GetPossibleFish(string location, WaterType water, Season s, Weather w, int time, int depth, int fishLevel, int mineLevel = -1) {
            switch (location) {
                default:
                    water = WaterType.BOTH;
                    break;
            }

            if (!ModFishing.Instance.Config.PossibleFish.ContainsKey(location)) return new KeyValuePair<int, FishData>[] { };
            return ModFishing.Instance.Config.PossibleFish[location].Where(f => f.Value.MeetsCriteria(water, s, w, time, depth, fishLevel, mineLevel));
        }

        public static int GetRandomTrash() {
            return Game1.random.Next(167, 173);
        }

        public static bool IsTrash(int id) {
            return id >= 167 && id <= 172;
        }

        public static bool IsLegendary(int fish) {
            return fish == 159 || fish == 160 || fish == 163 || fish == 682 || fish == 775;
        }

        public static int GetStreak(SFarmer who) {
            return FishHelper.Streaks.ContainsKey(who) ? FishHelper.Streaks[who] : 0;
        }

        public static void SetStreak(SFarmer who, int streak) {
            FishHelper.Streaks[who] = streak;
        }

        public static Dictionary<SFarmer, int> Streaks = new Dictionary<SFarmer, int>();
    }
}
