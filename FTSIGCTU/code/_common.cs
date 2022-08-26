using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU;

public static class common
{
	//
	public static bool drawThickHexes = false;
	//---------------------------------------------------//
	public static void playSound(Sound SOUND, float VOLUME)
	{
		class_158.method_376(SOUND.field_4061, class_269.field_2109 * VOLUME, false);
	}
	//---------------------------------------------------//
	//drawing helpers
	public static HexIndex getHexFromPoint(Vector2 point, SolutionEditorScreen SES)
	{
		return class_187.field_1742.method_493(point, SES.field_4009);
	}
	public static Vector2 getPointFromHex(HexIndex hex, SolutionEditorScreen SES)
	{
		return class_187.field_1742.method_491(hex, SES.field_4009);
	}
	//---------------------------------------------------//
	//input helpers
	public static bool withinBoardEditingBounds(Vector2 point, SolutionEditorScreen SES)
	{
		var SES_dyn = new DynamicData(SES);
		var sepp = SES_dyn.Get<SolutionEditorPartsPanel>("field_4001");
		Vector2 bottomLeft = new Vector2(310f, 268f) + new Vector2(class_162.method_417(0.0f, -360f, new DynamicData(sepp).Get<float>("field_3966")), 0.0f);
		Vector2 resolution = class_115.field_1433;
		Bounds2 fullBounds = Bounds2.WithCorners(bottomLeft, resolution);
		Bounds2 exitBounds = Bounds2.WithSize(resolution + new Vector2(-70f, -75f), new Vector2(70f, 75f));

		return fullBounds.Contains(point) && !exitBounds.Contains(point);
	}
	//---------------------------------------------------//
	//part-editing helpers
	public static HexIndex getPartOrigin(Part part)
	{
		return part.method_1161();
	}
	public static void setPartOrigin(Part part, HexIndex hex)
	{
		//easier than trying to invoke method_1162, which is private
		new DynamicData(part).Set("field_2692", hex);
	}
	public static void addUndoHistoryCheckpoint(SolutionEditorScreen SES_self)
	{
		SES_self.method_502().method_1961();
	}
	//---------------------------------------------------//
	public static void ApplySettings(bool _drawThickHexes)
	{
		drawThickHexes = _drawThickHexes;
	}
}