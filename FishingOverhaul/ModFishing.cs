using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using TehPers.Stardew.FishingOverhaul.Configs;
using TehPers.Stardew.Framework;
using SObject = StardewValley.Object;
using SFarmer = StardewValley.Farmer;
using static TehPers.Stardew.FishingOverhaul.MethodReplacer;
using TehPers.Stardew.SCCL.Items;
using TehPers.Stardew.FishingOverhaul.Items;

namespace TehPers.Stardew.FishingOverhaul {
    /// <summary>The mod entry point.</summary>
    public class ModFishing : Mod {
        public const bool DEBUG = false;
        public static ModFishing INSTANCE;

        // Shortcuts
        public static ConfigStrings Strings => INSTANCE.strings;
        public static ConfigMain Config => INSTANCE.config;
        public static ConfigFish Fish => INSTANCE.fishConfig;
        public static ConfigTreasure Treasure => INSTANCE.treasureConfig;
        public static SaveFile Save => INSTANCE.save;
        public static IModHelper ModHelper => INSTANCE.Helper;
        public static IReflectionHelper Reflection => ModHelper.Reflection;
        public static IMonitor Logger => INSTANCE.Monitor;

        public ConfigMain config;
        public ConfigTreasure treasureConfig;
        public ConfigFish fishConfig;
        public ConfigStrings strings;
        public SaveFile save;
        internal ItemLoader loader;

        private Dictionary<SObject, float> lastDurability = new Dictionary<SObject, float>();

        public ModFishing() {
            if (ModFishing.INSTANCE == null) ModFishing.INSTANCE = this;
        }

        /// <summary>Initialize the mod.</summary>
        /// <param name="helper">Provides methods for interacting with the mod directory, such as read/writing a config file or custom JSON files.</param>
        public override void Entry(IModHelper helper) {
            // Load configs
            this.config = helper.ReadConfig<ConfigMain>();
            this.treasureConfig = helper.ReadJsonFile<ConfigTreasure>("treasure.json") ?? new ConfigTreasure();
            this.fishConfig = helper.ReadJsonFile<ConfigFish>("fish.json") ?? new ConfigFish();

            // Make sure the extra configs are generated
            helper.WriteJsonFile("treasure.json", this.treasureConfig);
            helper.WriteJsonFile("fish.json", this.fishConfig);

            this.config.PostLoad();
            helper.WriteConfig(this.config);

            OnLanguageChange(LocalizedContentManager.CurrentLanguageCode);

            // Stop here if the mod is disabled
            if (!this.config.ModEnabled) return;

            // Create item loader
            this.loader = new ItemLoader();

            // Events
            GameEvents.UpdateTick += UpdateTick;
            ControlEvents.KeyPressed += KeyPressed;
            LocalizedContentManager.OnLanguageChange += OnLanguageChange;
            SaveEvents.BeforeSave += BeforeSave;
            SaveEvents.AfterLoad += AfterLoad;
        }

        #region Events
        private void UpdateTick(object sender, EventArgs e) {
            // Auto-populate the fish config file if it's empty
            if (this.fishConfig.PossibleFish == null) {
                this.fishConfig.PopulateData();
                this.Helper.WriteJsonFile("fish.json", this.fishConfig);
            }

            TryChangeFishingTreasure();

            if (Game1.player.CurrentTool is FishingRod) {
                FishingRod rod = Game1.player.CurrentTool as FishingRod;
                SObject bobber = rod.attachments[1];
                if (bobber != null) {
                    if (this.lastDurability.ContainsKey(bobber)) {
                        float last = this.lastDurability[bobber];
                        bobber.scale.Y = last + (bobber.scale.Y - last) * this.config.TackleDestroyRate;
                        if (bobber.scale.Y <= 0) {
                            this.lastDurability.Remove(bobber);
                            rod.attachments[1] = null;
                            try {
                                Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14086"));
                            } catch (Exception) {
                                Game1.showGlobalMessage("Your tackle broke!");
                                this.Monitor.Log("Could not load string for broken tackle", LogLevel.Warn);
                            }
                        } else this.lastDurability[bobber] = bobber.scale.Y;
                    } else this.lastDurability[bobber] = bobber.scale.Y;
                }
            }

            //loader.ForEach(item => item is FishingRod && !(item is ModRod), item => new ModRod(new ItemTemplate("fishingRod")));
        }

        private void KeyPressed(object sender, EventArgsKeyPressed e) {
            if (Enum.TryParse(this.config.GetFishInWaterKey, out Keys getFishKey) && e.KeyPressed == getFishKey) {
                if (Game1.currentLocation != null) {
                    int[] possibleFish;
                    if (Game1.currentLocation is MineShaft)
                        possibleFish = FishHelper.GetPossibleFish(5, (Game1.currentLocation as MineShaft).mineLevel).Select(f => f.Key).ToArray();
                    else
                        possibleFish = FishHelper.GetPossibleFish(5, -1).Select(f => f.Key).ToArray();
                    Dictionary<int, string> fish = Game1.content.Load<Dictionary<int, string>>("Data\\Fish");
                    string[] fishByName = (
                        from id in possibleFish
                        let data = fish[id].Split('/')
                        select data.Length > 13 ? data[13] : data[0]
                        ).ToArray();
                    if (fishByName.Length > 0)
                        Game1.showGlobalMessage(string.Format(this.strings.PossibleFish, string.Join<string>(", ", fishByName)));
                    else
                        Game1.showGlobalMessage(this.strings.NoPossibleFish);
                }
            }
        }

        private void OnLanguageChange(LocalizedContentManager.LanguageCode code) {
            //Directory.CreateDirectory(Path.Combine(this.Helper.DirectoryPath, "Translations"));
            this.strings = Helper.ReadJsonFile<ConfigStrings>("Translations/" + Helpers.GetLanguageCode() + ".json") ?? new ConfigStrings();
            Helper.WriteJsonFile("Translations/" + Helpers.GetLanguageCode() + ".json", this.strings);
        }

        private void BeforeSave(object sender, EventArgs e) {
            this.save.FishingStreak = FishHelper.GetStreak(Game1.player);
            this.Helper.WriteJsonFile<SaveFile>($"{Constants.SaveFolderName}/FishingOverhaul.json", this.save);
        }

        private void AfterLoad(object sender, EventArgs e) {
            this.save = this.Helper.ReadJsonFile<SaveFile>($"{Constants.SaveFolderName}/FishingOverhaul.json") ?? new SaveFile();
            FishHelper.SetStreak(Game1.player, save.FishingStreak);
        }
        #endregion

        private void TryChangeFishingTreasure() {
            if (Game1.player.CurrentTool is FishingRod rod) {
                // Look through all animated sprites in the main game
                if (this.config.OverrideFishing) {
                    foreach (TemporaryAnimatedSprite anim in Game1.screenOverlayTempSprites) {
                        if (anim.endFunction == rod.startMinigameEndFunction) {
                            this.Monitor.Log("Overriding bobber bar", LogLevel.Trace);
                            anim.endFunction = (i => FishingRodOverrides.StartMinigameEndFunction(rod, i));
                        }
                    }
                }

                // Look through all animated sprites in the fishing rod
                if (this.config.OverrideTreasureLoot) {
                    foreach (TemporaryAnimatedSprite anim in rod.animations) {
                        if (anim.endFunction == rod.openTreasureMenuEndFunction) {
                            this.Monitor.Log("Overriding treasure animation end function", LogLevel.Trace);
                            anim.endFunction = (i => FishingRodOverrides.OpenTreasureMenuEndFunction(rod, i));
                        }
                    }
                }
            }
        }

        #region Fish Data Generator
        public void GenerateWeightedFishData(string path) {
            IEnumerable<FishInfo> fishList = (from fishInfo in this.config.PossibleFish
                                              let loc = fishInfo.Key
                                              from entry in fishInfo.Value
                                              let fish = entry.Key
                                              let data = entry.Value
                                              let seasons = data.Season
                                              let chance = data.Chance
                                              select new FishInfo() { Seasons = seasons, Location = loc, Fish = fish, Chance = chance }
                                              );

            Dictionary<string, Dictionary<string, Dictionary<int, double>>> result = new Dictionary<string, Dictionary<string, Dictionary<int, double>>>();

            // Spring
            Season s = Season.SPRING;
            string str = "spring";
            result[str] = new Dictionary<string, Dictionary<int, double>>();
            IEnumerable<FishInfo> seasonalFish = fishList.Where((info) => (info.Seasons & s) > 0);
            foreach (string loc in seasonalFish.Select(info => info.Location).ToHashSet()) {
                IEnumerable<FishInfo> locFish = seasonalFish.Where(fish => fish.Location == loc);
                result[str][loc] = locFish.ToDictionary(fish => fish.Fish, fish => fish.Chance);
            }

            // Summer
            s = Season.SUMMER;
            str = "summer";
            result[str] = new Dictionary<string, Dictionary<int, double>>();
            seasonalFish = fishList.Where((info) => (info.Seasons & s) > 0);
            foreach (string loc in seasonalFish.Select(info => info.Location).ToHashSet()) {
                IEnumerable<FishInfo> locFish = seasonalFish.Where(fish => fish.Location == loc);
                result[str][loc] = locFish.ToDictionary(fish => fish.Fish, fish => fish.Chance);
            }

            // Fall
            s = Season.FALL;
            str = "fall";
            result[str] = new Dictionary<string, Dictionary<int, double>>();
            seasonalFish = fishList.Where((info) => (info.Seasons & s) > 0);
            foreach (string loc in seasonalFish.Select(info => info.Location).ToHashSet()) {
                IEnumerable<FishInfo> locFish = seasonalFish.Where(fish => fish.Location == loc);
                result[str][loc] = locFish.ToDictionary(fish => fish.Fish, fish => fish.Chance);
            }

            // Winter
            s = Season.WINTER;
            str = "winter";
            result[str] = new Dictionary<string, Dictionary<int, double>>();
            seasonalFish = fishList.Where((info) => (info.Seasons & s) > 0);
            foreach (string loc in seasonalFish.Select(info => info.Location).ToHashSet()) {
                IEnumerable<FishInfo> locFish = seasonalFish.Where(fish => fish.Location == loc);
                result[str][loc] = locFish.ToDictionary(fish => fish.Fish, fish => fish.Chance);
            }

            this.Helper.WriteJsonFile(nameof(path), result);
        }

        private class FishInfo {
            public Season Seasons { get; set; }
            public string Location { get; set; }
            public double Chance { get; set; }
            public int Fish { get; set; }
        }
        #endregion
    }
}