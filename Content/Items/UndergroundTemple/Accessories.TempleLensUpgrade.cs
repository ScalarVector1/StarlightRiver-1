﻿using StarlightRiver.Content.Buffs;
using StarlightRiver.Content.Items.BaseTypes;
using StarlightRiver.Core;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace StarlightRiver.Content.Items.UndergroundTemple
{
	class TempleLensUpgrade : SmartAccessory
    {
        public override string Texture => AssetDirectory.CaveTempleItem + Name;

        public TempleLensUpgrade() : base("Truestrike Lens", "Critical strikes expose enemies near the struck enemy\nExposed enemies take significantly more damage on the first hit\n+4% critical strike chance\n+10% critical strike damage") { }

        public override void SafeSetDefaults()
        {
            item.rare = ItemRarityID.Orange;
        }

        public override void SafeUpdateEquip(Player player)
        {
            player.meleeCrit += 4;
            player.rangedCrit += 4;
            player.magicCrit += 4;

            player.GetModPlayer<CritMultiPlayer>().AllCritMult += 0.1f;
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
                target.AddBuff(BuffType<Exposed>(), 120);
        }

        private void ModifyProjectileLens(Projectile projectile, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (Equipped(Main.player[projectile.owner]) && crit)
                target.AddBuff(BuffType<Exposed>(), 120);
        }

		public override void AddRecipes()
		{
            var r = new ModRecipe(mod);
            r.AddIngredient(ItemType<TempleLens>());
            r.AddIngredient(ItemType<Moonstone.MoonstoneBar>(), 5);
            r.AddTile(TileID.Anvils);
            r.SetResult(this);
            r.AddRecipe();
        }
	}

    class Exposed : SmartBuff
    {
        public Exposed() : base("Exposed", "How do you have this its NPC only", true) { }

		public override bool Autoload(ref string name, ref string texture)
		{
            StarlightNPC.ModifyHitByItemEvent += ExtraDamage;
            StarlightNPC.ModifyHitByProjectileEvent += ExtraDamageProjectile;

            return base.Autoload(ref name, ref texture);
		}

		private void ExtraDamageProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			if(Inflicted(npc) && !crit)
			{
                damage = (int)(damage * 1.2f);
                npc.DelBuff(npc.FindBuffIndex(Type));
			}
		}

		private void ExtraDamage(NPC npc, Player player, Item item, ref int damage, ref float knockback, ref bool crit)
		{
            if (Inflicted(npc) && !crit)
            {
                damage = (int)(damage * 1.2f);
                npc.DelBuff(npc.FindBuffIndex(Type));
            }
        }

		public override void Update(NPC npc, ref int buffIndex)
		{
			//spawn dust and do a spelunker-esque glow maybe?
		}
	}
}
