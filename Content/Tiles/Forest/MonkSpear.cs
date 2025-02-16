﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarlightRiver.Core;
using StarlightRiver.Physics;
using Terraria;
using Terraria.ID;
using static Terraria.ModLoader.ModContent;

namespace StarlightRiver.Content.Tiles.Forest
{
	class MonkSpear : DummyTile
	{
        public override int DummyType => ProjectileType<MonkSpearDummy>();

		public override bool Autoload(ref string name, ref string texture)
        {
            texture = AssetDirectory.ForestTile + name;
            return base.Autoload(ref name, ref texture);
        }

        public override void SetDefaults()
        {
            TileID.Sets.DrawsWalls[Type] = true;
            QuickBlock.QuickSetFurniture(this, 4, 6, DustID.Dirt, SoundID.Dig, false, new Color(100, 80, 40), false, false, "Monk's Spade");

            Main.tileMerge[TileID.Grass][Type] = true;
            Main.tileMerge[Type][TileID.Grass] = true;

            Main.tileMerge[TileID.Dirt][Type] = true;
            Main.tileMerge[Type][TileID.Dirt] = true;
        }

		public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
		{
            var tile = Framing.GetTileSafely(i, j);

            if (tile.frameY >= 6 * 16)
                WorldGen.PlaceTile(i, j, TileID.Grass);
        }

		public override void KillMultiTile(int i, int j, int frameX, int frameY)
		{
            Item.NewItem(new Vector2(i, j) * 16, ItemType<Items.Misc.MonkSpade>());
		}
	}

    class MonkSpearDummy : Dummy
	{
        public MonkSpearDummy() : base(TileType<MonkSpear>(), 4 * 16, 6 * 16) { }

        private VerletChainInstance ChainShort;
        private VerletChainInstance ChainLong;

        public override void SafeSetDefaults()
        {
            ChainLong = new VerletChainInstance(8, false, projectile.Center + new Vector2(14, -26), 8)
            {
                constraintRepetitions = 2,//defaults to 2, raising this lowers stretching at the cost of performance
                drag = 2f,//This number defaults to 1, Is very sensitive
                forceGravity = new Vector2(0f, 0.25f),//gravity x/y
                scale = 0.4f
            };

            ChainShort = new VerletChainInstance(6, false, projectile.Center + new Vector2(14, -26), 8)
            {
                constraintRepetitions = 2,//defaults to 2, raising this lowers stretching at the cost of performance
                drag = 2f,//This number defaults to 1, Is very sensitive
                forceGravity = new Vector2(0f, 0.22f),//gravity x/y
                scale = 0.5f
            };
        }

        public override void Update()
        {
            ChainLong.UpdateChain(projectile.Center + new Vector2(14, -16));
            ChainShort.UpdateChain(projectile.Center + new Vector2(14, -16));

            ChainLong.IterateRope(WindForceLong);
            ChainShort.IterateRope(WindForceShort);

            projectile.ai[0] += 0.005f;
        }

		public override void PostDraw(SpriteBatch spriteBatch, Color lightColor)
		{
            var tex = GetTexture(AssetDirectory.ForestTile + "MonkSpearOver");

            for (int k = 0; k < 3; k++)
            {
                int x = (int)((projectile.position.X + 8) / 16) + k;
                int y = (int)((projectile.position.Y + 6 * 16 + 8) / 16);
                spriteBatch.Draw(tex, projectile.position + new Vector2(k * 16, 6 * 16) - Main.screenPosition, new Rectangle(k * 16, 0, 16, 16), Lighting.GetColor(x, y));
            }
		}

		private void WindForceShort(int index)//wind
        {
            int offset = (int)(projectile.position.X / 16 + projectile.position.Y / 16);

            float sin = (float)System.Math.Sin(StarlightWorld.rottime + offset - index / 3f);

            float cos = (float)System.Math.Cos(projectile.ai[0]);
            float sin2 = (float)System.Math.Sin(StarlightWorld.rottime + offset + cos);

            Vector2 posShort = new Vector2(ChainShort.ropeSegments[index].posNow.X + 1 + sin2 * 0.6f, ChainShort.ropeSegments[index].posNow.Y + sin * 0.8f);
            Color colorShort = new Color(60, 90, 170).MultiplyRGB(Color.White * (1 - sin * 0.2f)).MultiplyRGB(Lighting.GetColor((int)posShort.X / 16, (int)posShort.Y / 16));
            ChainShort.ropeSegments[index].posNow = posShort;
            ChainShort.ropeSegments[index].color = colorShort;
        }

        private void WindForceLong(int index)
		{
            int offset = (int)(projectile.position.X / 16 + projectile.position.Y / 16);

            float sin = (float)System.Math.Sin(StarlightWorld.rottime + offset - index / 3f);

            float cos = (float)System.Math.Cos(projectile.ai[0]);
            float sin2 = (float)System.Math.Sin(StarlightWorld.rottime + offset + cos);

            Vector2 posLong = new Vector2(ChainLong.ropeSegments[index].posNow.X + 1 + sin2 * 0.6f, ChainLong.ropeSegments[index].posNow.Y + sin * 0.8f);
            Color colorLong = new Color(40, 60, 150).MultiplyRGB(Color.White * (1 - sin * 0.2f)).MultiplyRGB(Lighting.GetColor((int)posLong.X / 16, (int)posLong.Y / 16));
            ChainLong.ropeSegments[index].posNow = posLong;
            ChainLong.ropeSegments[index].color = colorLong;
        }

        public override void Kill(int timeLeft)
        {
            VerletChainInstance.toDraw.Remove(ChainShort);
            VerletChainInstance.toDraw.Remove(ChainLong);
        }
    }
}
