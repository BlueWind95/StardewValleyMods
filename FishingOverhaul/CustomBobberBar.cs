using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValley.Tools;
using System;
using SFarmer = StardewValley.Farmer;

namespace TehPers.Stardew.FishingOverhaul {
    public class CustomBobberBar : BobberBar {

        private IPrivateField<bool> perfectField;
        public bool Perfect {
            get => this.perfectField.GetValue();
            set => this.perfectField.SetValue(value);
        }
        private IPrivateField<int> fishQualityField;
        public int FishQuality {
            get => this.fishQualityField.GetValue();
            set => this.fishQualityField.SetValue(value);
        }
        private IPrivateField<float> difficultyField;
        public float Difficulty {
            get => this.difficultyField.GetValue();
            set => this.difficultyField.SetValue(value);
        }

        private IPrivateField<float> distanceFromCatchingField;
        public float DistanceFromCatching {
            get => this.distanceFromCatchingField.GetValue();
            set => this.distanceFromCatchingField.SetValue(value);
        }
        private IPrivateField<SparklingText> sparkleTextField;

        private IPrivateField<float> bobberBarPosField;
        public float BobberBarPosition {
            get => bobberBarPosField.GetValue();
            set => bobberBarPosField.SetValue(value);
        }
        private IPrivateField<int> bobberBarHeightField;
        public int BobberBarHeight {
            get => bobberBarHeightField.GetValue();
            set => bobberBarHeightField.SetValue(value);
        }

        // Treasure
        private IPrivateField<bool> treasureField;
        public bool Treasure {
            get => this.treasureField.GetValue();
            set => this.treasureField.SetValue(value);
        }
        private IPrivateField<bool> treasureCaughtField;
        public bool TreasureCaught {
            get => this.treasureCaughtField.GetValue();
            set => this.treasureCaughtField.SetValue(value);
        }
        private IPrivateField<float> treasureCatchLevelField;
        public float TreasureCatchLevel {
            get => this.treasureCatchLevelField.GetValue();
            set => this.treasureCatchLevelField.SetValue(value);
        }
        private IPrivateField<float> treasurePositionField;
        public float TreasurePosition {
            get => treasurePositionField.GetValue();
            set => treasurePositionField.SetValue(value);
        }
        private IPrivateField<float> treasureAppearTimerField;
        private IPrivateField<float> treasureScaleField;

        public SFarmer User { get; }
        public int OriginalStreak { get; }
        public int OriginalQuality { get; }

        private float lastDistanceFromCatching;
        private float lastTreasureCatchLevel;
        private bool perfectChanged = false;
        private bool treasureChanged = false;
        private bool wonOrLost = false;

        public CustomBobberBar(SFarmer user, int whichFish, float fishSize, bool treasure, int bobber, int waterDepth) : base(whichFish, fishSize, treasure, bobber) {
            /* Private field hooks */
            this.HookPrivateFields();

            // Set some fields/properties to starting values
            this.lastDistanceFromCatching = this.DistanceFromCatching;
            this.lastTreasureCatchLevel = this.treasureCatchLevelField.GetValue();

            this.User = user;
            this.OriginalStreak = FishHelper.GetStreak(user);
            this.OriginalQuality = this.FishQuality;

            // Applies difficulty modifier, including if fish isn't paying attention
            float newDifficulty = this.Difficulty * ModFishing.Config.BaseDifficultyMult;
            newDifficulty *= 1f + ModFishing.Config.DifficultyStreakEffect * this.OriginalStreak;
            double difficultyChance = ModFishing.Config.UnawareChance + user.LuckLevel * ModFishing.Config.UnawareLuckLevelEffect + Game1.dailyLuck * ModFishing.Config.UnawareDailyLuckEffect;
            if (Game1.random.NextDouble() < difficultyChance) {
                Game1.showGlobalMessage(string.Format(ModFishing.Strings.UnawareFish, 1f - ModFishing.Config.UnawareMult));
                newDifficulty *= ModFishing.Config.UnawareMult;
            }
            this.Difficulty = newDifficulty;

            // Adjusts quality to be increased by streak
            int qualityBonus = this.OriginalStreak / ModFishing.Config.StreakForIncreasedQuality;
            this.FishQuality = Math.Min(this.FishQuality + qualityBonus, 3);
            if (this.FishQuality == 3) this.FishQuality++; // Iridium-quality fish. Only possible through your perfect streak
            this.fishQualityField.SetValue(this.FishQuality);

            // Increase the user's perfect streak (this will be dropped to 0 if they don't get a perfect catch)
            if (this.OriginalStreak >= ModFishing.Config.StreakForIncreasedQuality)
                this.sparkleTextField.SetValue(new SparklingText(Game1.dialogueFont, string.Format(ModFishing.Strings.StreakDisplay, this.OriginalStreak), Color.Yellow, Color.White, false, 0.1, 2500, -1, 500));
        }

        private void HookPrivateFields() {
            this.treasureField = ModFishing.Reflection.GetPrivateField<bool>(this, "treasure");
            this.treasureCaughtField = ModFishing.Reflection.GetPrivateField<bool>(this, "treasureCaught");
            this.treasurePositionField = ModFishing.Reflection.GetPrivateField<float>(this, "treasurePosition");
            this.treasureAppearTimerField = ModFishing.Reflection.GetPrivateField<float>(this, "treasureAppearTimer");
            this.treasureScaleField = ModFishing.Reflection.GetPrivateField<float>(this, "treasureScale");

            this.distanceFromCatchingField = ModFishing.Reflection.GetPrivateField<float>(this, "distanceFromCatching");
            this.treasureCatchLevelField = ModFishing.Reflection.GetPrivateField<float>(this, "treasureCatchLevel");

            this.bobberBarPosField = ModFishing.Reflection.GetPrivateField<float>(this, "bobberBarPos");
            this.bobberBarHeightField = ModFishing.Reflection.GetPrivateField<int>(this, "bobberBarHeight");

            this.difficultyField = ModFishing.Reflection.GetPrivateField<float>(this, "difficulty");
            this.fishQualityField = ModFishing.Reflection.GetPrivateField<int>(this, "fishQuality");
            this.perfectField = ModFishing.Reflection.GetPrivateField<bool>(this, "perfect");

            this.sparkleTextField = ModFishing.Reflection.GetPrivateField<SparklingText>(this, "sparkleText");
        }

        protected virtual void OnFishLost() {
            if (this.Treasure && this.OriginalStreak >= ModFishing.INSTANCE.config.StreakForIncreasedQuality) {
                if (FishHelper.GetStreak(this.User) == 0)
                    Game1.showGlobalMessage(string.Format(ModFishing.Strings.LostStreak, this.OriginalStreak));
                else
                    Game1.showGlobalMessage(string.Format(ModFishing.Strings.LostSomeStreak, this.OriginalStreak));
            }
        }

        protected virtual void OnFishCaught() {
            // Keep the streak if they caught the treasure and fish but wasn't perfect
            if (!this.Perfect && this.treasureChanged) {
                // Keep streak
                if (this.OriginalStreak >= ModFishing.Config.StreakForIncreasedQuality)
                    Game1.showGlobalMessage(string.Format(ModFishing.Strings.KeptStreak, this.OriginalStreak));
                FishHelper.SetStreak(this.User, this.OriginalStreak);

                // If they're over treasure when they catch the fish, they should get the treasure
                bool overTreasure = this.TreasurePosition + 16f <= this.BobberBarPosition - 32f + this.BobberBarHeight && this.TreasurePosition - 16f >= this.BobberBarPosition - 32f;
                if (overTreasure && this.Perfect)
                    this.Treasure = true;
            } else if (this.Perfect) {
                // Increase streak
                FishHelper.SetStreak(this.User, this.OriginalStreak + 1);
            }
        }

        protected virtual void OnPerfectLost() {
            // Lose quality
            this.FishQuality = Math.Min(this.OriginalQuality, 1);

            // Lose streak
            int streak = FishHelper.GetStreak(this.User);
            if (ModFishing.Config.PerfectLostPunishment.Value < 0) streak = 0;
            else streak = Math.Max(0, streak - ModFishing.Config.PerfectLostPunishment.Value);
            FishHelper.SetStreak(this.User, streak);

            // Display streak lost message
            if (this.OriginalStreak >= ModFishing.Config.StreakForIncreasedQuality) {
                if (this.Treasure)
                    Game1.showGlobalMessage(string.Format(ModFishing.Strings.WarnStreak, this.OriginalStreak));
                else if (FishHelper.GetStreak(this.User) == 0)
                    Game1.showGlobalMessage(string.Format(ModFishing.Strings.LostStreak, this.OriginalStreak));
                else
                    Game1.showGlobalMessage(string.Format(ModFishing.Strings.LostSomeStreak, this.OriginalStreak));
            }
        }

        protected virtual void OnTreasureCaught() {
            if (!this.Perfect) {
                int qualityBonus = this.OriginalStreak / ModFishing.Config.StreakForIncreasedQuality;
                int quality = this.OriginalQuality;
                quality = Math.Min(quality + qualityBonus, 3);
                if (quality == 3) quality++;
                this.FishQuality = quality;
            }
        }

        public override void update(GameTime time) {
            // Speed warp on normal catching
            float delta = this.DistanceFromCatching - this.lastDistanceFromCatching;
            this.DistanceFromCatching += (ModFishing.INSTANCE.config.CatchSpeed - 1f) * delta;
            this.lastDistanceFromCatching = this.DistanceFromCatching;

            // Speed warp on treasure catching
            delta = this.TreasureCatchLevel - this.lastTreasureCatchLevel;
            this.TreasureCatchLevel += (ModFishing.INSTANCE.config.TreasureCatchSpeed - 1f) * delta;
            this.lastTreasureCatchLevel = this.TreasureCatchLevel;

            // Check if still perfect
            if (!this.perfectChanged && !this.Perfect) {
                this.perfectChanged = true;
                OnPerfectLost();
            }

            // Check if treasure was caught
            if (!this.treasureChanged && this.Treasure && this.TreasureCaught) {
                this.treasureChanged = true;
                OnTreasureCaught();
            }

            base.update(time);

            // Check if they won or failed the minigame
            if (this.DistanceFromCatching <= 0.0 && !this.wonOrLost) {
                this.wonOrLost = true;
                OnFishLost();
            } else if (this.DistanceFromCatching >= 1.0 && !this.wonOrLost) {
                this.wonOrLost = true;
                OnFishCaught();
            }
        }
    }
}
