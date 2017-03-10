﻿using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TehPers.Stardew.CombatOverhaul.Natures;
using TehPers.Stardew.SCCL.API;
using TehPers.Stardew.SCCL.Items;
using static TehPers.Stardew.CombatOverhaul.JunimoRod;

namespace TehPers.Stardew.CombatOverhaul {

    public class ModEntry : Mod {
        public static ModEntry INSTANCE;

        public ModConfig config;
        public List<UpdateEvent> updateEvents = new List<UpdateEvent>();
        public delegate bool UpdateEvent();

        public ContentInjector injector;

        public JunimoRod junimoRod = new JunimoRod("junimoRod");

        public ModEntry() {
            if (INSTANCE == null) INSTANCE = this;
        }

        public override void Entry(IModHelper helper) {
            this.config = helper.ReadConfig<ModConfig>();
            if (!config.ModEnabled) return;

            this.injector = ContentAPI.GetInjector(this, "CombatOverhaul");
            this.injector.RegisterDirectoryAssets(Path.Combine(this.Helper.DirectoryPath, "resources"));

            GameEvents.UpdateTick += UpdateTick;
            ControlEvents.KeyPressed += KeyPressed;
        }

        #region Events
        private void UpdateTick(object sender, EventArgs e) {
            for (int i = 0; i < updateEvents.Count; i++) {
                if (!updateEvents[i]()) {
                    updateEvents.RemoveAt(i);
                    i--;
                }
            }
        }

#pragma warning disable CRRSP01 // A misspelled word has been found.
        private void KeyPressed(object sender, EventArgsKeyPressed e) {
            if (e.KeyPressed == Keys.NumPad7)
                this.HijackWeapons();
            
            if (e.KeyPressed == Keys.OemPipe)
                Game1.player.addItemToInventory(new ModObject(junimoRod));

            if (e.KeyPressed == Keys.R)
                Game1.player.completelyStopAnimatingOrDoingAction();

            /*
                        if (Game1.player.CurrentTool is JunimoRod) {
                            JunimoRod rod = Game1.player.CurrentTool as JunimoRod;

                            if (e.KeyPressed == Keys.NumPad2)
                                rod.ActiveNature = new NatureFaythe();
                            else if (e.KeyPressed == Keys.NumPad4)
                                rod.ActiveNature = new NatureEnto();
                            else if (e.KeyPressed == Keys.NumPad6)
                                rod.ActiveNature = new NatureLusidity();
                            else if (e.KeyPressed == Keys.NumPad8)
                                rod.ActiveNature = new NatureLife();
                        }
                        */
        }
#pragma warning restore CRRSP01 // A misspelled word has been found.
        #endregion

        public void HijackWeapons() {
            /*
            for (int i = 0; i < Game1.player.items.Count; i++) {
                Item cur = Game1.player.items[i];
                if (cur is MeleeWeapon && !(cur is ModWeapon)) {
                    Game1.player.items[i] = new ModWeapon(cur as MeleeWeapon);
                }
            }
            */
        }
    }
}
