using System;
using System.Collections.Generic;

using Android.Content;
using Android.Graphics;
using Android.Views;

namespace Tetrim
{
	public class DialogBuilder
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		public enum DialogContentType
		{
			TextView,
			EditText,
			None
		}

		public enum DialogRequestCode
		{
			PosOrNeg,
			Text
		}

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private Context _context;

		public EventHandler PositiveAction, NegativeAction;
		public string Title, Message;
		public string PositiveText, NegativeText;
		public static string ReturnText;
		public List<View> Content = new List<View>();

		public Color StrokeColor = Utils.getAndroidDarkColor(TetrisColor.Cyan);
		public Color FillColor = Utils.getAndroidLightColor(TetrisColor.Cyan);
		public Color TextColor = Utils.getAndroidReallyDarkColor(TetrisColor.Cyan);

		public DialogContentType ContentType = DialogContentType.TextView;
		public DialogRequestCode RequestCode = DialogRequestCode.PosOrNeg;

		private int _strokeBorderWidth = 15;
		private int _radiusIn = 10;
		private int _radiusOut = 7;

		//--------------------------------------------------------------
		// PROPERTIES
		//--------------------------------------------------------------
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

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public DialogBuilder (Context context)
		{
			_context = context;

			// Convert in pixels
			StrokeBorderWidth = StrokeBorderWidth;
			RadiusIn = RadiusIn;
			RadiusOut = RadiusOut;
		}
	}
}

