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
	public class InterfaceFCMMisc : InterfaceFCMBase
	{
		public const int POS_X = 20, POS_Y = 306;
		public static readonly string[] MOON_PHASES = new string[] { "Full Moon", "Waning Gibbous", "Third Quarter", "Waning Crescent", "New Moon", "Waxing Crescent", "First Quarter", "Waxing Gibbous" };

		public static InterfaceFCMMisc me = null;

		public static void Reset()
		{
			
		}

		public static void SetAbsoluteTime(int time)
		{
			time += 27000;
			time = FixAbsoluteTime(time);
			Main.time = time;
			Main.dayTime = time < 54000;
			if (time >= 54000) Main.time -= 54000;
		}
		public static int GetAbsoluteTime()
		{
			int ret = (int)Main.time;
			if (!Main.dayTime) ret += 54000;
			ret -= 27000;
			return FixAbsoluteTime(ret);
		}
		public static int FixAbsoluteTime(int time)
		{
			while (time < 0) time += 86400;
			while (time >= 86400) time -= 86400;
			return time;
		}
		public static int[] GetTime(int time = -1)
		{
			if (time < 0) time = GetAbsoluteTime();
			time = FixAbsoluteTime(time);

			int h = 0, m = 0, s = 0;
			s = time % 60; time /= 60;
			m = time % 60; time /= 60;
			h = time % 24;

			return new int[] { h, m, s };
		}
		public static string GetTimeText(bool system24h, int time = -1)
		{
			int[] l = GetTime(time);
			if (system24h)
			{
				string ret = "", s;

				s = "" + l[0];
				while (s.Length < 2) s = "0" + s;
				ret += s;

				s = "" + l[1];
				while (s.Length < 2) s = "0" + s;
				ret += ":" + s;

				return ret;
			}
			else
			{
				string ret = "", s;
				bool am = false;

				if (l[0] >= 12 && l[0] < 24) am = true;
				l[0] %= 12;
				if (l[0] == 0) l[0] = 12;

				s = "" + l[0];
				ret += s;

				s = "" + l[1];
				while (s.Length < 2) s = "0" + s;
				ret += ":" + s;

				ret += " " + (am ? "AM" : "PM");

				return ret;
			}
		}

		protected string dragging = null;

		public InterfaceFCMMisc()
		{
			me = this;
		}

		public override void OnOpen()
		{
			base.OnOpen();
		}

		public override void Draw(InterfaceLayer layer, SpriteBatch sb)
		{
			if (dragging != null) Main.localPlayer.mouseInterface = true;
			base.Draw(layer, sb);

			Action<string, string, Vector2, Texture2D, float, Func<float, string>, Action<float>> drawSliderFloat = (name, tip, pos, sliderTex, ratio, textBuilder, codeSet) =>
			{
				sb.Draw(sliderTex, new Vector2(pos.X, pos.Y + 20), Color.White);
				sb.Draw(API.main.colorSliderTexture, new Vector2(pos.X + 4 + (sliderTex.Width - 8) * ratio, pos.Y + 20 + sliderTex.Height / 2), null, Color.White, 0f, API.main.colorSliderTexture.Size() * .5f, 1f, SpriteEffects.None, 0f);
				SDrawing.StringShadowed(sb, Main.fontMouseText, tip, pos, Color.White, .8f);
				SDrawing.StringShadowed(sb, Main.fontMouseText, textBuilder(ratio), new Vector2((float)Math.Round(pos.X + sliderTex.Width - Main.fontMouseText.MeasureString(textBuilder(ratio)).X * .8f), pos.Y), Color.White, .8f);
				if (dragging == name || (dragging == null && Math2.InRegion(Main.mouse, new Vector2(pos.X, pos.Y + 20), sliderTex.Width, sliderTex.Height)))
				{
					Main.localPlayer.mouseInterface = true;
					if (Main.mouseLeft)
					{
						dragging = name;
						int deltax = Main.mouseX - ((int)pos.X + 4);
						ratio = 1f * deltax / (sliderTex.Width - 8);
						ratio = Math.Min(Math.Max(ratio, 0f), 1f);
						codeSet(ratio);
					}
					else dragging = null;
				}
			};
			Action<string, string, Vector2, Texture2D, int, int, int, Func<int, string>, Action<int>> drawSliderInt = (name, tip, pos, sliderTex, value, vmin, vmax, textBuilder, codeSet) =>
			{
				float ratio = 1f * (value - vmin) / (vmax - vmin);
				sb.Draw(sliderTex, new Vector2(pos.X, pos.Y + 20), Color.White);
				sb.Draw(API.main.colorSliderTexture, new Vector2(pos.X + 4 + (sliderTex.Width - 8) * ratio, pos.Y + 20 + sliderTex.Height / 2), null, Color.White, 0f, API.main.colorSliderTexture.Size() * .5f, 1f, SpriteEffects.None, 0f);
				SDrawing.StringShadowed(sb, Main.fontMouseText, tip, pos, Color.White, .8f);
				SDrawing.StringShadowed(sb, Main.fontMouseText, textBuilder(value), new Vector2((float)Math.Round(pos.X + sliderTex.Width - Main.fontMouseText.MeasureString(textBuilder(value)).X * .8f), pos.Y), Color.White, .8f);
				if (dragging == name || (dragging == null && Math2.InRegion(Main.mouse, new Vector2(pos.X, pos.Y + 20), sliderTex.Width, sliderTex.Height)))
				{
					Main.localPlayer.mouseInterface = true;
					if (Main.mouseLeft)
					{
						dragging = name;
						int deltax = Main.mouseX - ((int)pos.X + 4);
						ratio = 1f * deltax / (sliderTex.Width - 8);
						ratio = Math.Min(Math.Max(ratio, 0f), 1f);
						codeSet((int)Math.Round(vmin + (vmax - vmin) * ratio));
					}
					else dragging = null;
				}
			};

			drawSliderFloat("TimeSlider", "Current time", new Vector2(POS_X, POS_Y), MBase.me.textures["Images/TimeSlider.png"], 1f * FixAbsoluteTime(GetAbsoluteTime() - 43200) / 86400,
			(ratio) => { return GetTimeText(false, GetAbsoluteTime()); },
			(ratio) => { SetAbsoluteTime((int)(Math.Min(Math.Max(ratio, 0f), 1f) * 86400) - 43200); });

			drawSliderInt("MoonPhase", "Moon phase", new Vector2(POS_X + 200, POS_Y), API.main.colorBarTexture, Main.moonPhase, 0, 7,
			(value) => { return MOON_PHASES[value]; },
			(value) => { Main.moonPhase = value; });

			drawSliderInt("TimeRateSlider", "Time rate", new Vector2(POS_X, POS_Y + 40), MBase.me.textures["Images/TimeRateSlider.png"], (int)Math.Ceiling(Math.Pow(Main.dayRate, 1f / 1.3545f)), 0, 50,
			(value) => { return "" + (int)Math.Pow(value, 1.3545f); },
			(value) => { Main.dayRate = (int)Math.Pow(value, 1.3545f); });


			drawSliderInt("PlayerLifeMax", "Max life", new Vector2(POS_X, POS_Y + 100), API.main.colorBarTexture, Main.localPlayer.statLifeMax / 20, 1, 25,
			(value) => { return "" + (value * 20); },
			(value) => { Main.localPlayer.statLifeMax = value * 20; });

			drawSliderInt("PlayerLife", "Life", new Vector2(POS_X, POS_Y + 140), API.main.colorBarTexture, Main.localPlayer.statLife, 1, Main.localPlayer.statLifeMax2,
			(value) => { return "" + value; },
			(value) => { Main.localPlayer.statLife = value; });


			drawSliderInt("PlayerManaMax", "Max mana", new Vector2(POS_X + 200, POS_Y + 100), API.main.colorBarTexture, Main.localPlayer.statManaMax / 20, 0, 10,
			(value) => { return "" + (value * 20); },
			(value) => { Main.localPlayer.statManaMax = value * 20; });

			drawSliderInt("PlayerMana", "Mana", new Vector2(POS_X + 200, POS_Y + 140), API.main.colorBarTexture, Main.localPlayer.statMana, 1, Main.localPlayer.statManaMax2,
			(value) => { return "" + value; },
			(value) => { Main.localPlayer.statMana = value; });
		}
	}
}