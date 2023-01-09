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
		heightUpper60,
		heightLower60,
		heightUpper300,
		heightLower300,
		widthLefter0,
		widthRighter0,
		widthLefter60,
		widthRighter60,
		widthLefter300,
		widthRighter300,
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

	private static int heightCoordinate(HexIndex hex, int direction)
	{
		switch (direction)
		{
			default:return -hex.Q;			// 60 degree
			case 1: return hex.R;			// 0 degree
			case 2: return hex.Q + hex.R;	// -60 degree
		}
	}
	private static int widthCoordinate(HexIndex hex, int direction)
	{
		switch (direction)
		{
			default:return 2 * hex.R + hex.Q;	// 150 degree
			case 1: return 2 * hex.Q + hex.R;	// 90 degree
			case 2: return hex.Q - hex.R;		// 30 degree
		}
	}

	//---------------------------------------------------//
	//internal main methods
	private static void displayHeightWidth(SolutionEditorScreen ses, HashSet<HexIndex> hexes)
	{
		if (hexes.Count == 0) return;
		//compute heights and widths in the different directions
		int MAX = int.MaxValue;
		int MIN = int.MinValue;
		int[] minH = new int[3] { MAX, MAX, MAX };
		int[] minW = new int[3] { MAX, MAX, MAX };
		int[] maxH = new int[3] { MIN, MIN, MIN };
		int[] maxW = new int[3] { MIN, MIN, MIN };
		int valH = MAX;
		int valW = MAX;
		HexRotation[] directions = new HexRotation[3] { HexRotation.R0 , HexRotation.R60, HexRotation.R300 };
		int dirH = 0;
		int dirW = 0;

		for (int dir = 0; dir < 3; dir++)
		{
			foreach (var hexIndex in hexes)
			{
				// rotate hex into the normal workspace
				// (we'll rotate back into realspace before drawing)
				var hex = hexIndex.Rotated(directions[dir].Negative());
				int h = hex.R;
				int w = 2 * hex.Q + hex.R;
				minH[dir] = Math.Min(minH[dir], h);
				maxH[dir] = Math.Max(maxH[dir], h);
				minW[dir] = Math.Min(minW[dir], w);
				maxW[dir] = Math.Max(maxW[dir], w);
			}
			int H = maxH[dir] - minH[dir];
			if (H < valH)
			{
				valH = H;
				dirH = dir;
			}
			int W = maxW[dir] - minW[dir];
			if (W < valW)
			{
				valW = W;
				dirW = dir;
			}
		}

		if (showHeight)
		{
			int q0, q1, q2, q3, r0, r1, w;
			r0 = minH[dirH] - 1;
			r1 = maxH[dirH] + 1;

			q0 = (int)Math.Floor(0.5 * (minW[dirH] - r0));
			q1 = (int)Math.Floor(0.5 * (minW[dirH] - r1));
			q2 = (int)Math.Ceiling(0.5 * (maxW[dirH] - r0));
			q3 = (int)Math.Ceiling(0.5 * (maxW[dirH] - r1));
			w = Math.Max(q2 - q0, q3 - q1);

			displayHeight(ses, new HexIndex(q0, r0), new HexIndex(q1, r1), w, directions[dirH]);
		}

		if (showWidth)
		{
			int q0, q1, r0, r1, h;
			bool leftInnie, rightOutie;
			r0 = minH[dirW];
			r1 = maxH[dirW];
			h = r1 - r0 + 1;

			q0 = minW[dirW] - r0;
			q1 = maxW[dirW] - r0;
			leftInnie = (q0 % 2 == 0);
			rightOutie = (q1 % 2 != 0);
			q0 = (int)Math.Floor(0.5 * (q0 - 1));
			q1 = (int)Math.Ceiling(0.5 * (q1 + 1));

			displayWidth(ses, new HexIndex(q0, r0), new HexIndex(q1, r0), h, leftInnie, rightOutie, directions[dirW]);
		}
	}

	private static void displayHeight(SolutionEditorScreen ses, HexIndex lowerLeft, HexIndex upperLeft, int width, HexRotation direction)
	{
		//rotate from workspace to realspace
		lowerLeft = lowerLeft.Rotated(direction);
		upperLeft = upperLeft.Rotated(direction);
		HexIndex step = new HexIndex(1, 0).Rotated(direction);

		//get textures
		Texture upperTex = textures[(int)resource.heightUpper0];
		Texture lowerTex = textures[(int)resource.heightLower0];
		if (direction.GetNumberOfTurns() == HexRotation.R60.GetNumberOfTurns())
		{
			upperTex = textures[(int)resource.heightUpper60];
			lowerTex = textures[(int)resource.heightLower60];
		} else if (direction.GetNumberOfTurns() == HexRotation.R300.GetNumberOfTurns())
		{
			upperTex = textures[(int)resource.heightUpper300];
			lowerTex = textures[(int)resource.heightLower300];
		}

		//prep for loop
		int c = 2;
		for (int i = 0; i < c; i++)
		{
			upperLeft -= step;
			lowerLeft -= step;
			width += 2;
		}
		//draw hexes
		for (int i = 0; i <= width; i++)
		{
			drawHex(ses, lowerLeft, lowerTex);
			drawHex(ses, upperLeft, upperTex);

			//update locations
			upperLeft += step;
			lowerLeft += step;
		}
	}
	private static void displayWidth(SolutionEditorScreen ses, HexIndex lowerLeft, HexIndex lowerRight, int height, bool leftStartsInnie, bool rightStartsOutie, HexRotation direction)
	{
		//rotate from workspace to realspace
		lowerLeft = lowerLeft.Rotated(direction);
		lowerRight = lowerRight.Rotated(direction);
		bool leftState = leftStartsInnie;
		bool rightState = rightStartsOutie;
		HexIndex[] step = new HexIndex[2] { new HexIndex(0, 1).Rotated(direction), new HexIndex(-1, 1).Rotated(direction) };

		//get textures
		Texture lefterTex = textures[(int)resource.widthLefter0];
		Texture righterTex = textures[(int)resource.widthRighter0];
		if (direction.GetNumberOfTurns() == HexRotation.R60.GetNumberOfTurns())
		{
			lefterTex = textures[(int)resource.widthLefter60];
			righterTex = textures[(int)resource.widthRighter60];
		}
		else if (direction.GetNumberOfTurns() == HexRotation.R300.GetNumberOfTurns())
		{
			lefterTex = textures[(int)resource.widthLefter300];
			righterTex = textures[(int)resource.widthRighter300];
		}

		//prep for loop
		int c = 1;
		for (int i = 0; i < c; i++)
		{
			lowerLeft -= step[0] + step[1];
			lowerRight -= step[0] + step[1];
			height += 4;
		}
		//draw hexes
		for (int i = 0; i < height; i++)
		{
			drawHex(ses, lowerLeft, leftState ? lefterTex : righterTex);
			drawHex(ses, lowerRight, rightState ? lefterTex : righterTex);

			//update state
			lowerLeft += step[leftState ? 0 : 1];
			lowerRight += step[rightState ? 0 : 1];
			leftState = !leftState;
			rightState = !rightState;
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

		if (showHeight || showWidth) displayHeightWidth(ses, hexes);





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
		textures[(int)resource.heightLower60] = class_235.method_615(path + "height_lower60");
		textures[(int)resource.heightUpper60] = class_235.method_615(path + "height_upper60");
		textures[(int)resource.heightLower300] = class_235.method_615(path + "height_lower300");
		textures[(int)resource.heightUpper300] = class_235.method_615(path + "height_upper300");
		textures[(int)resource.widthLefter0] = class_235.method_615(path + "width_lefter0");
		textures[(int)resource.widthRighter0] = class_235.method_615(path + "width_righter0");
		textures[(int)resource.widthLefter60] = class_235.method_615(path + "width_lefter60");
		textures[(int)resource.widthRighter60] = class_235.method_615(path + "width_righter60");
		textures[(int)resource.widthLefter300] = class_235.method_615(path + "width_lefter300");
		textures[(int)resource.widthRighter300] = class_235.method_615(path + "width_righter300");
	}

	//---------------------------------------------------//
}