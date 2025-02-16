﻿using StarlightRiver.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StarlightRiver.Content.Buffs;
using StarlightRiver.Content.Tiles.Underground.EvasionShrineBullets;

namespace StarlightRiver.Content.Tiles.Underground
{
	class EvasionShrine : DummyTile
	{
		public override int DummyType => ModContent.ProjectileType<EvasionShrineDummy>();

		public override bool Autoload(ref string name, ref string texture)
		{
			texture = "StarlightRiver/Assets/Tiles/Underground/EvasionShrine";
			return true;
		}

		public override void SetDefaults()
		{
			QuickBlock.QuickSetFurniture(this, 3, 6, DustID.Stone, SoundID.Tink, false, new Color(100, 100, 100), false, false, "Mysterious Shrine");
		}

		public override void SafeNearbyEffects(int i, int j, bool closer)
		{
			var tile = Framing.GetTileSafely(i, j);

			if (Dummy is null)
				return;

			if (((EvasionShrineDummy)Dummy.modProjectile).State == 0 && tile.frameX > 36)
				tile.frameX -= 3 * 18;
		}

		public override bool NewRightClick(int i, int j)
		{
			var tile = (Tile)(Framing.GetTileSafely(i, j).Clone());

			if ((Dummy.modProjectile as EvasionShrineDummy).State == 0)
			{
				for (int x = 0; x < 3; x++)
					for (int y = 0; y < 6; y++)
					{
						int realX = x + i - tile.frameX / 18;
						int realY = y + j - tile.frameY / 18;

						Framing.GetTileSafely(realX, realY).frameX += 3 * 18;
					}

				(Dummy.modProjectile as EvasionShrineDummy).State = 1;
				(Dummy.modProjectile as EvasionShrineDummy).lives = 4;
				return true;
			}

			return false;
		}
	}

	internal partial class EvasionShrineDummy : Dummy, IDrawAdditive
	{
		public int maxAttacks = 15;
		public int lives;
		public List<int> attackOrder;

		public ref float Timer => ref projectile.ai[0];
		public ref float State => ref projectile.ai[1];

		public float Windup => Math.Min(1, Timer / 120f);

		public EvasionShrineDummy() : base(ModContent.TileType<EvasionShrine>(), 3 * 16, 6 * 16) { }

		public override void Update()
		{
			var color = new Vector3(0.15f, 0.12f, 0.2f) * 3.4f;

			Lighting.AddLight(projectile.Center + new Vector2(240, 0), color);
			Lighting.AddLight(projectile.Center + new Vector2(-240, 0), color);

			Lighting.AddLight(projectile.Center + new Vector2(240, -50), color);
			Lighting.AddLight(projectile.Center + new Vector2(-240, -50), color);

			Lighting.AddLight(projectile.Center + new Vector2(240, -100), color);
			Lighting.AddLight(projectile.Center + new Vector2(-240, -100), color);

			Lighting.AddLight(projectile.Center + new Vector2(0, -230), color);

			if (State == 0 && Parent.frameX > 3 * 18)
			{
				for (int x = 0; x < 3; x++)
					for (int y = 0; y < 6; y++)
					{
						int realX = ParentX - 1 + x;
						int realY = ParentY - 3 + y;

						Framing.GetTileSafely(realX, realY).frameX -= 3 * 18;
					}

				Timer = 0;
			}

			if (State != 0)
			{
				(mod as StarlightRiver).useIntenseMusic = true;
				Dust.NewDustPerfect(projectile.Center + new Vector2(Main.rand.NextFloat(-24, 24), 28), ModContent.DustType<Dusts.Glow>(), Vector2.UnitY * -Main.rand.NextFloat(2), 0, new Color(150, 30, 205) * Windup, 0.2f);

				if (State > 0)
				{
					Timer++;

					if(attackOrder is null)
					{
						attackOrder = new List<int>();
						for (int k = 0; k < 15; k++)
							attackOrder.Add(k);

						attackOrder = Helpers.Helper.RandomizeList<int>(attackOrder);
					}

					if (State > maxAttacks)
					{
						if(Timer > 600)
						{
							State = -1;
						}
						return;
					}

					SpawnObstacles((int)Timer - 128);
				}
			}

			if (State == -1 || lives <= 0 || (!Main.player.Any(n => n.active && !n.dead && Vector2.Distance(n.Center, projectile.Center) < 500))) //"fail" conditions, no living players in radius or already failing
			{
				State = -1;

				if (Timer > 128)
					Timer = 128;

				Timer--;

				if (Timer <= 0)
				{
					State = 0;
					attackOrder = null;
				}

				return;
			}
		}

		public void SpawnObstacles(int timer)
		{
			switch(attackOrder[(int)State - 1])
			{
				case 0: VerticalSawJaws(timer); break;
				case 1: HorizontalSawJaws(timer); break;
				case 2: DartBurst(timer); break;
				case 3: SpearsAndSwooshes(timer); break;
				case 4: TopSpearsBottomDarts(timer); break;
				case 5: MiddleSqueeze(timer); break;
				case 6: ShooFromMiddle(timer); break;
				case 7: SideSqueeze(timer); break;
				case 8: CruelDarts(timer); break;
				case 9: SquareSpears(timer); break;
				case 10: DartBurst2(timer); break;
				default: EndAttack(); break;
			}
		}

		public void SpawnBlade(Vector2 start, Vector2 vel, int time)
		{
			int i = Projectile.NewProjectile(start, vel, ModContent.ProjectileType<SawbladeSmall>(), 10, 0, Main.myPlayer);
			var mp = (Main.projectile[i].modProjectile as SawbladeSmall);
			Main.projectile[i].timeLeft = time;
			mp.parent = this;
		}

		public void SpawnDart(Vector2 start, Vector2 mid, Vector2 end, int duration)
		{
			int i = Projectile.NewProjectile(start, Vector2.Zero, ModContent.ProjectileType<Dart>(), 7, 0, Main.myPlayer);
			var mp = (Main.projectile[i].modProjectile as Dart);
			mp.endPoint = end;
			mp.midPoint = mid;
			mp.duration = duration;
			mp.parent = this;
		}

		public void SpawnSpear(Vector2 start, Vector2 end, int teleTime, int riseTime, int retractTime, int holdTime = 0)
		{
			int i = Projectile.NewProjectile(start, Vector2.Zero, ModContent.ProjectileType<Spear>(), 15, 0, Main.myPlayer);
			var mp = (Main.projectile[i].modProjectile as Spear);
			mp.endPoint = end;
			mp.timeToRise = riseTime;
			mp.timeToRetract = retractTime;
			mp.teleTime = teleTime;
			mp.holdTime = holdTime;
			mp.parent = this;
		}

		private void SpawnReward()
		{
			switch (lives)
			{
				case 4:
					break;
				case 3:
					break;
				case 2:
					break;
				case 1:
					break;
				default:
					break;
			}
		}

		public void DrawAdditive(SpriteBatch spriteBatch)
		{
			if (State != 0)
			{
				var tex = ModContent.GetTexture("StarlightRiver/Assets/Tiles/Moonstone/GlowSmall");
				var origin = new Vector2(tex.Width / 2, tex.Height);
				spriteBatch.Draw(tex, projectile.Center - Main.screenPosition + new Vector2(0, 60), default, GetBeamColor(StarlightWorld.rottime), 0, origin, 3.5f, 0, 0);
				spriteBatch.Draw(tex, projectile.Center - Main.screenPosition + new Vector2(10, 60), default, GetBeamColor(StarlightWorld.rottime + 2) * 0.8f, 0, origin, 2.5f, 0, 0);
				spriteBatch.Draw(tex, projectile.Center - Main.screenPosition + new Vector2(-10, 60), default, GetBeamColor(StarlightWorld.rottime + 4) * 0.8f, 0, origin, 3.2f, 0, 0);

				if (State > 0)
				{
					var fireTex = ModContent.GetTexture("StarlightRiver/Assets/Tiles/Underground/BrazierFlame");
					var frame = new Rectangle(0, 32 * (int)(Main.GameUpdateCount / 6 % 6), 16, 32);

					Vector2 leftPos = projectile.Center - Main.screenPosition + new Vector2(-248, -220);
					Vector2 leftMidPos = projectile.Center - Main.screenPosition + new Vector2(-120, -140);
					Vector2 rightMidPos = projectile.Center - Main.screenPosition + new Vector2(120, -140);
					Vector2 rightPos = projectile.Center - Main.screenPosition + new Vector2(248, -220);

					if (State > maxAttacks)
					{
						if (Timer > 300)
						{
							float progress = Math.Min(1, (Timer - 300) / 240f);

							leftPos = projectile.Center - Main.screenPosition + Vector2.SmoothStep(new Vector2(-248, -220), Vector2.Zero, progress);
							leftMidPos = projectile.Center - Main.screenPosition + Vector2.SmoothStep(new Vector2(-120, -140), Vector2.Zero, progress);
							rightMidPos = projectile.Center - Main.screenPosition + Vector2.SmoothStep(new Vector2(120, -140), Vector2.Zero, progress);
							rightPos = projectile.Center - Main.screenPosition + Vector2.SmoothStep(new Vector2(248, -220), Vector2.Zero, progress);
						}
					}

					if (lives > 0) //kinda gross lol, should probably figure a better way of doing this
					{
						spriteBatch.Draw(fireTex, leftPos, frame, new Color(200, 100, 255), 0, fireTex.Size() / 2, 1, 0, 0);
						spriteBatch.Draw(fireTex, leftPos, frame, Color.White, 0, fireTex.Size() / 2, 0.95f, 0, 0);
					}

					if (lives > 1)
					{
						spriteBatch.Draw(fireTex, leftMidPos, frame, new Color(200, 100, 255), 0, fireTex.Size() / 2, 1, 0, 0);
						spriteBatch.Draw(fireTex, leftMidPos, frame, Color.White, 0, fireTex.Size() / 2, 0.95f, 0, 0);
					}

					if (lives > 2)
					{
						spriteBatch.Draw(fireTex, rightMidPos, frame, new Color(200, 100, 255), 0, fireTex.Size() / 2, 1, 0, 0);
						spriteBatch.Draw(fireTex, rightMidPos, frame, Color.White, 0, fireTex.Size() / 2, 0.95f, 0, 0);
					}

					if (lives > 3)
					{
						spriteBatch.Draw(fireTex, rightPos, frame, new Color(200, 100, 255), 0, fireTex.Size() / 2, 1, 0, 0);
						spriteBatch.Draw(fireTex, rightPos, frame, Color.White, 0, fireTex.Size() / 2, 0.95f, 0, 0);
					}
				}
			}
		}

		private Color GetBeamColor(float time)
		{
			var sin = (0.5f + (float)Math.Sin(time * 2 + 1) * 0.5f);
			var sin2 = (0.5f + (float)Math.Sin(time) * 0.5f);
			return new Color(80 + (int)(50 * sin), 60, 255) * sin2 * Windup;
		}
	}
}
