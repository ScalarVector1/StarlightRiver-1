﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarlightRiver.Core;
using StarlightRiver.Helpers;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace StarlightRiver.Content.Bosses.VitricBoss
{
	public class VitricBackdropLeft : ModNPC, IMoonlordLayerDrawable
    {
        public const int Scrolltime = 1000;
        public const int Risetime = 360;
        public List<NPC> platforms = new List<NPC>();

        protected ref float Timer => ref npc.ai[0];
        protected ref float State => ref npc.ai[1];
        protected ref float ScrollTimer => ref npc.ai[2];
        protected ref float ScrollDelay => ref npc.ai[3];

        public int shake = 0;

        public override string Texture => AssetDirectory.Invisible;

        public override bool CheckActive() => false;

        public override bool? CanBeHitByProjectile(Projectile projectile) => false;

        public override bool? CanBeHitByItem(Player player, Item item) => false;

        public override void SetStaticDefaults() => DisplayName.SetDefault("");

        public override void SetDefaults()
        {
            npc.height = 1;
            npc.width = 560;
            npc.aiStyle = -1;
            npc.lifeMax = 10;
            npc.knockBackResist = 0f;
            npc.lavaImmune = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.dontTakeDamage = true;
            npc.dontCountMe = true;
        }

        public override void AI()
        {
            /* AI fields:
             * 0: timer
             * 1: activation state, im too lazy to create an enum for this so: (0 = hidden, 1 = rising, 2 = still, 3 = scrolling, 4 = resetting)
             * 2: scrolling timer
             * 3: scroll acceleration
             */

            if (StarlightWorld.HasFlag(WorldFlags.VitricBossOpen) && State == 0) 
                State = 1; //when the altar is hit, make the BG rise out of the ground

            if (State == 1)
            {
                Timer++;

                SpawnPlatforms();
                ScrollDelay = 20; //initial acceleration delay

                if (Timer == Risetime - 1) //hitting the top
                {
                    Main.LocalPlayer.GetModPlayer<StarlightPlayer>().Shake += 18;
                    Helper.PlayPitched("ArenaHit", 0.6f, 0, npc.Center);
                }

                if (Timer > Risetime) 
                    State = 2;

                if (Timer % 10 == 0) 
                    Main.LocalPlayer.GetModPlayer<StarlightPlayer>().Shake += Timer < 100 ? 3 : 2;

                for (int k = 0; k < 18; k++)
                    Dust.NewDust(npc.position, 560, 1, DustType<Dusts.Sand>(), 0, Main.rand.NextFloat(-5f, -1f), Main.rand.Next(255), default, Main.rand.NextFloat(1.5f)); //spawns dust
            }

            if (State == 2)
                Timer = Risetime;

            if (State == 3) //scrolling
            {
                Timer++;

                if(Timer <= Risetime + 120) //when starting moving
                    shake = (int)(Helper.BezierEase(120 - (Timer - Risetime)) * 5); //this should work?

                if(Timer == Risetime + 120)
                {
                    for (int k = 0; k < 200; k++)
                    {
                        Dust.NewDust(npc.position, 560, 1, DustType<Dusts.Sand>(), 0, Main.rand.NextFloat(-5f, -1f), Main.rand.Next(255), default, Main.rand.NextFloat(1.5f)); //spawns dust
                        Dust.NewDust(npc.position + new Vector2(0, -55 * 16), 560, 1, DustType<Dusts.Sand>(), 0, Main.rand.NextFloat(-5f, -1f), Main.rand.Next(255), default, Main.rand.NextFloat(1.5f)); //spawns dust
                    }
                }

                if (Timer % ScrollDelay == 0)
                {
                    ScrollTimer++;

                    if (ScrollDelay > 1)
                        ScrollDelay--;
                }
            }

            if (ScrollTimer > Scrolltime)
            {
                ScrollTimer = 0;
                ResyncPlatforms();
            }

            if (State == 4)
            {
                if (ScrollTimer != 0)
                    ScrollTimer++; //stops once we're reset.
                else
                {
                    foreach (NPC npc in Main.npc.Where(n => n.modNPC is VitricBossPlatformUp))
                    {
                        npc.Center = (npc.modNPC as VitricBossPlatformUp).storedCenter;
                        npc.ai[0] = 0;
                    }

                    State = 2;
                    ScrollDelay = 20; //reset acceleration delay
                }

                if (ScrollTimer > Scrolltime) 
                    ScrollTimer = 0;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor) => false;

		public void DrawMoonlordLayer(SpriteBatch spriteBatch)
		{
			if (!npc.active)
				return;

            if (State == 3 || State == 4)
				ScrollDraw(spriteBatch);
			else  //animation for rising out of the sand
				MainDraw(spriteBatch);
        }

        public virtual void MainDraw(SpriteBatch sb)
        {
            string path = AssetDirectory.VitricBoss + Name;
            Texture2D tex = GetTexture(path);
            Texture2D tex2 = GetTexture(path + "Top");
            int targetHeight = (int)(Timer / Risetime * tex.Height);

            if (State >= 3) //ignore timer after rising is done
                targetHeight = tex.Height;

            const int yOffset = 3; // Fit perfectly in the gap

            Rectangle target = new Rectangle(
                (int)(npc.position.X - Main.screenPosition.X),
                (int)(npc.position.Y - targetHeight - Main.screenPosition.Y) - yOffset,
                tex.Width,
                targetHeight);

            Rectangle source = new Rectangle(0, 0, tex.Width, targetHeight);

            Helpers.LightingBufferRenderer.DrawWithLighting(target, tex, source, default);
            Helpers.LightingBufferRenderer.DrawWithLighting(target.TopLeft() - Vector2.UnitY * 56, tex2, tex2.Bounds, default);
        }

        public virtual void ScrollDraw(SpriteBatch sb) //im lazy
        {
            string path = AssetDirectory.VitricBoss + Name;
            Texture2D tex = GetTexture(path);
            int height1 = (int)(ScrollTimer / Scrolltime * tex.Height);
            int height2 = tex.Height - height1;
            //Color color = new Color(180, 225, 255);
            Vector2 off = Vector2.One.RotatedByRandom(6.28f) * shake;

            Rectangle target1 = new Rectangle((int)(npc.position.X - Main.screenPosition.X + off.X), (int)(npc.position.Y - height1 - Main.screenPosition.Y + off.Y), tex.Width, height1);
            Rectangle target2 = new Rectangle((int)(npc.position.X - Main.screenPosition.X + off.X), (int)(npc.position.Y - height1 - height2 - Main.screenPosition.Y + off.Y), tex.Width, height2);
            Rectangle source1 = new Rectangle(0, 0, tex.Width, height1);
            Rectangle source2 = new Rectangle(0, tex.Height - height2, tex.Width, height2);

            Helpers.LightingBufferRenderer.DrawWithLighting(target1, tex, source1, default);
            Helpers.LightingBufferRenderer.DrawWithLighting(target2, tex, source2, default);

            Texture2D tex2 = GetTexture(path + "Glow");
            sb.Draw(tex2, target1, source1, Color.White * (0.5f + (float)System.Math.Sin(StarlightWorld.rottime) * 0.1f), 0, Vector2.Zero, 0, 0);
            sb.Draw(tex2, target2, source2, Color.White * (0.5f + (float)System.Math.Sin(StarlightWorld.rottime) * 0.1f), 0, Vector2.Zero, 0, 0);
        }

        public virtual void SpawnPlatforms(bool rising = true)
        {
            PlacePlatform(205, 136, NPCType<VitricBossPlatformUp>(), rising);
            PlacePlatform(140, 420, NPCType<VitricBossPlatformUp>(), rising);
            PlacePlatform(440, 668, NPCType<VitricBossPlatformUp>(), rising);
            PlacePlatform(210, 30, NPCType<VitricBossPlatformUpSmall>(), rising);
            PlacePlatform(400, 230, NPCType<VitricBossPlatformUpSmall>(), rising);
            PlacePlatform(280, 310, NPCType<VitricBossPlatformUpSmall>(), rising);
            PlacePlatform(230, 570, NPCType<VitricBossPlatformUpSmall>(), rising);
            PlacePlatform(260, 790, NPCType<VitricBossPlatformUpSmall>(), rising);
        }

        public virtual void ResyncPlatforms()
		{
            SyncPlatform(platforms[0], 136, true);
            SyncPlatform(platforms[1], 420, true);
            SyncPlatform(platforms[2], 668, true);
            SyncPlatform(platforms[3], 30, true);
            SyncPlatform(platforms[4], 230, true);
            SyncPlatform(platforms[5], 310, true);
            SyncPlatform(platforms[6], 570, true);
            SyncPlatform(platforms[7], 790, true);
        }

        public void PlacePlatform(int x, int y, int type, bool rising)
        {
            if (rising && Timer == Risetime - (int)(y / 880f * Risetime))
            {
                var i = NPC.NewNPC((int)npc.position.X + x, (int)npc.position.Y - 2, type, 0, 0, Risetime - Timer); //When rising out of the ground, check for the appropriate time to spawn the platform based on y coord
                if (Main.npc[i].type == type)
                    (Main.npc[i].modNPC as VitricBossPlatformUp).parent = this;

                platforms.Add(Main.npc[i]);
            }
            else if (!rising)
            {
                var i = NPC.NewNPC((int)npc.position.X + x, (int)npc.position.Y - y, type, 0, 2, Risetime); //otherwise spawn it instantly AT the y coord
                if (Main.npc[i].type == type)
                    (Main.npc[i].modNPC as VitricBossPlatformUp).parent = this;

                platforms.Add(Main.npc[i]);
            }
        }

        public void SyncPlatform(NPC platform, int y, bool rising)
		{
            platform.position.Y = (int)npc.position.Y - y - platform.height;
		}
    }

    public class VitricBackdropRight : VitricBackdropLeft //im lazy
    {
        public override void MainDraw(SpriteBatch sb)
        {
            string path = AssetDirectory.VitricBoss + Name;
            Texture2D tex = GetTexture(path);
            Texture2D tex2 = GetTexture(path + "Top");
            int targetHeight = (int)(Timer / Risetime * tex.Height);

            if (State >= 3) //ignore timer after rising is done
                targetHeight = tex.Height;

            const int yOffset = 3; // Fit perfectly in the gap

            Rectangle target = new Rectangle(
                (int)(npc.position.X - Main.screenPosition.X),
                (int)(npc.position.Y - targetHeight - Main.screenPosition.Y) - yOffset,
                tex.Width,
                targetHeight);

            Rectangle source = new Rectangle(0, 0, tex.Width, targetHeight);

            Helpers.LightingBufferRenderer.DrawWithLighting(target, tex, source, default);
            Helpers.LightingBufferRenderer.DrawWithLighting(target.TopLeft() - Vector2.UnitY * 56, tex2, tex2.Bounds, default);

            //Texture2D tex3 = ModContent.GetTexture("StarlightRiver/Assets/Bosses/VitricBoss/VitricRightEasterEgg");
            //Helpers.LightingBufferRenderer.DrawWithLighting(target, tex3, source, Color.White * ((((float)Math.Sin(Main.GameUpdateCount  / 50f) + 1) / 2f) + 0.1f), sb, Configs.LightImportance.Some);//a
        }

        public override void ScrollDraw(SpriteBatch sb)
        {
            string path = AssetDirectory.VitricBoss + Name;
            Texture2D tex = GetTexture(path);
            int height1 = (int)(ScrollTimer / Scrolltime * tex.Height);
            int height2 = tex.Height - height1;
            //Color color = new Color(180, 225, 255);
            Vector2 off = Vector2.One.RotatedByRandom(6.28f) * shake;

            Rectangle target1 = new Rectangle((int)(npc.position.X - Main.screenPosition.X + off.X), (int)(npc.position.Y - tex.Height * 2 + height1 + height2 - Main.screenPosition.Y + off.Y), tex.Width, height1);
            Rectangle target2 = new Rectangle((int)(npc.position.X - Main.screenPosition.X + off.X), (int)(npc.position.Y - tex.Height + height1 - Main.screenPosition.Y + off.Y), tex.Width, height2);
            Rectangle source2 = new Rectangle(0, 0, tex.Width, height2);
            Rectangle source1 = new Rectangle(0, tex.Height - height1, tex.Width, height1);

            Helpers.LightingBufferRenderer.DrawWithLighting(target1, tex, source1, default);
            Helpers.LightingBufferRenderer.DrawWithLighting(target2, tex, source2, default);

            Texture2D tex2 = GetTexture(path + "Glow");
            sb.Draw(tex2, target1, source1, Color.White * (0.5f + (float)System.Math.Sin(StarlightWorld.rottime) * 0.1f), 0, Vector2.Zero, 0, 0);
            sb.Draw(tex2, target2, source2, Color.White * (0.5f + (float)System.Math.Sin(StarlightWorld.rottime) * 0.1f), 0, Vector2.Zero, 0, 0);

            //Texture2D tex2 = ModContent.GetTexture("StarlightRiver/Assets/Bosses/VitricBoss/VitricRightEasterEgg");
            //Helpers.LightingBufferRenderer.DrawWithLighting(target1, tex2, source1, default, sb, Configs.LightImportance.Some);
            //Helpers.LightingBufferRenderer.DrawWithLighting(target2, tex2, source2, default, sb, Configs.LightImportance.Some);
        }

        public override void SpawnPlatforms(bool rising = true)
        {
            PlacePlatform(160, 90, NPCType<VitricBossPlatformDown>(), rising);
            PlacePlatform(272, 330, NPCType<VitricBossPlatformDown>(), rising);
            PlacePlatform(192, 580, NPCType<VitricBossPlatformDown>(), rising);
            PlacePlatform(394, 198, NPCType<VitricBossPlatformDownSmall>(), rising);
            PlacePlatform(94, 440, NPCType<VitricBossPlatformDownSmall>(), rising);
            PlacePlatform(424, 660, NPCType<VitricBossPlatformDownSmall>(), rising);
            PlacePlatform(294, 760, NPCType<VitricBossPlatformDownSmall>(), rising);
        }

        public override void ResyncPlatforms()
        {
            SyncPlatform(platforms[0], 90, false);
            SyncPlatform(platforms[1], 330, false);
            SyncPlatform(platforms[2], 580, false);
            SyncPlatform(platforms[3], 198, false);
            SyncPlatform(platforms[4], 440, false);
            SyncPlatform(platforms[5], 660, false);
            SyncPlatform(platforms[6], 760, false);
        }
    }
}