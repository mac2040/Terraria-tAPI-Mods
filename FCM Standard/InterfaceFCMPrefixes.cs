﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shockah.Base;
using System;
using System.Collections.Generic;
using TAPI;
using Terraria;

namespace Shockah.FCM.Standard
{
	public class InterfaceFCMPrefixes : InterfaceFCM<Prefix>
	{
		public const int COLS = 2, ROWS = 7, POS_X = 20, POS_Y = 306;
		public const float SLOT_W = 150, SLOT_H = 28;
		public const float SORT_TEXT_SCALE = .75f;

		public static InterfaceFCMPrefixes me = null;
		internal static List<Prefix> defs = new List<Prefix>();
		internal static List<string> defsNames = new List<string>();

		public static void Reset()
		{
			Item fake = new Item();
			fake.displayName = "";
			defs.Clear();
			foreach (KeyValuePair<string, Prefix> kvp in Defs.prefixes)
			{
				defs.Add(kvp.Value);
				defsNames.Add(kvp.Value.type == 0 ? "<none>" : kvp.Value.SetItemName(fake).Trim());
			}
		}

		protected readonly ElSlider slider;
		protected readonly ElChooser<Sorter<Prefix>> sortingChooser;
		protected readonly ElButton bSearch, bSearchBar;
		protected PrefixSlot[] slots = new PrefixSlot[COLS * ROWS];
		internal ItemSlotPrefixFCM slotItem = null;
		private int _Scroll = 0;
		protected readonly Sorter<Prefix>
			SID, SName, SRarity, SDamage, SCrit, SKnockback, SUseTime, SManaCost, SDefense, SMoveSpeed, SMeleeSpeed, SVelocity, SSize;

		protected int Scroll
		{
			get
			{
				return _Scroll;
			}
			set
			{
				_Scroll = Math.Min(Math.Max(value, 0), ScrollMax);
			}
		}
		protected int ScrollMax
		{
			get
			{
				return Math.Max((int)Math.Ceiling(1f * (filtered.Count - ROWS * COLS) / COLS), 0);
			}
		}

		public InterfaceFCMPrefixes()
		{
			me = this;
			if (Main.dedServ) return;

			SID = new Sorter<Prefix>("ID", (i1, i2) => i1.type.CompareTo(i2.type), (p) => true);
			SName = new Sorter<Prefix>("Name", (i1, i2) => InterfaceFCMPrefixes.defsNames[InterfaceFCMPrefixes.defs.IndexOf(i1)].CompareTo(InterfaceFCMPrefixes.defsNames[InterfaceFCMPrefixes.defs.IndexOf(i2)]), (p) => p != null);
			SRarity = new Sorter<Prefix>("Rarity", (i1, i2) => i1.tier.CompareTo(i2.tier), (p) => p != null);
			SDamage = new Sorter<Prefix>("Damage", (i1, i2) => Math2.Max(i1.damage, i1.meleeDamage, i1.rangedDamage, i1.magicDamage).CompareTo(Math2.Max(i2.damage, i2.meleeDamage, i2.rangedDamage, i2.magicDamage)), (p) => p != null);
			SCrit = new Sorter<Prefix>("Crit", (i1, i2) => Math2.Max(i1.crit, i1.meleeCrit, i1.rangedCrit, i1.magicCrit).CompareTo(Math2.Max(i2.crit, i2.meleeCrit, i2.rangedCrit, i2.magicCrit)), (p) => p != null);
			SKnockback = new Sorter<Prefix>("Knockback", (i1, i2) => i1.knockBack.CompareTo(i2.knockBack), (p) => p != null);
			SUseTime = new Sorter<Prefix>("Use time", (i1, i2) => i1.useTime.CompareTo(i2.useTime), (p) => p != null);
			SManaCost = new Sorter<Prefix>("Mana cost", (i1, i2) => i1.mana.CompareTo(i2.mana), (p) => p != null);
			SDefense = new Sorter<Prefix>("Defense", (i1, i2) => i1.defense.CompareTo(i2.defense), (p) => p != null && p.defense != 0);
			SMoveSpeed = new Sorter<Prefix>("Move speed", (i1, i2) => i1.moveSpeed.CompareTo(i2.moveSpeed), (p) => p != null && p.moveSpeed != 1f);
			SMeleeSpeed = new Sorter<Prefix>("Speed", (i1, i2) => i1.meleeSpeed.CompareTo(i2.meleeSpeed), (p) => p != null && p.meleeSpeed != 1f);
			SVelocity = new Sorter<Prefix>("Velocity", (i1, i2) => i1.shootSpeed.CompareTo(i2.shootSpeed), (p) => p != null && p.shootSpeed != 1f);
			SSize = new Sorter<Prefix>("Size", (i1, i2) => i1.size.CompareTo(i2.size), (p) => p != null);

			sorters.AddRange(new Sorter<Prefix>[] { SID, SName, SRarity, SDamage, SCrit, SKnockback, SUseTime, SManaCost, SDefense, SMoveSpeed, SMeleeSpeed, SVelocity, SSize });

			slider = new ElSlider(
				(scroll) => { if (Scroll != scroll) { Scroll = scroll; Refresh(false); } },
				() => { return Scroll; },
				() => { return ROWS; },
				() => { return (int)Math.Ceiling(1f * filtered.Count / COLS); }
			);

			sorter = sorters[0];
			sortingChooser = new ElChooser<Sorter<Prefix>>(
				(item) => { reverseSort = object.ReferenceEquals(sorter, item) ? !reverseSort : false; sorter = item; Refresh(true); },
				() => { return sorter; },
				() => { return Shockah.FCM.MBase.me.textures[reverseSort ? "Images/ArrowDecrease" : "Images/ArrowIncrease"]; }
			);
			foreach (Sorter<Prefix> sorter2 in sorters) sortingChooser.Add(new Tuple<string, Sorter<Prefix>>(sorter2.name, sorter2));

			bSearch = new ElButton(
				(b, mb) =>
				{
					if (typing == null && filterText != null)
					{
						filterText = null;
						Refresh(true);
					}
					else
					{
						Main.GetInputText("");
						if (typing == null) typing = "";
						else
						{
							filterText = typing;
							if (filterText == "") filterText = null;
							typing = null;
						}
					}
				},
				(b, sb, mb) =>
				{
					Texture2D tex = typing == null && filterText != null ? Main.cdTexture : Shockah.FCM.MBase.me.textures["Images/Arrow"];
					float tscale = 1f;
					if (tex.Width * tscale > b.size.X - 4) tscale = (b.size.X - 4) / (tex.Width * tscale);
					if (tex.Height * tscale > b.size.Y - 4) tscale = (b.size.Y - 4) / (tex.Height * tscale);
					sb.Draw(tex, b.pos + b.size / 2, null, Color.White, 0f, tex.Size() / 2, tscale, SpriteEffects.None, 0f);
				}
			);

			bSearchBar = new ElButton(
				(b, mb) =>
				{
					Main.GetInputText("");
					if (typing == null) typing = "";
					else
					{
						filterText = typing;
						if (filterText == "") filterText = null;
						typing = null;
					}
				},
				(b, sb, mb) =>
				{
					if (typing == null && filterText == null) Drawing.StringShadowed(sb, Main.fontMouseText, "Search...", new Vector2(b.pos.X + 8, b.pos.Y + 4), Color.White * .5f);
					else Drawing.StringShadowed(sb, Main.fontMouseText, typing == null ? filterText : typing + "|", new Vector2(b.pos.X + 8, b.pos.Y + 4));
				}
			);

			slotItem = new ItemSlotPrefixFCM(this);

			sorter = sorters[0];
			reverseSort = false;
			Refresh(true);
		}

		public override void OnOpen()
		{
			base.OnOpen();
			if (!resetInterface) { resetInterface = true; return; }
			/*sorter = sorters[0];
			reverseSort = false;
			Refresh(true);*/
		}

		public override void OnClose()
		{
			base.OnClose();
			if (!slotItem.MyItem.IsBlank())
			{
				Main.localPlayer.GetItem(Main.myPlayer, slotItem.MyItem.Clone()); //used to be DeepClone
				slotItem.MyItem.SetDefaults(0);
			}
		}

		public override void Draw(InterfaceLayer layer, SpriteBatch sb)
		{
			Main.inventoryScale = 1f;
			int offX = (int)Math.Ceiling(SLOT_W * Main.inventoryScale), offY = (int)Math.Ceiling(SLOT_H * Main.inventoryScale);
			
			bool blocked = false;
			string oldTyping = typing;
			base.Draw(layer, sb);

			bSearch.pos = new Vector2(POS_X + COLS * offX - 12, POS_Y + ROWS * offY + 4);
			bSearch.size = new Vector2(32, 32);
			blocked = bSearch.Draw(sb, false, !blocked) || blocked;

			bSearchBar.pos = new Vector2(POS_X, POS_Y + ROWS * offY + 4);
			bSearchBar.size = new Vector2(COLS * offX - 16, 32);
			blocked = bSearchBar.Draw(sb, false, !blocked) || blocked;

			if (oldTyping != typing) Refresh(true);

			int scrollBy = (Main.mouseState.ScrollWheelValue - Main.oldMouseState.ScrollWheelValue) / 120;
			int oldScroll = Scroll;
			Scroll -= scrollBy;
			if (Scroll != oldScroll) Refresh(false);

			Drawing.StringShadowed(sb, Main.fontMouseText, (filtered.Count == defs.Count ? "Prefixes" : "Matching prefixes") + ": " + filtered.Count, new Vector2(POS_X, POS_Y - 26));

			Main.inventoryScale = 1f;
			for (int y = 0; y < ROWS; y++) for (int x = 0; x < COLS; x++)
			{
				slots[x + y * COLS].scale = Main.inventoryScale;
				slots[x + y * COLS].UpdatePos(new Vector2(POS_X + x * offX, POS_Y + y * offY));
				slots[x + y * COLS].Draw(sb, true, !blocked);
			}

			slider.pos = new Vector2(POS_X + 4 + COLS * offX * Main.inventoryScale, POS_Y);
			slider.size = new Vector2(16, ROWS * offY * Main.inventoryScale);
			blocked = slider.Draw(sb, true, !blocked) || blocked;

			slotItem.scale = Main.inventoryScale;
			slotItem.UpdatePos(new Vector2(POS_X + COLS * offX * Main.inventoryScale + 32, POS_Y + ROWS * offY * Main.inventoryScale - 56));
			slotItem.Draw(sb, true, !blocked);

			Drawing.StringShadowed(sb, Main.fontMouseText, "Sort:", new Vector2(POS_X - 8 + COLS * offX * Main.inventoryScale, POS_Y - 22), Color.White, SORT_TEXT_SCALE);
			sortingChooser.pos = new Vector2(POS_X + 24 + COLS * offX * Main.inventoryScale, POS_Y - 26);
			sortingChooser.size = new Vector2(96, 20);
			blocked = sortingChooser.Draw(sb, false, !blocked) || blocked;

			float oldInventoryScale = Main.inventoryScale;
			Main.inventoryScale = .75f;

			bSearch.Draw(sb, true, false);
			bSearchBar.Draw(sb, true, false);
			sortingChooser.Draw(sb, true, false);
		}

		public void Refresh(bool resetScroll)
		{
			Scroll = resetScroll ? 0 : Scroll;
			for (int i = 0; i < slots.Length; i++) slots[i] = new PrefixSlot(this, i + Scroll * COLS, new Vector2(SLOT_W, SLOT_H));
			RunFilters();
		}

		protected void RunFilters()
		{
			filtered.Clear();
			string typing = this.typing == null ? filterText : this.typing;
			if (typing != null) typing = typing.ToLower();
			foreach (Prefix prefix in defs)
			{
				if (prefix.type == 0) continue;
				string name = defsNames[defs.IndexOf(prefix)].ToLower();
				if (typing != null && name.IndexOf(typing) == -1) continue;
				if (!sorter.allow(prefix)) continue;
				if (prefix.type != 0 && !slotItem.MyItem.IsBlank() && !prefix.CanApplyToItem(slotItem.MyItem)) continue;
				filtered.Add(prefix);
			}
			filtered.Sort(sorter);
			if (reverseSort) filtered.Reverse();
			if (typing == null)
			{
				if (filtered.Count == 0)
					filtered.Add(defs[0]);
				else
					filtered.Insert(0, defs[0]);
			}
		}
	}
}