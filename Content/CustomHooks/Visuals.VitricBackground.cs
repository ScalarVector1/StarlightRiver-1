﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using StarlightRiver.Configs;
using StarlightRiver.Core;
using StarlightRiver.Helpers;
using System;
using Terraria;
using static Terraria.ModLoader.ModContent;

namespace StarlightRiver.Content.CustomHooks
{
    class VitricBackground : HookGroup
    {
        internal static ParticleSystem ForegroundParticles;
        internal static ParticleSystem BackgroundParticles;

        //Its just drawing, but its alot of drawing, and some questionable vanilla RenderBlack replication stuff that likely needs to be removed at some point.
        public override SafetyLevel Safety => SafetyLevel.Questionable;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            ForegroundParticles = new ParticleSystem("StarlightRiver/Assets/GUI/HolyBig", UpdateForegroundBody, 1);
            BackgroundParticles = new ParticleSystem("StarlightRiver/Assets/GUI/Holy", UpdateBackgroundBody, 1);

            On.Terraria.Main.DrawBackgroundBlackFill += DrawVitricBackground;
            On.Terraria.Main.DrawBlack += ForceDrawBlack;
            IL.Terraria.Main.DrawBlack += ChangeBlackThreshold;
        }

		public override void Unload()
        {
            ForegroundParticles = null;
            BackgroundParticles = null;
        }

        private void ForceDrawBlack(On.Terraria.Main.orig_DrawBlack orig, Main self, bool force)
        {
            if (StarlightWorld.VitricBiome.Intersects(Helper.ScreenTiles))
                orig(self, true);
            else
                orig(self, force);
        }

        private void ChangeBlackThreshold(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.TryGotoNext(n => n.MatchLdloc(5), n => n.MatchStloc(11)); //beginning of the loop, local 11 is a looping variable
            c.Index++; //this is kinda goofy since I dont think you could actually ever write c# to compile to the resulting IL from emitting here.
            c.Emit(OpCodes.Ldloc, 3); //pass the original value so we can set that instead if we dont want to change the threshold
            c.EmitDelegate<Func<float, float>>(NewThreshold); //check if were in the biome to set, else set the original value
            c.Emit(OpCodes.Stloc, 3); //num2 in vanilla, controls minimum threshold to turn a tile black
        }

		private float NewThreshold(float orig)
		{
            if (StarlightWorld.VitricBiome.Intersects(Helper.ScreenTiles))
                return 0.1f;
            else
                return orig;
        }

		private static void UpdateForegroundBody(Particle particle)
        {
            particle.Timer--;
            particle.StoredPosition += particle.Velocity;

            var randTime = (particle.GetHashCode() % 100) + 200f;

            particle.Position.X = particle.StoredPosition.X - Main.screenPosition.X + GetParallaxOffset(particle.StoredPosition.X, 0.15f) + (float)Math.Sin(particle.Timer / randTime * 6.28f) * 20;
            particle.Position.Y = particle.StoredPosition.Y - Main.screenPosition.Y + GetParallaxOffsetY(particle.StoredPosition.Y, 0.1f);

            particle.Color = Color.Lerp(new Color(255, 40, 0), new Color(255, 170, 100), particle.Timer / 1800f) * (0.85f * particle.Timer / 1800f);
            particle.Scale = (particle.Timer / 1800f) * 0.55f;
            particle.Rotation += 0.015f;
        }

        private static void UpdateBackgroundBody(Particle particle)
        {
            particle.Timer--;
            particle.StoredPosition += particle.Velocity;
            var randTime = (particle.GetHashCode() % 50) + 100f;
            particle.Position.X = particle.StoredPosition.X - Main.screenPosition.X + GetParallaxOffset(particle.StoredPosition.X, 0.5f) + (float)Math.Sin(particle.Timer / randTime * 6.28f) * 6;
            particle.Position.Y = particle.StoredPosition.Y - Main.screenPosition.Y + GetParallaxOffsetY(particle.StoredPosition.Y, 0.2f);
            particle.Color = Color.Lerp(Color.Red, new Color(255, 255, 200), particle.Timer / 2400f);
            particle.Scale = (particle.Timer / 2400f);
            particle.Rotation += 0.02f;
        }

        private void DrawVitricBackground(On.Terraria.Main.orig_DrawBackgroundBlackFill orig, Main self)
        {
            orig(self);

            if (Main.gameMenu || Main.dedServ)
                return;

            Player player = Main.LocalPlayer;

            if (player != null && StarlightWorld.VitricBiome.Intersects(Helper.ScreenTiles))
            {
                Vector2 basepoint = (StarlightWorld.VitricBiome != null) ? StarlightWorld.VitricBiome.TopLeft() * 16 + new Vector2(-2000, 0) : Vector2.Zero;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default, default, Main.GameViewMatrix.ZoomMatrix);

                float x = basepoint.X + GetParallaxOffset(basepoint.X, 0.6f) - Main.screenPosition.X;
                float y = basepoint.Y + GetParallaxOffsetY(basepoint.Y, 0.2f) - Main.screenPosition.Y;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default, default, Main.GameViewMatrix.ZoomMatrix);

                for (int k = 5; k >= 0; k--)
                {
                    if (k == 3)
                        BackgroundParticles.DrawParticles(Main.spriteBatch);

                    int off = 140 + (340 - k * 110);
                    if (k == 0) off -= 100;
					if (k == 1) off -= 25;
                    if (k == 2) off -= 100;
                    if (k == 5) off = 100;

                    DrawLayer(basepoint, GetTexture("StarlightRiver/Assets/Backgrounds/Glass" + k), k + 1, Vector2.UnitY * off, default, false); //the crystal layers and front sand

                    if(k == 1)
					{
                        DrawLayer(basepoint, GetTexture("StarlightRiver/Assets/Backgrounds/GlassTowerLoop"), k + 1, new Vector2(1304, off - 248), default, false);
                    }

                    if (k == 0)
                    {
                        Main.spriteBatch.End();
                        Main.spriteBatch.Begin(default, BlendState.Additive, SamplerState.PointClamp, default, default, default, Main.GameViewMatrix.ZoomMatrix);

                        var progress = (float)Math.Sin(Main.GameUpdateCount / 50f);
                        var color = new Color(255, 255, 100);

                        DrawLayer(basepoint, GetTexture("StarlightRiver/Assets/Backgrounds/Glass0Glow"), k + 1, Vector2.UnitY * off + Vector2.One * progress * 2, color * (0.45f + progress * 0.2f), false);
                        DrawLayer(basepoint, GetTexture("StarlightRiver/Assets/Backgrounds/Glass0Glow"), k + 1, Vector2.UnitY * off + Vector2.One.RotatedBy(MathHelper.PiOver2) * progress * 2, color * (0.45f + progress * 0.2f), false);

                        Main.spriteBatch.End();
                        Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default, default, Main.GameViewMatrix.ZoomMatrix);
                    }

                    if (k == 1)
                        ForegroundParticles.DrawParticles(Main.spriteBatch);
                }

                int screenCenterX = (int)(Main.screenPosition.X + Main.screenWidth / 2);
                for (int k = (int)(screenCenterX - basepoint.X) - (int)(Main.screenWidth * 1.5f); k <= (int)(screenCenterX - basepoint.X) + (int)(Main.screenWidth * 1.5f); k += 30)
                {
                    Vector2 spawnPos = basepoint + new Vector2(2000 + Main.rand.Next(12000), 1800);
                    if (Main.rand.Next(400) == 0)
                        BackgroundParticles.AddParticle(new Particle(new Vector2(0, basepoint.Y + 1600), new Vector2(0, Main.rand.NextFloat(-1.3f, -0.3f)), 0, 0, Color.White, 2400, spawnPos));

                    if (Main.rand.Next(1300) == 0)
                        ForegroundParticles.AddParticle(new Particle(new Vector2(0, basepoint.Y + 1600), new Vector2(0, Main.rand.NextFloat(-1.9f, -0.3f)), 0, 0, Color.White, 1800, spawnPos));
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default, default, Main.GameViewMatrix.ZoomMatrix);
                DrawTilingBackground(Main.spriteBatch);
            }
        }

        private void DrawTilingBackground(SpriteBatch spriteBatch)
        {
            Texture2D tex = GetTexture("StarlightRiver/Assets/Backgrounds/VitricSand");
            Texture2D texBot = GetTexture("StarlightRiver/Assets/Backgrounds/VitricSandBottom");
            Texture2D texTop = GetTexture("StarlightRiver/Assets/Backgrounds/VitricSandTop");
            Texture2D texLeft = GetTexture("StarlightRiver/Assets/Backgrounds/VitricSandLeft");
            Texture2D texRight = GetTexture("StarlightRiver/Assets/Backgrounds/VitricSandRight");

            Rectangle blacklist = new Rectangle(StarlightWorld.VitricBiome.X, StarlightWorld.VitricBiome.Y - 2, StarlightWorld.VitricBiome.Width, StarlightWorld.VitricBiome.Height);

            for (int x = -tex.Width; x <= Main.screenWidth + tex.Width; x += tex.Width)
                for (int y = -tex.Height; y <= Main.screenHeight + tex.Height; y += tex.Height)
                {
                    Vector2 pos = new Vector2(x, y) - new Vector2(Main.screenPosition.X % tex.Width, Main.screenPosition.Y % tex.Height);
                    Texture2D drawtex;
                    if (!CheckBackground(pos + new Vector2(0, tex.Height), tex.Size(), blacklist, true))
                        drawtex = texTop;
                    else if (!CheckBackground(pos + new Vector2(0, -tex.Height), tex.Size(), blacklist, true))
                        drawtex = texBot;
                    else if (!CheckBackground(pos + new Vector2(-tex.Width, 0), tex.Size(), blacklist, true))
                        drawtex = texRight;
                    else if (!CheckBackground(pos + new Vector2(tex.Width, 0), tex.Size(), blacklist, true))
                        drawtex = texLeft;
                    else
                        drawtex = tex;

                    if (CheckBackground(pos, drawtex.Size(), blacklist))
                        LightingBufferRenderer.DrawWithLighting(pos, drawtex, default);
                }
        }

        private bool CheckBackground(Vector2 pos, Vector2 size, Rectangle biome, bool dontCheckScreen = false)
        {
            if (dontCheckScreen || Helper.OnScreen(new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y)))
            {
                if (!Main.BackgroundEnabled) 
                    return true;
                else if (!biome.Contains(((pos + Main.screenPosition) / 16).ToPoint()) || !biome.Contains(((pos + size + Main.screenPosition) / 16).ToPoint()))
                    return true;
            }
            return false;
        }

        private static void DrawLayer(Vector2 basepoint, Texture2D texture, float parallax, Vector2 off = default, Color color = default, bool flip = false)
        {
            if (color == default) color = Color.White;

            for (int k = 0; k <= 5; k++)
            {
                float x = basepoint.X + off.X + (k * 739 * 4) + GetParallaxOffset(basepoint.X, parallax * 0.1f) - (int)Main.screenPosition.X;
                float y = basepoint.Y + off.Y - (int)Main.screenPosition.Y + GetParallaxOffsetY(basepoint.Y + StarlightWorld.VitricBiome.Height * 8, parallax * 0.04f);

                if (x > -texture.Width && x < Main.screenWidth + 30)
                    Main.spriteBatch.Draw(texture, new Vector2(x, y), null, color, 0f, Vector2.Zero, 1f, flip ? SpriteEffects.FlipVertically : 0, 0);
            }
        }

        private static int GetParallaxOffset(float startpoint, float factor)
        {
            float vanillaParallax = 1 - (Main.caveParallax - 0.8f) / 0.2f;
            return (int)((Main.screenPosition.X + Main.screenWidth / 2 - startpoint) * factor * vanillaParallax);
        }

        private static int GetParallaxOffsetY(float startpoint, float factor)
        {
            return (int)((Main.screenPosition.Y + Main.screenHeight / 2 - startpoint) * factor);
        }
    }
}

