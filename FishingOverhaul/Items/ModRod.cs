using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TehPers.Stardew.SCCL.Items;
using StardewValley;

namespace TehPers.Stardew.FishingOverhaul.Items {
    public class ModRod : FishingRod, ISavable {

        public ItemTemplate Template { get; set; }
        public Dictionary<string, object> Data { get; set; }

        public ModRod(ItemTemplate template) : this(template, 0) { }

        public ModRod(ItemTemplate template, int upgradeLevel) : base(upgradeLevel) {
            ModFishing.Logger.Log("Replaced a fishing rod");
        }

        #region Overrides
        public override string DisplayName => Template.GetName(Data);
        public override string getDescription() => Template.GetDescription(Data);
        public override int salePrice() => Template.GetPrice(Data);
        public override void DoFunction(GameLocation location, int x, int y, int power, StardewValley.Farmer who) => Template.DoFunction(Data, base.DoFunction, location, x, y, power, who);
        #endregion
    }
}
