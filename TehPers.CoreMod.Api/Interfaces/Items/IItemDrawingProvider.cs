﻿using TehPers.CoreMod.Api.Drawing.Sprites;

namespace TehPers.CoreMod.Api.Items {
    public interface IItemDrawingProvider {
        /// <summary>Tries to get the sprite for a particular item.</summary>
        /// <param name="key">The item's key.</param>
        /// <param name="sprite">The sprite associated with the item.</param>
        /// <returns>True if the sprite was retrieved, false if this provider cannot provide a sprite for the given key.</returns>
        bool TryGetSprite(in ItemKey key, out ISprite sprite);
    }
}