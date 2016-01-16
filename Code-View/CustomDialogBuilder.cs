﻿using System;

using Android.Content;
using Android.Graphics;

namespace Tetrim
{
	public class CustomDialogBuilder
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		public enum DialogContentType
		{
			TextView,
			EditText
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
		public string ReturnText;

		public Color StrokeColor = Utils.getAndroidDarkColor(TetrisColor.Blue);
		public Color FillColor = new Color(0, 0, 50, 255);

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
		public CustomDialogBuilder (Context context)
		{
			_context = context;

			// Convert in pixels
			StrokeBorderWidth = StrokeBorderWidth;
			RadiusIn = RadiusIn;
			RadiusOut = RadiusOut;
		}
	}
}

