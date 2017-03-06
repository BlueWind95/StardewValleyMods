﻿using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TehPers.Stardew.SCCL.Configs;
using xTile.Dimensions;

namespace TehPers.Stardew.SCCL.API {
    public sealed class ContentInjector {
        private static MethodInfo registerAssetMethod = typeof(ContentInjector).GetMethods().Where(m => m.Name == "RegisterAsset" && m.IsGenericMethod).First();

        internal Dictionary<string, HashSet<object>> ModContent { get; } = new Dictionary<string, HashSet<object>>();

        /// <summary>Mod that created this injector</summary>
        private Mod Owner { get; }

        /// <summary>Injector name</summary>
        public string Name { get; }

        public bool Enabled {
            get {
                return !ModEntry.INSTANCE.config.DisabledMods.Contains(this.Name);
            }
            set {
                ModConfig config = ModEntry.INSTANCE.config;
                bool changed = value != this.Enabled;

                if (value)
                    config.DisabledMods.Remove(this.Name);
                else
                    config.DisabledMods.Add(this.Name);

                if (changed) {
                    ModEntry.INSTANCE.Helper.WriteConfig(config);
                    foreach (string asset in ModContent.Keys)
                        this.RefreshAsset(asset);
                }
            }
        }

        internal ContentInjector(Mod owner, string name) {
            this.Owner = owner;
            this.Name = name;
        }

        /**
         * <summary>Registers the given asset with the given type and asset name</summary>
         * <param name="assetName">The name of the asset</param>
         * <param name="asset">The asset</param>
         * <typeparam name="T">The type of the asset to merge. If unknown, use non-generic <seealso cref="RegisterAsset(string, object)"/> instead</typeparam>
         * <returns>Whether the asset was registered successfully. If false, then T was probably incompatible with the asset</returns>
         **/
        public bool RegisterAsset<T>(string assetName, T asset) {
            assetName = assetName.Replace('/', '\\');

            if (!ModContent.ContainsKey(assetName))
                ModContent[assetName] = new HashSet<object>();

            if (asset is Texture2D)
                this.RequestTextureSize(assetName, new Size((asset as Texture2D).Width, (asset as Texture2D).Height));

            ModContent[assetName].Add(asset);
            this.RefreshAsset(assetName);

            ModEntry.INSTANCE.Monitor.Log(string.Format("[{2}] Registered {0} ({1})", assetName, typeof(T).ToString(), Name), LogLevel.Trace);

            return true;
        }

        /**
         * <summary>Registers the given asset with the given asset name</summary>
         * <param name="assetName">The name of the asset</param>
         * <param name="asset">The asset to merge</param>
         * <returns>Whether the asset was registered successfully. If false, then the asset was probably the wrong type</returns>
         **/
        public bool RegisterAsset(string assetName, object asset) {
            return (bool) registerAssetMethod.MakeGenericMethod(asset.GetType()).Invoke(this, new object[] { assetName, asset });
        }


        /**
         * <summary>Removes the given asset with the given asset name</summary>
         * <param name="assetName">The name of the asset</param>
         * <param name="asset">The asset to remove</param>
         * <returns>Whether the asset was removed successfully</returns>
         **/
        public bool UnregisterAsset(string assetName, object asset) {
            assetName = assetName.Replace('/', '\\');

            if (ModContent.ContainsKey(assetName) && ModContent[assetName].Remove(asset)) {
                ModEntry.INSTANCE.Monitor.Log(string.Format("[{2}] Unregistered {0} ({1})", assetName, asset.GetType().ToString(), Name), LogLevel.Trace);
                this.RefreshAsset(assetName);
                return true;
            }
            return false;
        }

        /**
         * <summary>Mark the given asset to be regenerated</summary>
         * <param name="assetName">The name of the asset</param>
         * <returns>Whether the asset needs to be marked</returns>
         **/
        public bool RefreshAsset(string assetName) {
            return ModEntry.INSTANCE.merger.Dirty.Add(assetName);
        }

        /**
         * <summary>Sets the minimum required size for the texture. Call this before the texture is loaded, during mod entry.</summary>
         * <param name="assetName">The name of the asset</param>
         * <param name="size">The required size of the asset. The final asset might not be this size.</param>
         * <returns>The required texture width, or null if it shouldn't change.</returns>
         * <remarks>If the original asset is larger than the size that was set, that size will be used. Mod textures will be clipped if needed.</remarks>
         **/
        public void RequestTextureSize(string assetName, Size size) {
            ContentMerger merger = ModEntry.INSTANCE.merger;
            if (merger.RequiredSize.ContainsKey(assetName)) {
                Size orig = merger.RequiredSize[assetName];
                merger.RequiredSize[assetName] = new Size(Math.Max(size.Width, orig.Width), Math.Max(size.Height, orig.Height));
            }
            merger.RequiredSize[assetName] = size;
        }
    }
}
