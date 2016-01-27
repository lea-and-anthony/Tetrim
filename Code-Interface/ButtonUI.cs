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

		public Utils.ColorShade StrokeShade = Utils.ColorShade.Dark;
		public Utils.ColorShade FillShade = Utils.ColorShade.Normal;
		public Color StrokeColor = new Color(100, 100, 100, 255);
		public Color FillColor = new Color(255, 255, 255, 255);
		public bool IsTextStroked = true;
		public bool IsSquared = false;
		public Typeface Typeface = UtilsUI.TextFont;
		public GravityFlags Gravity = GravityFlags.Center;
		public ButtonShape Shape = ButtonShape.RoundedRectangle;
		public int Padding = 0;
		public int _textSize = 30;
		private int _strokeBorderWidth = 15;
		private int _strokeTextWidth = 7;
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
			StrokeBorderWidth = StrokeBorderWidth;
			StrokeTextWidth = StrokeTextWidth;
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
			_textSize = buttonUI._textSize;
			_strokeBorderWidth = buttonUI._strokeBorderWidth;
			_strokeTextWidth = buttonUI._strokeTextWidth;
			_radiusIn = buttonUI._radiusIn;
			_radiusOut = buttonUI._radiusOut;
		}

		public ButtonUI Clone()
		{
			return new ButtonUI(this);
		}

		public void SetTextSize(ComplexUnitType unit, int size)
		{
			_textSize = Utils.ConvertTextSize(_context, unit, size);
		}

		//--------------------------------------------------------------
		// PROPERTIES
		//--------------------------------------------------------------
		public int TextSize
		{
			get
			{
				return _textSize;
			}
		}

		public int StrokeBorderWidth
		{
			get
			{
				return _strokeBorderWidth;
			}
			set
			{
				_strokeBorderWidth = Utils.GetPixelsFromDP(_context, value);
			}
		}

		public int StrokeTextWidth
		{
			get
			{
				return _strokeTextWidth;
			}
			set
			{
				_strokeTextWidth = Utils.GetPixelsFromDP(_context, value);
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

