﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shockah.Base;
using System;
using System.Collections.Generic;
using TAPI;
using Terraria;

namespace Shockah.FCM
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
		protected PrefixSlot[] slots = new PrefixSlot[COLS * ROWS];
		internal ItemSlotPrefixFCM slotItem = null;
		private int _Scroll = 0;
		protected readonly Sorter<Prefix>
			SID = new Sorter<Prefix>("ID", (i1, i2) => { return i1.type.CompareTo(i2.type); }, (npc) => { return true; }),
			SName = new Sorter<Prefix>("Name", (i1, i2) => { return i1.displayName.CompareTo(i2.displayName); }, (npc) => { return true; });

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

			sorters.AddRange(new Sorter<Prefix>[] { SID, SName });

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
				() => { return MBase.me.textures[reverseSort ? "Images/ArrowDecrease.png" : "Images/ArrowIncrease.png"]; }
			);
			foreach (Sorter<Prefix> sorter2 in sorters) sortingChooser.Add(new Tuple<string, Sorter<Prefix>>(sorter2.name, sorter2));

			slotItem = new ItemSlotPrefixFCM(this);
		}

		public override void OnOpen()
		{
			base.OnOpen();
			if (!resetInterface) { resetInterface = true; return; }
			sorter = sorters[0];
			reverseSort = false;
			Refresh(true);
		}

		public override void Draw(InterfaceLayer layer, SpriteBatch sb)
		{
			bool blocked = false;
			string oldTyping = typing;
			base.Draw(layer, sb);
			if (oldTyping != typing) Refresh(true);

			int scrollBy = (Main.mouseState.ScrollWheelValue - Main.oldMouseState.ScrollWheelValue) / 120;
			int oldScroll = Scroll;
			Scroll -= scrollBy;
			if (Scroll != oldScroll) Refresh(false);

			SDrawing.StringShadowed(sb, Main.fontMouseText, (filtered.Count == defs.Count ? "Prefixes" : "Matching prefixes") + ": " + filtered.Count, new Vector2(POS_X, POS_Y - 26));

			Main.inventoryScale = 1f;
			int offX = (int)Math.Ceiling(SLOT_W * Main.inventoryScale), offY = (int)Math.Ceiling(SLOT_H * Main.inventoryScale);
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

			SDrawing.StringShadowed(sb, Main.fontMouseText, "Sort:", new Vector2(POS_X + 16 + COLS * offX * Main.inventoryScale, POS_Y - 26), Color.White, SORT_TEXT_SCALE);
			sortingChooser.pos = new Vector2(POS_X + 48 + COLS * offX * Main.inventoryScale, POS_Y - 30);
			sortingChooser.size = new Vector2(72, 24);
			blocked = sortingChooser.Draw(sb, false, !blocked) || blocked;

			float oldInventoryScale = Main.inventoryScale;
			Main.inventoryScale = .75f;

			sortingChooser.Draw(sb, true, false);

			string text = typing == null ? filterText : typing + "|";
			if (!string.IsNullOrEmpty(text))
			{
				Drawing.DrawBox(sb, POS_X, POS_Y + ROWS * offY * oldInventoryScale + 4, 20 + COLS * offX * oldInventoryScale, 32);
				SDrawing.StringShadowed(sb, Main.fontMouseText, text, new Vector2(POS_X + 8, POS_Y + ROWS * offY * oldInventoryScale + 8));
			}
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
			foreach (Prefix prefix in defs)
			{
				if ((typing != null || filterText != null) && (defsNames[defs.IndexOf(prefix)].ToLower().IndexOf((typing == null ? filterText : typing).ToLower()) == -1 || prefix.type == 0)) continue;
				if (!sorter.allow(prefix)) continue;
				if (prefix.type != 0 && !slotItem.MyItem.IsBlank() && !prefix.CanApplyToItem(slotItem.MyItem)) continue;
				filtered.Add(prefix);
			}
			filtered.Sort(sorter);
			if (reverseSort) filtered.Reverse();
		}
	}
}