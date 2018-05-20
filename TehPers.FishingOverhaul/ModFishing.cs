﻿using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network;
using StardewValley.Tools;
using TehPers.Core;
using TehPers.Core.Api.Weighted;
using TehPers.Core.Helpers.Static;
using TehPers.FishingOverhaul.Configs;
using TehPers.FishingOverhaul.Patches;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TehPers.FishingOverhaul {
    public class ModFishing : Mod {
        public static ModFishing Instance { get; private set; }

        public FishingApi Api { get; private set; }
        public ConfigMain MainConfig { get; private set; }
        public ConfigFish FishConfig { get; private set; }
        public ConfigFishTraits FishTraitsConfig { get; private set; }
        public ConfigTreasure TreasureConfig { get; private set; }

        internal FishingRodOverrider Overrider { get; set; }
        internal HarmonyInstance Harmony { get; private set; }
        internal TehCoreApi CoreApi { get; }

        public ModFishing() {
            this.CoreApi = TehCoreApi.Create(this);
        }

        public override void Entry(IModHelper helper) {
            ModFishing.Instance = this;
            this.Api = new FishingApi();

            // Make sure TehPers.Core isn't loaded as it's not needed anymore
            if (helper.ModRegistry.IsLoaded("TehPers.Core"))
                this.Monitor.Log("Delete TehCore, it's not needed anymore. Your game will probably crash with it installed anyway.", LogLevel.Error);

            // Load the configs
            this.LoadConfigs();

            // Make sure this mod is enabled
            if (!this.MainConfig.ModEnabled)
                return;

            // Apply patches
            this.Harmony = HarmonyInstance.Create(this.ModManifest.UniqueID);
            Type targetType = AssortedHelpers.GetSDVType(nameof(NetAudio));
            this.Harmony.Patch(targetType.GetMethod(nameof(NetAudio.PlayLocal)), new HarmonyMethod(typeof(NetAudioPatches).GetMethod(nameof(NetAudioPatches.Prefix))), null);

            this.Overrider = new FishingRodOverrider();
            GraphicsEvents.OnPostRenderHudEvent += this.PostRenderHud;

            /*ControlEvents.KeyPressed += (sender, pressed) => {
                if (pressed.KeyPressed == Keys.NumPad7) {
                    Menu menu = new Menu(Game1.viewport.Width / 6, Game1.viewport.Height / 6, 2 * Game1.viewport.Width / 3, 2 * Game1.viewport.Height / 3);

                    menu.MainElement.AddChild(new TextElement {
                        Text = "Test Menu",
                        Color = Color.Black,
                        Size = new BoxVector(0, 50, 1F, 0F),
                        Scale = new Vector2(3, 3),
                        HorizontalAlignment = Alignment.MIDDLE,
                        VerticalAlignment = Alignment.TOP
                    });

                    menu.MainElement.AddChild(new TextboxElement {
                        Location = new BoxVector(0, 100, 0, 0)
                    });

                    Game1.activeClickableMenu = menu;
                }
            };*/
        }

        public override object GetApi() {
            return this.Api;
        }

        private void LoadConfigs() {
            // Load configs
            this.MainConfig = this.CoreApi.JsonHelper.ReadOrCreate<ConfigMain>("config.json", this.Helper);
            this.TreasureConfig = this.CoreApi.JsonHelper.ReadOrCreate<ConfigTreasure>("treasure.json", this.Helper, this.MainConfig.MinifyConfigs);
            this.FishConfig = this.CoreApi.JsonHelper.ReadOrCreate("fish.json", this.Helper, () => {
                // Populate fish data
                ConfigFish config = new ConfigFish();
                config.PopulateData();
                return config;
            }, this.MainConfig.MinifyConfigs);
            this.FishTraitsConfig = this.CoreApi.JsonHelper.ReadOrCreate("fishTraits.json", this.Helper, () => {
                // Populate fish traits data
                ConfigFishTraits config = new ConfigFishTraits();
                config.PopulateData();
                return config;
            }, this.MainConfig.MinifyConfigs);

            // Not a config, but whatever
            this.Api.AddTrashData(new DefaultTrashData());
            this.Api.AddTrashData(new SpecificTrashData(new[] { 797 }, 0.01D, "Submarine")); // Pearl
            this.Api.AddTrashData(new SpecificTrashData(new[] { 152 }, 0.99D, "Submarine")); // Seaweed
        }

        #region Events
        private void PostRenderHud(object sender, EventArgs eventArgs) {
            if (!this.MainConfig.ShowFishingData || Game1.eventUp || !(Game1.player.CurrentTool is FishingRod rod))
                return;

            Color textColor = Color.White;
            SpriteFont font = Game1.smallFont;

            // Draw the fishing GUI to the screen
            float boxWidth = 0;
            float lineHeight = font.LineSpacing;
            float boxHeight = 0;

            // Setup the sprite batch
            SpriteBatch batch = Game1.spriteBatch;
            batch.End();
            batch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);

            // Draw streak
            string streakText = ModFishing.Translate("text.streak", this.Api.GetStreak(Game1.player));
            batch.DrawStringWithShadow(font, streakText, Vector2.Zero, textColor, 1f);
            boxWidth = Math.Max(boxWidth, font.MeasureString(streakText).X);
            boxHeight += lineHeight;

            // Get info on all the possible fish
            IWeightedElement<int?>[] possibleFish = this.Api.GetPossibleFish(Game1.player).ToArray();
            double totalWeight = possibleFish.SumWeights(); // Should always be 1
            possibleFish = possibleFish.Where(e => e.Value != null).ToArray();
            double fishChance = possibleFish.SumWeights() / totalWeight;

            // Draw treasure chance
            string treasureText = ModFishing.Translate("text.treasure", ModFishing.Translate("text.percent", this.Api.GetTreasureChance(Game1.player, rod)));
            batch.DrawStringWithShadow(font, treasureText, new Vector2(0, boxHeight), textColor, 1f);
            boxWidth = Math.Max(boxWidth, font.MeasureString(treasureText).X);
            boxHeight += lineHeight;

            // Draw trash chance
            string trashText = ModFishing.Translate("text.trash", ModFishing.Translate("text.percent", 1f - fishChance));
            batch.DrawStringWithShadow(font, trashText, new Vector2(0, boxHeight), textColor, 1f);
            boxWidth = Math.Max(boxWidth, font.MeasureString(trashText).X);
            boxHeight += lineHeight;

            if (possibleFish.Any()) {
                // Calculate total weigh of each possible fish (for percentages)
                totalWeight = possibleFish.SumWeights();

                // Draw info for each fish
                const float iconScale = Game1.pixelZoom / 2f;
                foreach (IWeightedElement<int?> fishData in possibleFish) {
                    // Skip trash
                    if (fishData.Value == null)
                        continue;

                    // Get fish ID
                    int fish = fishData.Value.Value;

                    // Don't draw hidden fish
                    if (this.Api.IsHidden(fish))
                        continue;

                    // Draw fish icon
                    Rectangle source = GameLocation.getSourceRectForObject(fish);
                    batch.Draw(Game1.objectSpriteSheet, new Vector2(0, boxHeight), source, Color.White, 0.0f, Vector2.Zero, iconScale, SpriteEffects.None, 1F);
                    lineHeight = Math.Max(lineHeight, source.Height * iconScale);

                    // Draw fish information
                    string chanceText = ModFishing.Translate("text.percent", fishChance * fishData.GetWeight() / totalWeight);
                    string fishText = $"{this.Api.GetFishName(fish)} - {chanceText}";
                    batch.DrawStringWithShadow(font, fishText, new Vector2(source.Width * iconScale, boxHeight), textColor, 1F);
                    boxWidth = Math.Max(boxWidth, font.MeasureString(fishText).X + source.Width * iconScale);

                    // Update destY
                    boxHeight += lineHeight;
                }
            }

            // Draw the background rectangle
            batch.Draw(DrawHelpers.WhitePixel, new Rectangle(0, 0, (int) boxWidth, (int) boxHeight), null, new Color(0, 0, 0, 0.25F), 0f, Vector2.Zero, SpriteEffects.None, 0.85F);

            // Debug info
            StringBuilder text = new StringBuilder();
            if (text.Length > 0) {
                batch.DrawStringWithShadow(Game1.smallFont, text.ToString(), new Vector2(0, boxHeight), Color.White, 0.8F);
            }

            batch.End();
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
        }
        #endregion

        #region Static Helpers
        public static string Translate(string key, params object[] formatArgs) {
            return string.Format(ModFishing.Instance.Helper.Translation.Get(key), formatArgs);
        }
        #endregion
    }
}