﻿using Microsoft.Xna.Framework;
using Shockah.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAPI;
using Terraria;

namespace Shockah.ETooltip.ModuleItem
{
	public class ModuleItemRange : Module<Item>
	{
		public override void ModifyTip(ETipStyle style, OptionList options, STooltip tip, Item item)
		{
			if (item.tileBoost != 0)
			{
				if (HideSocial(options, item)) return;
				
				Color color = Color.White;
				switch ((string)options["itemToolRangeColor"].Value)
				{
					case "Green/Red": color = item.tileBoost > 0 ? Color.Lime : Color.Red; break;
					default: break;
				}
				if (GraySocial(options, item)) color = Color.DarkGray;

				if (style == ETipStyle.Vanilla) tip += CText(color, item.tileBoost > 0 ? "+" : "", item.tileBoost, "#; range");
				if (style == ETipStyle.TwoCols) tip += new string[] { "Range:", CText(color, item.tileBoost > 0 ? "+" : "", item.tileBoost) };
			}
		}
	}
}