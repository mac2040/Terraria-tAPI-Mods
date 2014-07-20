using Microsoft.Xna.Framework.Graphics;
using Shockah.Base;
using System.Collections.Generic;
using TAPI;
using Terraria;

namespace Shockah.ETooltip
{
	public class MInterface : ModInterface
	{
		public static InterfaceLayer LayerPreTooltip = null, LayerPreMouseOver = null, LayerPreBuffs = null;
		
		public MInterface(ModBase modBase) : base(modBase) { }

		public override void ModifyInterfaceLayerList(List<InterfaceLayer> list)
		{
			if (LayerPreTooltip == null)
			{
				LayerPreTooltip = new ILPreTooltip();
				LayerPreMouseOver = new ILPreMouseOver();
				LayerPreBuffs = new ILPreBuffs();
			}

			list.Insert(list.IndexOf(InterfaceLayer.LayerMouseText), LayerPreTooltip);
			list.Insert(list.IndexOf(InterfaceLayer.LayerMouseOver), LayerPreMouseOver);
			list.Insert(list.IndexOf(InterfaceLayer.LayerBuffs), LayerPreBuffs);

			LayerPreTooltip.visible = true;
			LayerPreMouseOver.visible = true;
			LayerPreBuffs.visible = true;

			InterfaceLayer.LayerMouseText.visible = false;
			InterfaceLayer.LayerMouseOver.visible = false;
			InterfaceLayer.LayerBuffs.visible = false;
		}
	}
}