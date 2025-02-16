﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarlightRiver.Content.NPCs.Hell;
using StarlightRiver.Content.Tiles.Overgrow;
using StarlightRiver.Core;
using StarlightRiver.Keys;
using System;
using System.Linq;
using Terraria;
using static Terraria.ModLoader.ModContent;

namespace StarlightRiver.Content.CustomHooks
{
	class DrawLayers : HookGroup
    {
        //A few different hooks for drawing on certain layers. Orig is always run and its just draw calls.
        public override SafetyLevel Safety => SafetyLevel.Safe;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            //Under water
            On.Terraria.Main.drawWaters += DrawUnderwaterNPCs;
            //Keys
            On.Terraria.Main.DrawItems += DrawKeys;
            //Tile draws infront of the player
            On.Terraria.Main.DrawPlayer += PostDrawPlayer;
        }

        private void DrawUnderwaterNPCs(On.Terraria.Main.orig_drawWaters orig, Main self, bool bg, int styleOverride, bool allowUpdate) //TODO: Generalize this for later use
        {
            orig(self, bg, styleOverride, allowUpdate);

            foreach (NPC npc in Main.npc.Where(npc => npc.type == NPCType<BoneMine>() && npc.active))
            {
                SpriteBatch spriteBatch = Main.spriteBatch;
                Color drawColor = Lighting.GetColor((int)npc.position.X / 16, (int)npc.position.Y / 16) * 0.3f;

                spriteBatch.Draw(GetTexture(npc.modNPC.Texture), npc.position - Main.screenPosition + Vector2.One * 16 * 12 + new Vector2((float)Math.Sin(npc.ai[0]) * 4f, 0), drawColor);
                for (int k = 0; k >= 0; k++)
                {
                    if (Main.tile[(int)npc.position.X / 16, (int)npc.position.Y / 16 + k + 2].active()) break;
                    spriteBatch.Draw(GetTexture(AssetDirectory.OvergrowItem + "ShakerChain"),
                        npc.Center - Main.screenPosition + Vector2.One * 16 * 12 + new Vector2(-4 + (float)Math.Sin(npc.ai[0] + k) * 4, 18 + k * 16), drawColor);
                }
            }
        }

        private void PostDrawPlayer(On.Terraria.Main.orig_DrawPlayer orig, Main self, Player drawPlayer, Vector2 Position, float rotation, Vector2 rotationOrigin, float shadow) //TODO: Generalize this for later use, and possibly optimize it also
        {
            drawPlayer.GetModPlayer<StarlightPlayer>().PreDraw(drawPlayer, Main.spriteBatch);

            orig(self, drawPlayer, Position, rotation, rotationOrigin, shadow);

            drawPlayer.GetModPlayer<StarlightPlayer>().PostDraw(drawPlayer, Main.spriteBatch);
        }

        private void DrawKeys(On.Terraria.Main.orig_DrawItems orig, Main self)
        {
            foreach (Key key in StarlightWorld.Keys)
                key.Draw(Main.spriteBatch);

            orig(self);
        }
    }
}