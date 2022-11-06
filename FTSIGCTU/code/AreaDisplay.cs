using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU;
using Texture = class_256;

public static class AreaDisplay
{
	//data structs, enums, variables
	private static Texture[] textures;
	private static Sound sound_toggle;

	private static SDL.enum_160 heightKey = SDL.enum_160.SDLK_u;
	private static SDL.enum_160 widthKey = SDL.enum_160.SDLK_i;
	private static bool showHeight = false;
	private static bool showWidth = false;

	private enum resource : byte
	{
		blank,
		heightUpper0,
		heightLower0,
		heightUpper1,
		heightLower1,
		heightUpper2,
		heightLower2,
		COUNT,
	}

	//---------------------------------------------------//
	//internal helper methods
	private static IEnumerable<HexIndex> getAreaHexes(SolutionEditorScreen ses) => ses.method_507().method_484();
	private static IEnumerable<HexIndex> getFootprint(SolutionEditorScreen ses)
	{
		HashSet<HexIndex> hexIndexSet = new HashSet<HexIndex>();
		foreach (Part part in ses.method_502().method_1937()) hexIndexSet.UnionWith(part.method_1186(ses.method_502()));
		return hexIndexSet;
	}

	private static void drawHex(SolutionEditorScreen ses, HexIndex hex, Texture tex, float alpha = 1)
	{
		Vector2 vec2 = class_187.field_1742.method_491(hex, ses.field_4009) - tex.field_2056.ToVector2() / 2;
		int reps = common.drawThickHexes ? 2 : 1;
		for (int i = 0; i < reps; i++)
		{
			class_135.method_271(tex, Color.White.WithAlpha(alpha), vec2.Rounded());
		}
	}

	//---------------------------------------------------//
	//internal main methods

	private static void displayHeightAndWidth(SolutionEditorScreen ses, HashSet<HexIndex> hexes)
	{
		if (hexes.Count == 0) return;

		//first, compute the height and width
		if (showHeight)
		{
			int minH = int.MaxValue;
			int maxH = int.MinValue;

			//compute min height
			int[] h = new int[6] { minH, maxH, minH, maxH, minH, maxH };
			int[] uv = new int[6] { minH, maxH, minH, maxH, minH, maxH };
			foreach (var hex in hexes)
			{
				h[0] = Math.Min(h[0], hex.R);
				h[1] = Math.Max(h[1], hex.R);
				h[2] = Math.Min(h[2], hex.Q);
				h[3] = Math.Max(h[3], hex.Q);
				h[4] = Math.Min(h[4], hex.Q + hex.R);
				h[5] = Math.Max(h[5], hex.Q + hex.R);

				uv[0] = Math.Min(uv[0], 2*hex.Q + hex.R);
				uv[1] = Math.Max(uv[1], 2*hex.Q + hex.R);
				uv[2] = Math.Min(uv[2], hex.Q + 2*hex.R);
				uv[3] = Math.Max(uv[3], hex.Q + 2*hex.R);
				uv[4] = Math.Min(uv[4], hex.Q - hex.R);
				uv[5] = Math.Max(uv[5], hex.Q - hex.R);
			}
			int valH = Math.Min(h[1]-h[0],Math.Min(h[3] - h[2], h[5] - h[4]));

			//then display the height borders
			int x0, x1, x2, x3, y0, y1, y2, y3, w;
			if (valH == h[1] - h[0])
			{
				//solution runs in the usual direction (y=k) direction
				y0 = h[0] - 1;
				y1 = h[1] + 1;

				x0 = (int) Math.Floor(0.5 * (uv[0] - y0));
				x1 = (int) Math.Floor(0.5 * (uv[0] - y1));
				x2 = (int) Math.Ceiling(0.5 * (uv[1] - y0));
				x3 = (int) Math.Ceiling(0.5 * (uv[1] - y1));

				w = Math.Max(x2 - x0, x3 - x1);

				displayHeight(ses, new HexIndex(x0, y0), new HexIndex(x1, y1), new HexIndex(1, 0), w);
			}
			else if (valH == h[3] - h[2])
			{
				//solutions runs up to the right, in the (x=k) direction
				x0 = h[2] - 1;
				x1 = h[3] + 1;

				y0 = (int) Math.Floor((uv[2] - x0) / 2.0);
				y1 = (int) Math.Floor((uv[2] - x1) / 2.0);
				y2 = (int) Math.Ceiling((uv[3] - x0) / 2.0);
				y3 = (int) Math.Ceiling((uv[3] - x1) / 2.0);

				w = Math.Max(y2 - y0, y3 - y1);

				displayHeight(ses, new HexIndex(x1, y1), new HexIndex(x0, y0), new HexIndex(0, 1), w);
			}
			else // (H == h[5] - h[4])
			{
				//solutions runs down to the right, in the (x+y=k) direction
				int z0 = h[4] - 1;
				int z1 = h[5] + 1;

				x0 = (int)Math.Floor((z0 + uv[4]) / 2.0);
				x1 = (int)Math.Floor((z1 + uv[4]) / 2.0);
				x2 = (int)Math.Ceiling((z0 + uv[5]) / 2.0);
				x3 = (int)Math.Ceiling((z1 + uv[5]) / 2.0);

				y0 = z0-x0;
				y1 = z1-x1;
				//y2 = z0-x2;
				//y3 = z1-x3;

				w = Math.Max(x2 - x0, x3 - x1); // fun fact: x2 - x0 == y0 - y2 and x3 - x1 == y1 - y3

				displayHeight(ses, new HexIndex(x0, y0), new HexIndex(x1, y1), new HexIndex(1, -1), w);
			}

		}
	}

	private static void displayHeight(SolutionEditorScreen ses, HexIndex lowerLeft, HexIndex upperLeft, HexIndex step, int width)
	{
		Texture upperTex = textures[(int)resource.heightUpper2];
		Texture lowerTex = textures[(int)resource.heightLower2];

		//choose texture
		if (step == new HexIndex(1, 0))
		{
			upperTex = textures[(int)resource.heightUpper0];
			lowerTex = textures[(int)resource.heightLower0];
		}
		else if (step == new HexIndex(0, 1))
		{
			upperTex = textures[(int)resource.heightUpper1];
			lowerTex = textures[(int)resource.heightLower1];
		}

		//prep for-loop
		int c = 3;
		for (int i = 0; i < c; i++)
		{
			upperLeft -= step;
			lowerLeft -= step;
		}
		//draw hexes
		for (int i = 0; i <= width + 2 * c; i++)
		{
			//draw hexes
			drawHex(ses, lowerLeft, lowerTex);
			drawHex(ses, upperLeft, upperTex);

			//update locations
			upperLeft += step;
			lowerLeft += step;
		}
	}

	//---------------------------------------------------//

	public static void c153_method_221(class_153 c153_self)
	{
		var c153_dyn = new DynamicData(c153_self);
		var ses = c153_dyn.Get<SolutionEditorScreen>("field_2007");

		bool simStopped = ses.method_503() == enum_128.Stopped;

		if (Input.IsSdlKeyPressed(heightKey))
		{
			showHeight = !showHeight;
			common.playSound(sound_toggle, 0.2f);
		}
		if (Input.IsSdlKeyPressed(widthKey))
		{
			showWidth = !showWidth;
			common.playSound(sound_toggle, 0.2f);
		}


		HashSet<HexIndex> hexes = new();
		if (!simStopped)
		{
			hexes.UnionWith(getAreaHexes(ses));
		}
		hexes.UnionWith(getFootprint(ses));

		displayHeightAndWidth(ses, hexes);







	}

	public static void LoadPuzzleContent()
	{
		//load textures and sounds

		sound_toggle = class_238.field_1991.field_1821; // click_button

		textures = new Texture[(int)resource.COUNT];

		string path = "ftsigctu/textures/board/areaHex/";
		textures[(int)resource.blank] = class_235.method_615(path + "blank");
		textures[(int)resource.heightLower0] = class_235.method_615(path + "height_lower0");
		textures[(int)resource.heightUpper0] = class_235.method_615(path + "height_upper0");
		textures[(int)resource.heightLower1] = class_235.method_615(path + "height_lower1");
		textures[(int)resource.heightUpper1] = class_235.method_615(path + "height_upper1");
		textures[(int)resource.heightLower2] = class_235.method_615(path + "height_lower2");
		textures[(int)resource.heightUpper2] = class_235.method_615(path + "height_upper2");
	}

	//---------------------------------------------------//
}