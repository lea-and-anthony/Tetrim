using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace Tetrim
{
	public class ButtonUI
	{
		//--------------------------------------------------------------
		// TYPES
		//--------------------------------------------------------------
		public enum ButtonShape
		{
			RoundedRectangle,
			Rectangle,
			LeftRight,
			BottomTop,
		};

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private static Context _context;

		// Shades
		public Utils.ColorShade StrokeShade = Utils.ColorShade.Dark;
		public Utils.ColorShade FillShade = Utils.ColorShade.Normal;

		// Colors
		public Color StrokeColor = new Color(100, 100, 100, 255);
		public Color FillColor = new Color(255, 255, 255, 255);

		// Booleans
		public bool IsTextStroked = true;
		public bool IsSquared = false;

		// General apprearance
		public GravityFlags Gravity = GravityFlags.Center;
		public ButtonShape Shape = ButtonShape.RoundedRectangle;
		public int Padding = 0;

		// Font
		public Typeface Typeface = UtilsUI.TextFont;
		public float TextSizeRatio = 0.5f;
		private float _textSize = 0;

		// Stroke
		public float StrokeBorderWidthRatio = 0.2f;
		public float StrokeTextWidthRatio = 0.1f;
		public float StrokeBorderWidth = 15f;
		public float StrokeTextWidth = 7f;

		// Corner
		private int _radiusIn = 10;
		private int _radiusOut = 7;

		//--------------------------------------------------------------
		// METHODS
		//--------------------------------------------------------------
		public static void GiveContext(Context context)
		{
			_context = context;
		}

		public ButtonUI()
		{
			SetTextSize(ComplexUnitType.Dip, TextSize);
			StrokeBorderWidth = Utils.GetPixelsFromDP(_context, StrokeBorderWidth);
			StrokeTextWidth = Utils.GetPixelsFromDP(_context, StrokeTextWidth);
			RadiusIn = RadiusIn;
			RadiusOut = RadiusOut;
		}

		public ButtonUI(ButtonUI buttonUI)
		{
			StrokeShade = buttonUI.StrokeShade;
			FillShade = buttonUI.FillShade;
			StrokeColor = buttonUI.StrokeColor;
			FillColor = buttonUI.FillColor;
			IsTextStroked = buttonUI.IsTextStroked;
			IsSquared = buttonUI.IsSquared;
			Typeface = buttonUI.Typeface;
			Gravity = buttonUI.Gravity;
			Shape = buttonUI.Shape;
			Padding = buttonUI.Padding;
			TextSizeRatio = buttonUI.TextSizeRatio;
			StrokeBorderWidthRatio = buttonUI.StrokeBorderWidthRatio;
			StrokeTextWidthRatio = buttonUI.StrokeTextWidthRatio;
			StrokeBorderWidth = buttonUI.StrokeBorderWidth;
			StrokeTextWidth = buttonUI.StrokeTextWidth;
			_textSize = buttonUI._textSize;
			_radiusIn = buttonUI._radiusIn;
			_radiusOut = buttonUI._radiusOut;
		}

		public ButtonUI Clone()
		{
			return new ButtonUI(this);
		}

		public void SetTextSize(ComplexUnitType unit, float size)
		{
			_textSize = Utils.ConvertTextSize(_context, unit, size);
		}

		public void SetTextSize(ComplexUnitType unit, int size)
		{
			SetTextSize(unit, (float)size);
		}

		//--------------------------------------------------------------
		// PROPERTIES
		//--------------------------------------------------------------
		public float TextSize
		{
			get
			{
				return _textSize;
			}
		}

		public int RadiusIn
		{
			get
			{
				return _radiusIn;
			}
			set
			{
				_radiusIn = Utils.GetPixelsFromDP(_context, value);
			}
		}

		public int RadiusOut
		{
			get
			{
				return _radiusOut;
			}
			set
			{
				_radiusOut = Utils.GetPixelsFromDP(_context, value);
			}
		}
	};
}

