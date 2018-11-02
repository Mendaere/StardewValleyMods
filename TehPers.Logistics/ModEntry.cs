﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using TehPers.CoreMod.Api;
using TehPers.CoreMod.Api.Drawing;
using TehPers.CoreMod.Api.Items;
using TehPers.Logistics.Items;
using SObject = StardewValley.Object;

namespace TehPers.Logistics {
    public class ModEntry : Mod {
        public override void Entry(IModHelper helper) {

            // Register an event for the first update tick to handle all core API calls
            GameEvents.FirstUpdateTick += (sender, e) => {
                if (helper.ModRegistry.GetApi<Func<IMod, ICoreApi>>("TehPers.CoreMod") is Func<IMod, ICoreApi> coreApiFactory) {
                    // Create core API
                    ICoreApi coreApi = coreApiFactory(this);

                    // Register custom machines
                    this.RegisterMachines(coreApi);
                }
            };

            // TODO: debug
            ControlEvents.KeyPressed += (sender, pressed) => {
                if (pressed.KeyPressed == Keys.NumPad3) {
                    SObject machine = new SObject(Vector2.Zero, 1950, false);
                    Game1.player.addItemToInventory(machine);
                }
            };
        }

        private void RegisterMachines(ICoreApi coreApi) {
            this.Monitor.Log("Registering machines...", LogLevel.Info);
            IItemApi itemApi = coreApi.Items;

            // Stone converter
            TextureInformation textureInfo = new TextureInformation(coreApi.Drawing.WhitePixel, null, Color.Blue);
            StoneConverterMachine stoneConverter = new StoneConverterMachine(this, "stoneConverter", textureInfo);
            itemApi.Register("stoneConverter", stoneConverter);

            this.Monitor.Log("Done");
        }
    }

    internal static class MachineDelegator {
        private static bool _patched = false;
    }
}
