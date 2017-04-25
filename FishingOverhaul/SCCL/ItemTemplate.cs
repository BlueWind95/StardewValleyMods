using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TehPers.Stardew.SCCL.Items {
    public abstract class ItemTemplate {
        public static Dictionary<string, ItemTemplate> Templates { get; } = new Dictionary<string, ItemTemplate>();

        public string ID { get; }

        public ItemTemplate(string id) {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException($"{nameof(id)} is null or empty.", nameof(id));
            if (Templates.ContainsKey(id)) throw new ArgumentException($"{id} has already been registered.", nameof(id));
            Templates[id] = this;

            this.ID = id;
        }

        public abstract string GetName(Dictionary<string, object> data);

        public abstract string GetDescription(Dictionary<string, object> data);

        public virtual int GetPrice(Dictionary<string, object> data) => -1;

        public virtual int GetEdibility(Dictionary<string, object> data) => -300;

        public virtual bool IsRecipe(Dictionary<string, object> data) => false;

        public delegate void BaseDoFunction(GameLocation location, int x, int y, int power, StardewValley.Farmer who);
        public virtual void DoFunction(Dictionary<string, object> data, BaseDoFunction baseFunc, GameLocation location, int x, int y, int power, StardewValley.Farmer who) => baseFunc(location, x, y, power, who);

        // TODO: Replace Texture2D with a structure that supports texture and source rectangle
        public abstract Texture2D GetTexture(Dictionary<string, object> data);
    }
}
