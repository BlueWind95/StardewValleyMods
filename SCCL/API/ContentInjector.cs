using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TehPers.Stardew.SCCL.Configs;
using TehPers.Stardew.SCCL.Content;
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
            get => !ModEntry.INSTANCE.config.DisabledMods.Contains(this.Name);
            set {
                ModConfig config = ModEntry.INSTANCE.config;
                bool changed = value != this.Enabled;

                if (value)
                    config.DisabledMods.Remove(this.Name);
                else
                    config.DisabledMods.Add(this.Name);

                if (changed) {
                    ModEntry.INSTANCE.Helper.WriteConfig(config);
                    foreach (string asset in this.ModContent.Keys)
                        RefreshAsset(asset);
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
            if (assetName.EndsWith(".xnb")) assetName = assetName.Substring(0, assetName.Length - 4);

            if (!this.ModContent.ContainsKey(assetName))
                this.ModContent[assetName] = new HashSet<object>();

            if (asset is Texture2D) this.ModContent[assetName].Add(new OffsetTexture2D(asset as Texture2D));
            else this.ModContent[assetName].Add(asset);

            RefreshAsset(assetName);

            ModEntry.INSTANCE.Monitor.Log($"[{this.Name}] Registered {assetName} ({typeof(T).ToString()})", LogLevel.Trace);

            return true;
        }

        /**
         * <summary>Registers the given asset with the given asset name</summary>
         * <param name="assetName">The name of the asset</param>
         * <param name="asset">The asset</param>
         * <returns>Whether the asset was registered successfully. If false, then the asset was probably the wrong type</returns>
         **/
        public bool RegisterAsset(string assetName, object asset) {
            return (bool) registerAssetMethod.MakeGenericMethod(asset.GetType()).Invoke(this, new object[] { assetName, asset });
        }

        /**
         * <summary>Registers the given texture with the given asset name and offset. Will not automatically request the required size.</summary>
         * <param name="assetName">The name of the asset</param>
         * <param name="texture">The texture</param>
         * <param name="xOff">The location in the original texture to merge it in. For example, if xOff is 50, the mod texture will begin at x=50.</param>
         * <param name="yOff">The location in the original texture to merge it in. For example, if yOff is 50, the mod texture will begin at y=50.</param>
         * <returns>Whether the texture was registered successfully. If false, then the asset was probably the wrong type</returns>
         **/
        public bool RegisterTexture(string assetName, Texture2D texture, int xOff = 0, int yOff = 0) {
            return RegisterAsset(assetName, new OffsetTexture2D(texture, xOff, yOff));
        }

        /// <summary>Registers all the assets relative to the directory <paramref name="path"/></summary>
        /// <param name="path">The path to the directory</param>
        public void RegisterDirectoryAssets(string path) {
            if (!Directory.Exists(path)) return;
            List<string> checkDirs = new List<string> { path };
            while (checkDirs.Count > 0) {
                string dir = checkDirs[0];
                checkDirs.RemoveAt(0);
                checkDirs.AddRange(Directory.GetDirectories(dir));

                // Go through each xnb file
                foreach (string xnb in Directory.GetFiles(dir, "*.xnb")) {
                    RegisterFileAsset(path, xnb);
                }

                // Go through each png file
                foreach (string png in Directory.GetFiles(dir, "*.png")) {
                    RegisterFileAsset(path, png);
                }
            }
        }

        /// <summary>Registers the xnb or png file at the given path with the given base directory</summary>
        /// <param name="baseDir">The root directory of your content</param>
        /// <param name="filePath">The absolute path to the file</param>
        /// <returns>Whether the asset was successfully registered</returns>
        public bool RegisterFileAsset(string baseDir, string filePath) {
            if (filePath.EndsWith(".xnb", StringComparison.CurrentCultureIgnoreCase)) {
                try {
                    string localModPath = Helpers.LocalizePath(ModEntry.CONTENT_DIRECTORY, filePath);
                    localModPath = localModPath.Substring(0, localModPath.Length - 4).Replace('/', '\\');
                    object modAsset = ModEntry.INSTANCE.contentManager.Load<object>(localModPath);

                    if (modAsset != null) {
                        return RegisterAsset(Helpers.LocalizePath(baseDir, filePath), modAsset);
                    }
                } catch (Exception) {
                    ModEntry.INSTANCE.Monitor.Log($"Unable to load {filePath}", LogLevel.Warn);
                }
            } else if (filePath.EndsWith(".png", StringComparison.CurrentCultureIgnoreCase)) {
                try {
                    using (FileStream stream = File.Open(filePath, FileMode.Open)) {
                        Texture2D modAsset = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);

                        if (modAsset != null) {
                            return RegisterAsset(Helpers.LocalizePath(baseDir, filePath.Substring(0, filePath.Length - 4)), modAsset);
                        }
                    }
                } catch (Exception) {
                    ModEntry.INSTANCE.Monitor.Log($"Unable to load {filePath}", LogLevel.Warn);
                }
            }
            return false;
        }

        /**
         * <summary>Removes the given asset with the given asset name</summary>
         * <param name="assetName">The name of the asset</param>
         * <param name="asset">The asset to remove</param>
         * <returns>Whether the asset was removed successfully</returns>
         **/
        public bool UnregisterAsset(string assetName, object asset) {
            assetName = assetName.Replace('/', '\\');

            if (this.ModContent.ContainsKey(assetName) && this.ModContent[assetName].Remove(asset)) {
                ModEntry.INSTANCE.Monitor.Log($"[{this.Name}] Unregistered {assetName} ({asset.GetType().ToString()})", LogLevel.Trace);
                RefreshAsset(assetName);
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
         * <param name="size">The required size of the asset.</param>
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

        /**
         * <summary>Sets the minimum required size for the texture. Call this before the texture is loaded, during mod entry.</summary>
         * <param name="assetName">The name of the asset</param>
         * * <param name="height">The requested width</param>
         * <param name="height">The requested height</param>
         * <returns>The required texture width, or null if it shouldn't change.</returns>
         * <remarks>If the original asset is larger than the size that was set, that size will be used. Mod textures will be clipped if needed.</remarks>
         **/
        public void RequestTextureSize(string assetName, int width, int height) {
            RequestTextureSize(assetName, new Size(width, height));
        }
    }
}
