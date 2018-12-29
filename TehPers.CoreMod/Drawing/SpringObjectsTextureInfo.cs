﻿using StardewValley;
using TehPers.CoreMod.Api.Drawing;

namespace TehPers.CoreMod.Drawing {
    internal class SpringObjectsTextureInfo : ITextureSourceInfo {
        /// <inheritdoc />
        public string TextureName { get; } = @"Maps\springobjects";

        /// <inheritdoc />
        public int GetIndexFromUV(int u, int v) {
            int tileX = u / 16;
            int tileY = v / 16;
            int widthInTiles = Game1.objectSpriteSheet.Width / 16;
            return tileY * widthInTiles + tileX;
        }

        public static ITextureSourceInfo Value { get; } = new SpringObjectsTextureInfo();
    }
}