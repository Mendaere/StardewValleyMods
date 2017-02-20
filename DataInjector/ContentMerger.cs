﻿using Entoarox.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using TehPers.Stardew.Framework;
using TehPers.Stardew.SCCL.API;
using xTile.Dimensions;

namespace TehPers.Stardew.SCCL {
    public class ContentMerger {
        internal Dictionary<string, object> Cache { get; } = new Dictionary<string, object>();
        private static HashSet<Type> Unmergables { get; } = new HashSet<Type>();

        public T Inject<T>(LoadBase<T> loader, string assetName) {
            ModConfig config = ModEntry.INSTANCE.config;

            T asset = loader(assetName);
            try {
                if (false || Cache.ContainsKey(assetName))
                    return (T) Cache[assetName];
                else {
                    if (this.getModAssets<T>(assetName).Count > 0) {
                        Type t = typeof(T);
                        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                            asset = (T) this.GetType().GetMethod("MergeDictionary", BindingFlags.Public | BindingFlags.Instance)
                                .MakeGenericMethod(t.GetGenericArguments())
                                .Invoke(this, new object[] { asset, assetName });
                            //asset = (T) (object) this.MergeMods(asset as Dictionary<int, string>, assetName);
                        } else if (t == typeof(Texture2D) && config.OverwriteAllTextures) {
                            Texture2D texture = asset as Texture2D;
                            if (texture == null || texture.Format == SurfaceFormat.Color)
                                asset = (T) (object) this.MergeTextures<Color>(texture, assetName);
                            else {
                                ModEntry.INSTANCE.Monitor.Log("Cannot merge this texture format, overriding instead: " + Enum.GetName(typeof(SurfaceFormat), texture.Format), LogLevel.Info);
                                asset = this.ReplaceIfExists(asset, assetName);
                            }
                        } else {
                            if (!Unmergables.Contains(typeof(T))) {
                                ModEntry.INSTANCE.Monitor.Log("Cannot merge this type, overriding instead: " + typeof(T).ToString(), LogLevel.Trace);
                                Unmergables.Add(typeof(T));
                            }
                            asset = this.ReplaceIfExists(asset, assetName);
                        }
                    }

                    Cache[assetName] = asset;
                }
            } catch (Exception ex) {
                ModEntry.INSTANCE.Monitor.Log(LogLevel.Error, "Derp");
            }
            return asset;
        }

        public List<KeyValuePair<string, T>> getModAssets<T>(string assetName) {
            ModConfig config = ModEntry.INSTANCE.config;
            config.LoadOrder.AddRange(
                from modKV in ContentAPI.mods
                let mod = modKV.Key
                where !config.LoadOrder.Contains(mod)
                select mod
            );
            ModEntry.INSTANCE.Helper.WriteConfig(config);

            return (
                from mod in ContentAPI.mods
                orderby config.LoadOrder.IndexOf(mod.Key)
                let injector = mod.Value
                where injector.ModContent.ContainsKey(assetName)
                from asset in injector.ModContent[assetName]
                where asset is T
                select new KeyValuePair<string, T>(injector.Name, (T) asset)
                ).ToList();
        }

        public Dictionary<TKey, TVal> MergeDictionary<TKey, TVal>(Dictionary<TKey, TVal> orig, string assetName) {
            Dictionary<TKey, TVal> diffs = new Dictionary<TKey, TVal>();
            Dictionary<TKey, string> diffMods = new Dictionary<TKey, string>();
            List<KeyValuePair<string, Dictionary<TKey, TVal>>> mods = getModAssets<Dictionary<TKey, TVal>>(assetName);

            bool collision = false;

            foreach (KeyValuePair<string, Dictionary<TKey, TVal>> modKV in mods) {
                foreach (KeyValuePair<TKey, TVal> injection in modKV.Value) {
                    if (!(orig.ContainsKey(injection.Key) && orig[injection.Key].Equals(injection.Value))) {
                        if (!collision && diffs.ContainsKey(injection.Key)) {
                            ModEntry.INSTANCE.Monitor.Log(string.Format("Collision detected between {0} and {1}! Overwriting...", diffMods[injection.Key], modKV.Key), LogLevel.Warn);
                            collision = true;
                        }
                        diffs[injection.Key] = injection.Value;
                        diffMods[injection.Key] = modKV.Key;
                    }
                }
            }

            foreach (KeyValuePair<TKey, TVal> diff in diffs)
                orig[diff.Key] = diff.Value;

            if (diffs.Count > 0)
                ModEntry.INSTANCE.Monitor.Log(string.Format("{0} injected {1} changes into {2}.xnb", string.Join(", ", diffMods.Values), diffs.Count, assetName), LogLevel.Info);

            return orig;
        }

        public Texture2D MergeTextures<TFormat>(Texture2D orig, string assetName) where TFormat : struct {
            Dictionary<int, string> diffMods = new Dictionary<int, string>();
            TFormat[] origData = new TFormat[orig.Width * orig.Height];
            Size origSize = new Size(orig.Width, orig.Height);
            orig.GetData(origData);
            List<TFormat> diffData = new List<TFormat>(origData);

            List<KeyValuePair<string, Texture2D>> mods = getModAssets<Texture2D>(assetName);

            foreach (KeyValuePair<string, Texture2D> modKV in mods) {
                string mod = modKV.Key;
                Texture2D modTexture = modKV.Value;
                bool collision = false;
                TFormat[] modData = new TFormat[modTexture.Width * modTexture.Height];
                Size modSize = new Size(modTexture.Width, modTexture.Height);
                modTexture.GetData(modData);

                if (modSize.Width != origSize.Width)
                    ModEntry.INSTANCE.Monitor.Log("Mod's texture is too wide for the game, so it's being trimmed: " + mod, LogLevel.Warn);

                for (int y = 0; y < modSize.Height; y++) { // Use mod's height
                    for (int x = 0; x < origSize.Width; x++) { // Use original's width
                        int i = y * origSize.Width + x; // Use the original's index because we're keeping that width for the final
                        TFormat pixel;
                        if (modSize.Width < origSize.Width) // If the mod's texture does not contain this pixel coordinate
                            pixel = origData[i];
                        else // Otherwise if it contains this pixel
                            pixel = modData[y * modSize.Width + x];

                        if (i >= origData.Length || !origData[i].Equals(pixel)) {
                            if (!collision && diffMods.ContainsKey(i)) {
                                ModEntry.INSTANCE.Monitor.Log(string.Format("Collision detected between {0} and {1}! Overwriting...", mod, diffMods[i]), LogLevel.Warn);
                                collision = true;
                            }

                            while (i >= diffData.Count) diffData.Add(default(TFormat));

                            diffData[i] = pixel;
                            diffMods[i] = mod;
                        }
                    }
                }
            }

            ModEntry.INSTANCE.Monitor.Log(string.Format("{0} injected changes into {1}.xnb", string.Join(", ", diffMods.Values), assetName), LogLevel.Info);

            Texture2D result = new Texture2D(orig.GraphicsDevice, origSize.Width, (int) Math.Ceiling((double) diffData.Count / origSize.Width));
            result.SetData(diffData.ToArray());
            orig.Dispose();
            return result;
        }

        public T ReplaceIfExists<T>(T orig, string assetName) {
            T diff = orig;
            string diffMod = null;

            List<KeyValuePair<string, T>> mods = getModAssets<T>(assetName);

            foreach (KeyValuePair<string, T> modKV in mods) {
                if (diffMod != null)
                    ModEntry.INSTANCE.Monitor.Log("Collision detected with " + diffMod + "! Overwriting...", LogLevel.Warn);

                diff = modKV.Value;
                diffMod = modKV.Key;
            }

            if (diffMod != null)
                ModEntry.INSTANCE.Monitor.Log(string.Format("{0} replaced {1}.xnb", diffMod, assetName), LogLevel.Info);

            return diff;
        }
    }
}
