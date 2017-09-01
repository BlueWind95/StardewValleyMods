using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using TehPers.Stardew.FishingOverhaul.Configs;
using static TehPers.Stardew.FishingOverhaul.Configs.ConfigFish;
using System.Linq;
using TehPers.Stardew.Framework;
using StardewValley.Tools;
using SFarmer = StardewValley.Farmer;

namespace TehPers.Stardew.FishingOverhaul {
    public class CustomBobberBar : BobberBar {

        private readonly IPrivateField<bool> _treasure;
        private readonly IPrivateField<bool> _treasureCaught;
        private IPrivateField<float> _treasurePosition;
        private IPrivateField<float> _treasureAppearTimer;
        private IPrivateField<float> _treasureScale;

        private readonly IPrivateField<float> _distanceFromCatching;
        private readonly IPrivateField<float> _treasureCatchLevel;

        private IPrivateField<float> _bobberBarPos;
        private readonly IPrivateField<float> _difficulty;
        private readonly IPrivateField<int> _fishQuality;
        private readonly IPrivateField<bool> _perfect;

        private readonly IPrivateField<SparklingText> _sparkleText;

        private float _lastDistanceFromCatching;
        private float _lastTreasureCatchLevel;
        private bool _perfectChanged;
        private bool _treasureChanged;
        private bool _notifiedFailOrSucceed;
        private readonly int _origStreak;
        private readonly int _origQuality;

        public SFarmer User;

        public CustomBobberBar(SFarmer user, int whichFish, float fishSize, bool treasure, int bobber, int waterDepth) : base(whichFish, fishSize, treasure, bobber) {
            this.User = user;
            this._origStreak = FishHelper.GetStreak(user);

            /* Private field hooks */
            _treasure = ModFishing.Instance.Helper.Reflection.GetPrivateField<bool>(this, "treasure");
            _treasureCaught = ModFishing.Instance.Helper.Reflection.GetPrivateField<bool>(this, "treasureCaught");
            _treasurePosition = ModFishing.Instance.Helper.Reflection.GetPrivateField<float>(this, "treasurePosition");
            _treasureAppearTimer = ModFishing.Instance.Helper.Reflection.GetPrivateField<float>(this, "treasureAppearTimer");
            _treasureScale = ModFishing.Instance.Helper.Reflection.GetPrivateField<float>(this, "treasureScale");

            _distanceFromCatching = ModFishing.Instance.Helper.Reflection.GetPrivateField<float>(this, "distanceFromCatching");
            _treasureCatchLevel = ModFishing.Instance.Helper.Reflection.GetPrivateField<float>(this, "treasureCatchLevel");

            _bobberBarPos = ModFishing.Instance.Helper.Reflection.GetPrivateField<float>(this, "bobberBarPos");
            _difficulty = ModFishing.Instance.Helper.Reflection.GetPrivateField<float>(this, "difficulty");
            _fishQuality = ModFishing.Instance.Helper.Reflection.GetPrivateField<int>(this, "fishQuality");
            _perfect = ModFishing.Instance.Helper.Reflection.GetPrivateField<bool>(this, "perfect");

            _sparkleText = ModFishing.Instance.Helper.Reflection.GetPrivateField<SparklingText>(this, "sparkleText");

            _lastDistanceFromCatching = _distanceFromCatching.GetValue();
            _lastTreasureCatchLevel = _treasureCatchLevel.GetValue();

            /* Actual code */
            ConfigMain config = ModFishing.Instance.Config;
            ConfigStrings strings = ModFishing.Instance.Strings;

            // Choose a random fish, this time using the custom fish selector
            FishingRod rod = Game1.player.CurrentTool as FishingRod;
            //int waterDepth = rod != null ? ModEntry.INSTANCE.Helper.Reflection.GetPrivateValue<int>(rod, "clearWaterDistance") : 0;

            // Applies difficulty modifier, including if fish isn't paying attention
            float difficulty = _difficulty.GetValue() * config.BaseDifficultyMult;
            difficulty *= 1f + config.DifficultyStreakEffect * this._origStreak;
            double difficultyChance = config.UnawareChance + user.LuckLevel * config.UnawareLuckLevelEffect + Game1.dailyLuck * config.UnawareDailyLuckEffect;
            if (Game1.random.NextDouble() < difficultyChance) {
                Game1.showGlobalMessage(string.Format(strings.UnawareFish, 1f - config.UnawareMult));
                difficulty *= config.UnawareMult;
            }
            _difficulty.SetValue(difficulty);

            // Adjusts quality to be increased by streak
            int fishQuality = _fishQuality.GetValue();
            this._origQuality = fishQuality;
            int qualityBonus = (int) Math.Floor((double) this._origStreak / config.StreakForIncreasedQuality);
            fishQuality = Math.Min(fishQuality + qualityBonus, 3);
            if (fishQuality == 3) fishQuality++; // Iridium-quality fish. Only possible through your perfect streak
            _fishQuality.SetValue(fishQuality);

            // Increase the user's perfect streak (this will be dropped to 0 if they don't get a perfect catch)
            if (this._origStreak >= config.StreakForIncreasedQuality)
                _sparkleText.SetValue(new SparklingText(Game1.dialogueFont, string.Format(strings.StreakDisplay, this._origStreak), Color.Yellow, Color.White, false, 0.1, 2500, -1, 500));
            FishHelper.SetStreak(user, this._origStreak + 1);
        }

        public override void update(GameTime time) {
            // Speed warp on normal catching
            float distanceFromCatching = _distanceFromCatching.GetValue();
            float delta = distanceFromCatching - this._lastDistanceFromCatching;
            distanceFromCatching += (ModFishing.Instance.Config.CatchSpeed - 1f) * delta;
            _lastDistanceFromCatching = distanceFromCatching;
            _distanceFromCatching.SetValue(distanceFromCatching);

            // Speed warp on treasure catching
            float treasureCatchLevel = _treasureCatchLevel.GetValue();
            delta = treasureCatchLevel - this._lastTreasureCatchLevel;
            treasureCatchLevel += (ModFishing.Instance.Config.TreasureCatchSpeed - 1f) * delta;
            _lastTreasureCatchLevel = treasureCatchLevel;
            _treasureCatchLevel.SetValue(treasureCatchLevel);

            bool perfect = _perfect.GetValue();
            bool treasure = _treasure.GetValue();
            bool treasureCaught = _treasureCaught.GetValue();

            ConfigStrings strings = ModFishing.Instance.Strings;

            // Check if still perfect, otherwise apply changes to loot
            if (!_perfectChanged && !perfect) {
                _perfectChanged = true;
                _fishQuality.SetValue(Math.Min(this._origQuality, 1));
                int streak = FishHelper.GetStreak(this.User);
                FishHelper.SetStreak(this.User, 0);
                if (this._origStreak >= ModFishing.Instance.Config.StreakForIncreasedQuality) {
                    if (!treasure)
                        Game1.showGlobalMessage(string.Format(strings.LostStreak, this._origStreak));
                    else
                        Game1.showGlobalMessage(string.Format(strings.WarnStreak, this._origStreak));
                }
            }

            if (!_treasureChanged && !perfect && treasure && treasureCaught) {
                _treasureChanged = true;
                int qualityBonus = (int) Math.Floor((double) this._origStreak / ModFishing.Instance.Config.StreakForIncreasedQuality);
                int quality = this._origQuality;
                quality = Math.Min(quality + qualityBonus, 3);
                if (quality == 3) quality++;
                _fishQuality.SetValue(quality);
            }

            base.update(time);

            distanceFromCatching = _distanceFromCatching.GetValue();

            if (distanceFromCatching <= 0.0) {
                // Failed to catch fish
                //FishHelper.setStreak(this.user, 0);
                if (!_notifiedFailOrSucceed && treasure) {
                    _notifiedFailOrSucceed = true;
                    if (this._origStreak >= ModFishing.Instance.Config.StreakForIncreasedQuality)
                        Game1.showGlobalMessage(string.Format(strings.LostStreak, this._origStreak));
                }
            } else if (distanceFromCatching >= 1.0) {
                // Succeeded in catching the fish
                if (!_notifiedFailOrSucceed && !perfect && treasure && treasureCaught) {
                    _notifiedFailOrSucceed = true;
                    if (this._origStreak >= ModFishing.Instance.Config.StreakForIncreasedQuality)
                        Game1.showGlobalMessage(string.Format(strings.KeptStreak, this._origStreak));
                    FishHelper.SetStreak(this.User, this._origStreak);
                }
            }
        }
    }
}
