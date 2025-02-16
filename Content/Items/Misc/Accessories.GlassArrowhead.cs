﻿using Microsoft.Xna.Framework;
using StarlightRiver.Content.Items.BaseTypes;
using StarlightRiver.Core;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace StarlightRiver.Content.Items.Misc
{
	public class GlassArrowhead : SmartAccessory
    {
        public override string Texture => AssetDirectory.MiscItem + Name;
        public GlassArrowhead() : base("Glass Arrowhead", "Critical strikes cause fired arrows to shatter into glass shards") { }

        public override bool Autoload(ref string name)
        {
            StarlightPlayer.OnHitNPCWithProjEvent += OnHitNPCWithProjAccessory;
            return true;
        }

        private void OnHitNPCWithProjAccessory(Player player, Projectile proj, NPC target, int damage, float knockback, bool crit)
        {
            if (Equipped(player) && proj.arrow && crit)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 velocity = proj.velocity.RotatedByRandom(MathHelper.Pi / 6f);
                    velocity *= Main.rand.NextFloat(0.5f, 0.75f);
                    Projectile.NewProjectile(proj.Center, velocity, ModContent.ProjectileType<Vitric.VitricArrowShattered>(), (int)(damage * 0.2f), knockback * 0.15f, player.whoAmI);
                }
            }
        }
    }
}