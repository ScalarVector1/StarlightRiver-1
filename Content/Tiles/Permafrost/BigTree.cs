﻿using Microsoft.Xna.Framework;
using StarlightRiver.Core;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace StarlightRiver.Content.Tiles.Permafrost
{
	class BigTree : ModTile
    {
        public override bool Autoload(ref string name, ref string texture)
        {
            texture = "StarlightRiver/Assets/Tiles/Permafrost/BigTree";
            return true;
        }

        public override void SetDefaults() => QuickBlock.QuickSetFurniture(this, 16, 17, DustType<Dusts.Stone>(), SoundID.Tink, false, new Color(100, 200, 200));
    }
}
