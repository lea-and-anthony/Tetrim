using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Tetrim
{
	public class ButtonStroked : ImageButton
	{
		//--------------------------------------------------------------
		// CONSTANTS
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
		Context _context;

		public Color StrokeColor = new Color(100, 100, 100, 255);
		public Color FillColor = new Color(255, 255, 255, 255);
		private int _strokeBorderWidth = 15;
		private int _strokeTextWidth = 7;
		private int _radiusIn = 10;
		private int _radiusOut = 7;
		public bool IsTextStroked = true;

		public int TextSize = 30;
		public string Text = "";
		public Typeface Typeface;
		public bool IsSquared = false;
		public GravityFlags Gravity = GravityFlags.Center;
		public ButtonShape Shape = ButtonShape.RoundedRectangle;

		Bitmap _pressedImage = null;
		Bitmap _unpressedImage = null;

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

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public ButtonStroked (Context context, IAttributeSet set) : base(context, set)
		{
			CreateButton(context);
		}

		public ButtonStroked (Context context) : base(context)
		{
			CreateButton(context);
		}

		private void CreateButton(Context context)
		{
			_context = context;
			Typeface = Typeface.CreateFromAsset(context.Assets,"Foo.ttf");
			Text = Tag == null ? Text : Tag.ToString();
			TextSize = Utils.GetPixelsFromDP(_context, TextSize);
			SetScaleType(ScaleType.FitCenter);
			SetAdjustViewBounds(true);

			// Convert in pixels
			StrokeBorderWidth = StrokeBorderWidth;
			StrokeTextWidth = StrokeTextWidth;
			RadiusIn = RadiusIn;
			RadiusOut = RadiusOut;
		}

		public void SetTypeface(Typeface typeface, TypefaceStyle style)
		{
			Typeface = typeface;
		}

		public void SetTextSize(ComplexUnitType unit, int size)
		{
			TextSize = Utils.ConvertTextSize(_context, unit, size);
		}

		protected override void OnSizeChanged (int w, int h, int oldw, int oldh)
		{
			base.OnSizeChanged (w, h, oldw, oldh);
			if((w != 0 && h != 0 && _unpressedImage == null) || (_unpressedImage != null && w != _unpressedImage.Width && h != _unpressedImage.Height))
			{
				InitializeImages();
			}
		}

		protected void InitializeImages()
		{
			_unpressedImage = Bitmap.CreateBitmap(Width, IsSquared ? Width : Height, Bitmap.Config.Argb8888);
			_pressedImage = Bitmap.CreateBitmap(Width, IsSquared ? Width : Height, Bitmap.Config.Argb8888);
			Canvas unpressedCanvas = new Canvas(_unpressedImage);
			Canvas pressedCanvas = new Canvas(_pressedImage);

			TextSize = TextSize == 0 ? Height / 2 : TextSize;

			// Background fill paint
			Paint fillBackPaint = new Paint();
			fillBackPaint.Color = FillColor;
			fillBackPaint.AntiAlias = true;

			// Background stroke paint
			Paint strokeBackPaint = new Paint();
			strokeBackPaint.Color = StrokeColor;
			strokeBackPaint.SetStyle(Paint.Style.Stroke);
			strokeBackPaint.StrokeWidth = this._strokeBorderWidth;
			strokeBackPaint.AntiAlias = true;

			// Text paint
			Paint textPaint = new Paint();
			textPaint.Color = IsTextStroked ? FillColor : StrokeColor;
			textPaint.TextAlign = Paint.Align.Center;
			textPaint.TextSize = TextSize;
			textPaint.SetTypeface(Typeface);
			textPaint.AntiAlias = true;

			// Text stroke paint
			Paint strokePaint = new Paint();
			strokePaint.Color = StrokeColor;
			strokePaint.TextAlign = Paint.Align.Center;
			strokePaint.TextSize = TextSize;
			strokePaint.SetTypeface(Typeface);
			strokePaint.SetStyle(Paint.Style.Stroke);
			strokePaint.StrokeWidth = _strokeTextWidth;
			strokePaint.AntiAlias = true;

			// Background bounds
			Rect local = new Rect();
			this.GetLocalVisibleRect(local);
			RectF bounds = new RectF(local);
			bounds.Top += _strokeBorderWidth/2;
			bounds.Left += _strokeBorderWidth/2;
			bounds.Right -= _strokeBorderWidth/2;
			bounds.Bottom -= _strokeBorderWidth/2;

			while(bounds.Top > Height)
			{
				bounds.Top -= Height;
			}
			while(bounds.Bottom > Height)
			{
				bounds.Bottom -= Height;
			}
			while(bounds.Left > Width)
			{
				bounds.Left -= Width;
			}
			while(bounds.Right > Width)
			{
				bounds.Right -= Width;
			}

			// Text location
			Rect r = new Rect();
			strokePaint.GetTextBounds(Text, 0, Text.Length, r);
			while(r.Width() > Width)
			{
				this.TextSize =(int)(TextSize/1.5);
				textPaint.TextSize = TextSize;
				strokePaint.TextSize = TextSize;
				strokePaint.GetTextBounds(Text, 0, Text.Length, r);
			}

			float x=0, y=0;
			switch (Gravity)
			{
			case GravityFlags.Top:
				y = PaddingTop + r.Height()/2;
				break;
			case GravityFlags.Bottom:
				y = Height - r.Height()/2 - PaddingBottom;
				break;
			default:
				y = Height / 2f + r.Height() / 2f - r.Bottom;
				break;
			}
			switch (Gravity)
			{
			case GravityFlags.Left:
				x = PaddingLeft + r.Width()/2;
				break;
			case GravityFlags.Right:
				x = Width - r.Width()/2 - PaddingRight;
				break;
			default:
				x = Width/2;
				break;
			}

			// Draw unpressed
			DrawBackground(unpressedCanvas, bounds, fillBackPaint, strokeBackPaint);
			if(IsTextStroked)
				unpressedCanvas.DrawText(Text, x, y, strokePaint);
			unpressedCanvas.DrawText(Text, x, y, textPaint);

			// Change colors
			fillBackPaint.Color = StrokeColor;
			strokeBackPaint.Color = FillColor;
			strokePaint.Color = FillColor;
			textPaint.Color = IsTextStroked ? StrokeColor : FillColor;

			// Draw pressed
			DrawBackground(pressedCanvas, bounds, fillBackPaint, strokeBackPaint);
			if(IsTextStroked)
				pressedCanvas.DrawText(Text, x, y, strokePaint);
			pressedCanvas.DrawText(Text, x, y, textPaint);

			// Set images for states
			StateListDrawable states = new StateListDrawable();
			states.AddState(new int[] {Android.Resource.Attribute.StatePressed}, new BitmapDrawable(_pressedImage));
			states.AddState(new int[] {Android.Resource.Attribute.StateFocused}, new BitmapDrawable(_pressedImage));
			states.AddState(new int[] {Android.Resource.Attribute.StateSelected}, new BitmapDrawable(_pressedImage));
			states.AddState(new int[] { }, new BitmapDrawable(_unpressedImage));
			SetBackgroundDrawable(states);
		}

		private void DrawBackground(Canvas canvas, RectF bounds, Paint fillBackPaint, Paint strokeBackPaint)
		{
			switch(Shape)
			{
			case ButtonShape.BottomTop:
				bounds.Left = 0;
				bounds.Right = Width;
				canvas.DrawRect(bounds, fillBackPaint);
				canvas.DrawLine(bounds.Left, bounds.Top, bounds.Right, bounds.Top, strokeBackPaint);
				canvas.DrawLine(bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom, strokeBackPaint);
				break;
			case ButtonShape.LeftRight:
				bounds.Top = 0;
				bounds.Bottom = Height;
				canvas.DrawRect(bounds, fillBackPaint);
				canvas.DrawLine(bounds.Left, bounds.Top, bounds.Left, bounds.Bottom, strokeBackPaint);
				canvas.DrawLine(bounds.Right, bounds.Top, bounds.Right, bounds.Bottom, strokeBackPaint);
				break;
			case ButtonShape.Rectangle:
				canvas.DrawRect(bounds, strokeBackPaint);
				canvas.DrawRect(bounds, fillBackPaint);
				break;
			default:
				canvas.DrawRoundRect(bounds, _radiusOut, _radiusOut, strokeBackPaint);
				canvas.DrawRoundRect(bounds, _radiusIn, _radiusIn, fillBackPaint);
				break;
			}
		}
	}
}

