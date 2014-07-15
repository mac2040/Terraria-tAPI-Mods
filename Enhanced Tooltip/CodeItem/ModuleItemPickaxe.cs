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
	public class ModuleItemPickaxe : Module<Item>
	{
		public override void ModifyTip(ETipStyle style, OptionList options, STooltip tip, Item item)
		{
			if (item.pick > 0)
			{
				Color color = Color.White;
				switch ((string)options["itemToolPowerColor"].Value)
				{
					case "Green": color = Color.Lime; break;
					case "Power": float f = 1f * item.pick / MBase.me.maxPowerPick; color = DoubleLerp(Color.Red, Color.Yellow, Color.Lime, f); break;
					default: break;
				}

				if (style == ETipStyle.Vanilla) tip += new STooltip.Line(CText(color, item.pick, "%#; pickaxe power"));
				if (style == ETipStyle.TwoCols) tip += new STooltip.Line("Pickaxe power:", CText(color, item.pick, "%"));
			}
		}
	}
}