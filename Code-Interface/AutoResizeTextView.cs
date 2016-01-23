using System;

using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Widget;

namespace Tetrim
{
	/// <summary>
	/// TextView that automatically resizes it's content to fit the layout dimensions
	/// </summary>
	/// <remarks>Port of: http://ankri.de/autoscale-textview/</remarks>
	public class AutoResizeTextView : TextView
	{
		/// <summary>
		/// How close we have to be to the perfect size
		/// </summary>
		private const float Threshold = .5f;

		/// <summary>
		/// Default minimum text size
		/// </summary>
		private const float DefaultMinTextSize = 10f;

		private Paint _textPaint;
		private float _preferredTextSize;

		public AutoResizeTextView(Context context) : this(context, null) { }

		public AutoResizeTextView(Context context, IAttributeSet attrs) : this(context, attrs, Android.Resource.Attribute.TextViewStyle) { }

		public AutoResizeTextView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
		{
			Initialize(context, attrs);
		}

		// Default constructor override for MonoDroid
		public AutoResizeTextView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
			Initialize(null, null);
		}

		private void Initialize(Context context, IAttributeSet attrs)
		{
			_textPaint = new Paint();

			if (context != null && attrs != null)
			{
				/*var attributes = context.ObtainStyledAttributes(attrs, Resource.Styleable.AutoResizeTextView);
				MinTextSize = attributes.GetDimension(Resource.Styleable.AutoResizeTextView_minTextSize, DefaultMinTextSize);
				attributes.Recycle();*/
				MinTextSize = 0;

				_preferredTextSize = TextSize;
			}
		}

		/// <summary>
		/// Minimum text size in actual pixels
		/// </summary>
		public float MinTextSize { get; set; }

		/// <summary>
		/// Resize the text so that it fits.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="textWidth">Width of the TextView</param>
		/// <param name="font">Font of the TextView</param>
		protected virtual void RefitText(string text, int textWidth, Typeface font)
		{
			if (textWidth <= 0 || string.IsNullOrWhiteSpace(text))
				return;

			int targetWidth = textWidth - PaddingLeft - PaddingRight;
			_textPaint.Set(this.Paint);
			_textPaint.SetTypeface(font);

			if(font == UtilsUI.TextFont)
				_textPaint.SetTypeface(UtilsUI.TextFont);

			while ((_preferredTextSize - MinTextSize) > Threshold)
			{
				float size = (_preferredTextSize + MinTextSize) / 2f;
				_textPaint.TextSize = size;

				if (_textPaint.MeasureText(text) >= targetWidth)
				{
					_preferredTextSize = size; // Too big
				}
				else
				{
					MinTextSize = size; // Too small
				}
			}

			SetTextSize(ComplexUnitType.Px, MinTextSize);
		}

		protected override void OnTextChanged(Java.Lang.ICharSequence text, int start, int before, int after)
		{
			base.OnTextChanged(text, start, before, after);

			RefitText(text.ToString(), Width, Typeface);
		}

		protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
		{
			base.OnSizeChanged(w, h, oldw, oldh);

			if (w != oldw)
				RefitText(Text, Width, Typeface);
		}

		public override void SetTypeface (Typeface tf, TypefaceStyle style)
		{
			base.SetTypeface (tf, style);
			RefitText(Text, Width, tf);
			SetWillNotDraw(false);
			PostInvalidate();
		}
	}
}