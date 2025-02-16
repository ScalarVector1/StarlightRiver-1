﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarlightRiver.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace StarlightRiver.Content.Bosses.VitricBoss
{
	internal class ArenaBottom : ModNPC, IDrawAdditive
    {
        public VitricBoss Parent;
        public override string Texture => AssetDirectory.VitricBoss + "CrystalWaveHot";

        public override bool? CanBeHitByProjectile(Projectile projectile) => false;

        public override bool? CanBeHitByItem(Player player, Item item) => false;

        public override bool CheckActive() => false;

        public override void SetStaticDefaults() => DisplayName.SetDefault("");

        public override void SetDefaults()
        {
            npc.height = 16;
            npc.width = 1260;
            npc.aiStyle = -1;
            npc.lifeMax = 2;
            npc.knockBackResist = 0f;
            npc.lavaImmune = true;
            npc.noGravity = false;
            npc.noTileCollide = false;
            npc.dontTakeDamage = true;
            npc.dontCountMe = true;
            npc.hide = true;
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCsBehindNonSolidTiles.Add(index);
        }

        public override void AI()
        {
            /* AI fields:
             * 0: timer
             * 1: state
             * 2: mirrored?
             */
            if (Parent?.npc.active != true) 
            { 
                npc.active = false; 
                return; 
            }

            if (Parent.Phase == (int)VitricBoss.AIStates.FirstPhase && Parent.AttackPhase == 0 && npc.ai[1] != 2)
            {
                npc.ai[1] = 2;
                npc.ai[0] = 0;
                npc.ai[3]++;//the 4th ai slot is used here as a random seed
            }

            switch (npc.ai[1])
            {
                case 0:
                    if (Main.player.Any(n => n.Hitbox.Intersects(npc.Hitbox)))
                        npc.ai[0]++; //ticks the enrage timer when players are standing on the ground

                    if (npc.ai[0] > 120) //after standing there for too long a wave comes by
                    {
                        npc.ai[1] = 1; //wave mode
                        npc.ai[0] = 0; //reset timer so it can be reused

                        npc.TargetClosest();
                        if (Main.player[npc.target].Center.X > npc.Center.X) npc.ai[2] = 0;
                        else npc.ai[2] = 1;
                    }
                    break;

                case 1:

                    if (Parent.Phase > (int)VitricBoss.AIStates.SecondPhase)
                        return;

                    npc.ai[0] += 8; //timer is now used to track where we are in the crystal wave
                    if (npc.ai[0] % 32 == 0) //summons a crystal at every tile covered by the NPC
                    {
                        Vector2 pos = new Vector2(npc.ai[2] == 1 ? npc.position.X + npc.width - npc.ai[0] : npc.position.X + npc.ai[0], npc.position.Y + 48);
                        Projectile.NewProjectile(pos, Vector2.Zero, ProjectileType<CrystalWave>(), 20, 1);
                    }
                    if (npc.ai[0] > npc.width)
                    {
                        npc.ai[1] = 0; //go back to waiting for enrage time
                        npc.ai[0] = 0; //reset timer
                    }
                    break;

                case 2: //during every crystal phase

                    if (Parent.Phase == (int)VitricBoss.AIStates.FirstPhase && Parent.AttackPhase == 0)
                        npc.ai[0]++;
                    else if (Parent.Phase == (int)VitricBoss.AIStates.FirstPhase || Parent.Phase == (int)VitricBoss.AIStates.Dying)
                    {
                        if (npc.ai[0] > 150)
                            npc.ai[0] = 150;

                        npc.ai[0]--;

                        if (npc.ai[0] <= 0)
                            npc.ai[1] = 0;
                    }

                    if (npc.ai[0] < 120) //dust before rising
                        Dust.NewDust(npc.position, npc.width, npc.height, Terraria.ID.DustID.Fire);

                    if (npc.ai[0] >= 150)
                        foreach (Player target in Main.player.Where(n => n.active))
                        {
                            Rectangle rect = new Rectangle((int)npc.position.X, (int)npc.position.Y - 840, npc.width, npc.height);

                            if (target.Hitbox.Intersects(rect))
                            {
                                target.Hurt(PlayerDeathReason.ByCustomReason(target.name + " was impaled..."), Main.expertMode ? 80 : 40, 0);
                                target.GetModPlayer<StarlightPlayer>().platformTimer = 15;
                                target.velocity.Y = Main.rand.Next(9, 13);
                            }

                            if (target.Hitbox.Intersects(npc.Hitbox))
                            {
                                target.Hurt(PlayerDeathReason.ByCustomReason(target.name + " was impaled..."), Main.expertMode ? 80 : 40, 0);
                                target.GetModPlayer<StarlightPlayer>().platformTimer = 15;
                                target.velocity.Y = -Main.rand.Next(9, 13);

                                target.rocketTime = target.rocketTimeMax;
                                target.wingTime = target.wingTimeMax;
                                target.jumpAgainCloud = true;
                                target.jumpAgainBlizzard = true;
                                target.jumpAgainSandstorm = true;
                                target.jumpAgainFart = true;
                                target.jumpAgainSail = true;
                            }
                        }
                    break;
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            Rectangle rect = new Rectangle((int)npc.position.X, (int)npc.position.Y - 820, npc.width, npc.height);

            if (target.Hitbox.Intersects(rect) || target.Hitbox.Intersects(npc.Hitbox)) //duplicate?? leaving in for now since unsure
                target.Hurt(PlayerDeathReason.ByCustomReason(target.name + " was impaled..."), Main.expertMode ? 80 : 40, 0);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor) => false;

        public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            if (npc.ai[1] == 2 && npc.ai[0] > 120) //in the second phase after the crystals have risen
            {
                Random rand = new Random((int)npc.ai[3] + npc.whoAmI);//the seed changes each time this attack activates, whoAmI is added so the top/bottom are different

                int eggIndex = rand.Next(50) == 0 ? rand.Next(0, npc.width / 18) * 18 : -1;// 1/50 chance

                float off = Math.Min((npc.ai[0] - 120) / 30f * 32, 32);
                Texture2D tex = Main.npcTexture[npc.type];
                for (int k = 0; k < npc.width; k += 18)
                {
                    Vector2 pos = npc.position + new Vector2(k, 36 - off + (float)Math.Sin(Main.GameUpdateCount * 0.05f + rand.Next(100) * 0.2f) * 6) - Main.screenPosition; //actually draw the crystals
                    Vector2 pos2 = npc.position + new Vector2(k, -940 + 32 + off - (float)Math.Sin(Main.GameUpdateCount * 0.05f + rand.Next(100) * 0.2f) * 6) - Main.screenPosition; 
                    
                    if (eggIndex == k)//ugly but I this way its only checked once
                    {
                        Texture2D eggTex = GetTexture(AssetDirectory.VitricBoss + "MagMegg");
                        spriteBatch.Draw(eggTex, pos, null, Color.White, 0.1f * ((float)rand.NextDouble() - 0.5f), default, 1, default, default);
                        spriteBatch.Draw(eggTex, pos2, null, Color.White, 0.1f * ((float)rand.NextDouble() - 0.5f), default, 1, default, default);
                    }
                    else
                    {
                        spriteBatch.Draw(tex, pos, null, Color.White, 0.1f * ((float)rand.NextDouble() - 0.5f), default, 1, default, default);
                        spriteBatch.Draw(tex, pos2, null, Color.White, 0.1f * ((float)rand.NextDouble() - 0.5f), default, 1, default, default);
                    }
                }
            }
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            var tex = GetTexture(AssetDirectory.VitricBoss + "LongGlow");

            if (npc.ai[1] == 2) 
            {
                Color color;

                if (npc.ai[0] < 360)
                    color = Color.Lerp(Color.Transparent, Color.Red, npc.ai[0] / 360f);

                else if (npc.ai[0] < 390)
                    color = Color.Lerp(Color.Red, Color.Red * 0.6f, (npc.ai[0] - 360f) / 30f);

                else
                {
                    color = Color.Red * (0.5f + ((float)Math.Sin(npc.ai[0] / 20f) + 1) * 0.1f);
                    color.G += (byte)((Math.Sin(npc.ai[0] / 50f) + 1) * 25);
                }

                spriteBatch.Draw(tex, new Rectangle(npc.Hitbox.X - (int)Main.screenPosition.X, npc.Hitbox.Y - 66 - (int)Main.screenPosition.Y, npc.Hitbox.Width, 100), null, color, 0, default, default, default);
                spriteBatch.Draw(tex, new Rectangle(npc.Hitbox.X - (int)Main.screenPosition.X, npc.Hitbox.Y - 848 - (int)Main.screenPosition.Y, npc.Hitbox.Width, 100), null, color, 0, default, SpriteEffects.FlipVertically, default);
            }
        }
    }

    internal class CrystalWave : ModProjectile
    {
        private float startY;

        public override string Texture => AssetDirectory.VitricBoss + Name;

        public override void SetDefaults()
        {
            projectile.hostile = true;
            projectile.width = 16;
            projectile.height = 48;
            projectile.timeLeft = 30;
            projectile.hide = true;
        }

        public override void AI()
        {
            float off = 128 * projectile.timeLeft / 15 - 64 * (float)Math.Pow(projectile.timeLeft, 2) / 225;
            if (projectile.timeLeft == 30)
            {
                Main.PlaySound(Terraria.ID.SoundID.DD2_WitherBeastCrystalImpact, projectile.Center);

                for (int k = 0; k < Main.rand.Next(6); k++)
                {
                    var type = ModGore.GetGoreSlot("StarlightRiver/Assets/NPCs/Vitric/MagmiteGore");
                    Gore.NewGoreDirect(projectile.Center - Vector2.UnitY * 16, (Vector2.UnitY * -8).RotatedByRandom(0.2f), type, Main.rand.NextFloat(0.3f, 0.5f));
                    Dust.NewDustPerfect(projectile.Center - Vector2.UnitY * 16, DustType<Dusts.Glow>(), (Vector2.UnitY * Main.rand.Next(-5, -2)).RotatedByRandom(0.8f), 0, new Color(255, 200, 100), 0.3f);
                }

                startY = projectile.position.Y;
            }
            projectile.position.Y = startY - off;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            drawCacheProjsBehindNPCsAndTiles.Add(index);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.Draw(GetTexture(Texture), projectile.position - Main.screenPosition, Lighting.GetColor((int)projectile.Center.X / 16, (int)projectile.Center.Y / 16 - 2) * 1.4f );

            var color = Color.White * (projectile.timeLeft / 30f);
            spriteBatch.Draw(GetTexture(Texture + "Hot"), projectile.position - Main.screenPosition, color);
        }
    }
}