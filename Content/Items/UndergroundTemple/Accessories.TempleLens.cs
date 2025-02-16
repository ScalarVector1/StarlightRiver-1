﻿using StarlightRiver.Content.Items.BaseTypes;
using StarlightRiver.Core;
using Terraria;
using Terraria.ID;
using static Terraria.ModLoader.ModContent;

namespace StarlightRiver.Content.Items.UndergroundTemple
{
	class TempleLens : SmartAccessory
    {
        public override string Texture => AssetDirectory.CaveTempleItem + Name;

        public TempleLens() : base("Ancient Lens", "Critical strikes cause enemies around the struck enemy to glow\n+3% critical strike chance") { }

        public override void SafeSetDefaults()
        {
            item.rare = ItemRarityID.Blue;
        }

        public override void SafeUpdateEquip(Player player)
        {
            player.meleeCrit += 3;
            player.rangedCrit += 3;
            player.magicCrit += 3;
        }

        public override bool Autoload(ref string name)
        {
            StarlightPlayer.ModifyHitNPCEvent += ModifyHurtLens;
            StarlightProjectile.ModifyHitNPCEvent += ModifyProjectileLens;
            return true;
        }

        private void ModifyHurtLens(Player player, Item item, NPC target, ref int damage, ref float knockback, ref bool crit)
        {
            if (Equipped(player) && crit)
                target.AddBuff(BuffType<Buffs.Illuminant>(), 300);
        }

        private void ModifyProjectileLens(Projectile projectile, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (Equipped(Main.player[projectile.owner]) && crit)
                target.AddBuff(BuffType<Buffs.Illuminant>(), 300);
        }
    }
}
