﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarlightRiver.Content.Dusts;
using StarlightRiver.Content.Items.BaseTypes;
using StarlightRiver.Core;
using StarlightRiver.Helpers;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace StarlightRiver.Content.Items.Vitric
{
	class CeirosExpert : SmartAccessory
    {
        public int cooldown = 0;

        public override string Texture => AssetDirectory.VitricItem + Name;

        public CeirosExpert() : base("Shattered Aegis", "Releases a burning ring when damaged\n'Meet your foes head-on, and give them a scorching embrace'") { }

        public override void SafeSetDefaults()
        {
            item.rare = ItemRarityID.Expert;
            item.accessory = true;
            item.width = 32;
            item.height = 32;
        }

        public override bool Autoload(ref string name)
        {
            StarlightPlayer.PreHurtEvent += PreHurtKnockback;
            return true;
        }

		public override void SafeUpdateEquip(Player player)
		{
            if (cooldown > 0)
                cooldown--;

            player.statDefense += 4;
		}

		private bool PreHurtKnockback(Player player, bool pvp, bool quiet, ref int damage, ref int hitDirection, ref bool crit, ref bool customDamage, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            var instance = (GetEquippedInstance(player) as CeirosExpert);

            if (Equipped(player) && instance.cooldown <= 0)
            {
                Helper.PlayPitched("Magic/FireSpell", 1, 0.75f, player.Center);
                Projectile.NewProjectile(player.Center, Vector2.Zero, ModContent.ProjectileType<FireRing>(), 20 + damage, 0, player.whoAmI);
                instance.cooldown = 60;
            }

            return true;
        }
    }

    class FireRing : ModProjectile, IDrawPrimitive
	{
        private List<Vector2> cache;
        private Trail trail;

        public float TimeFade => 1 - projectile.timeLeft / 20f;
        public float Radius => Helper.BezierEase((20 - projectile.timeLeft) / 20f) * 100;

        public override string Texture => AssetDirectory.Invisible;

        public override void SetDefaults()
		{
            projectile.friendly = true;
            projectile.width = 1;
            projectile.height = 1;
            projectile.tileCollide = false;
            projectile.timeLeft = 20;
            projectile.penetrate = -1;
		}

		public override void AI()
		{
            ManageCaches(ref cache);
            ManageTrail(ref trail, cache, 50);

            for (int k = 0; k < 8; k++)
            {
                float rot = Main.rand.NextFloat(0, 6.28f);
                Dust.NewDustPerfect(projectile.Center + Vector2.One.RotatedBy(rot) * (Radius + 15), ModContent.DustType<Dusts.Glow>(), Vector2.One.RotatedBy(rot + Main.rand.NextFloat(1.1f, 1.3f)) * 2, 0, new Color(255, 120 + (int)(100 * (float)Math.Sin(TimeFade * 3.14f)), 65), 0.4f);
            }
        }

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
            return Helper.CheckCircularCollision(projectile.Center, (int)Radius + 20, targetHitbox);
		}

		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
            target.velocity += Vector2.Normalize(target.Center - projectile.Center) * (20 + damage * 0.05f) * target.knockBackResist;
            target.AddBuff(BuffID.OnFire, 180);

            for(int k = 0; k < 4; k++)
			{
                Vector2 vel = Vector2.Normalize(target.Center - projectile.Center).RotatedByRandom(0.5f) * Main.rand.Next(5);

                Projectile.NewProjectile(target.Center, vel, ModContent.ProjectileType<NeedlerEmber>(), 0, 0);

                //Dust.NewDustPerfect(target.Center, ModContent.DustType<NeedlerDustTwo>(), vel);
                //Dust.NewDustPerfect(target.Center, ModContent.DustType<NeedlerDustFour>(), vel);
			}
		}

		private void ManageCaches(ref List<Vector2> cache)
        {
            if (cache is null)
            {
                cache = new List<Vector2>();

                for (int i = 0; i < 40; i++)
                {
                    cache.Add(projectile.Center);
                }
            }

            for(int k = 0; k < 40; k++)
			{
                cache[k] = (projectile.Center + Vector2.One.RotatedBy(k / 19f * 6.28f) * (Radius + 20));
			}

            while (cache.Count > 40)
            {
                cache.RemoveAt(0);
            }
        }

        private void ManageTrail(ref Trail trail, List<Vector2> cache, int width)
        {
            trail = trail ?? new Trail(Main.instance.GraphicsDevice, 40, new TriangularTip(40 * 4), factor => width, factor =>
            {
                return new Color(255, 100 + (int)(100 * (float)Math.Sin(TimeFade * 3.14f)), 65) * (float)Math.Sin(TimeFade * 3.14f) * 0.5f;
            });

            trail.Positions = cache.ToArray();
            trail.NextPosition = cache[39];
        }

        public void DrawPrimitives()
        {
            Effect effect = Filters.Scene["CeirosRing"].GetShader().Shader;

            Matrix world = Matrix.CreateTranslation(-Main.screenPosition.Vec3());
            Matrix view = Main.GameViewMatrix.ZoomMatrix;
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

            effect.Parameters["time"].SetValue(projectile.timeLeft * 0.01f);
            effect.Parameters["repeats"].SetValue(6);
            effect.Parameters["transformMatrix"].SetValue(world * view * projection);
            effect.Parameters["sampleTexture"].SetValue(ModContent.GetTexture("StarlightRiver/Assets/EnergyTrail"));

            trail?.Render(effect);

            effect.Parameters["sampleTexture"].SetValue(ModContent.GetTexture("StarlightRiver/Assets/FireTrail"));

            trail?.Render(effect);
        }

    }
}
