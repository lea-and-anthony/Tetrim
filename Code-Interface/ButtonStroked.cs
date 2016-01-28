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
		// ATTRIBUTES
		//--------------------------------------------------------------
		Context _context;
		Bitmap _pressedImage = null;
		Bitmap _unpressedImage = null;

		public ButtonUI Settings = UtilsUI.MenuButtonUI;
		public string Text = "";

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
		
		//--------------------------------------------------------------
		// PUBLIC METHODS
		//--------------------------------------------------------------
		public void RemoveBitmaps()
		{
			if(_pressedImage != null)
			{
				_pressedImage.Recycle();
				_pressedImage.Dispose();
				_pressedImage = null;
			}
			if(_unpressedImage != null)
			{
				_unpressedImage.Recycle();
				_unpressedImage.Dispose();
				_unpressedImage = null;
			}
		}

		//--------------------------------------------------------------
		// EVENT METHODS
		//--------------------------------------------------------------
		protected override void OnSizeChanged (int w, int h, int oldw, int oldh)
		{
			base.OnSizeChanged (w, h, oldw, oldh);
			if((w != 0 && h != 0 && _unpressedImage == null) || (_unpressedImage != null && (w != _unpressedImage.Width || h != _unpressedImage.Height)))
			{
				InitializeImages();
			}
		}

		//--------------------------------------------------------------
		// PRIVATE METHODS
		//--------------------------------------------------------------
		private void CreateButton(Context context)
		{
			_context = context;
			Text = Tag == null ? Text : Tag.ToString();
			SetScaleType(ScaleType.FitCenter);
			SetAdjustViewBounds(true);
		}

		private void InitializeImages()
		{
			if(_pressedImage != null)
			{
				_pressedImage.Recycle();
				_pressedImage.Dispose();
			}
			if(_unpressedImage != null)
			{
				_unpressedImage.Recycle();
				_unpressedImage.Dispose();
			}

			_unpressedImage = Bitmap.CreateBitmap(Width, Settings.IsSquared ? Width : Height, Bitmap.Config.Argb8888);
			_pressedImage = Bitmap.CreateBitmap(Width, Settings.IsSquared ? Width : Height, Bitmap.Config.Argb8888);
			Canvas unpressedCanvas = new Canvas(_unpressedImage);
			Canvas pressedCanvas = new Canvas(_pressedImage);

			Settings.SetTextSize(ComplexUnitType.Px, Settings.TextSize == 0f ? Height * Settings.TextSizeRatio : Settings.TextSize);
			Settings.StrokeBorderWidth = Height * Settings.StrokeBorderWidthRatio;
			Settings.StrokeTextWidth = Height * Settings.StrokeTextWidthRatio;

			// Background fill paint
			Paint fillBackPaint = new Paint();
			fillBackPaint.Color = Settings.FillColor;
			fillBackPaint.AntiAlias = true;

			// Background stroke paint
			Paint strokeBackPaint = new Paint();
			strokeBackPaint.Color = Settings.StrokeColor;
			strokeBackPaint.SetStyle(Paint.Style.Stroke);
			strokeBackPaint.StrokeWidth = Settings.StrokeBorderWidth;
			strokeBackPaint.AntiAlias = true;

			// Text paint
			Paint textPaint = new Paint();
			textPaint.Color = Settings.IsTextStroked ? Settings.FillColor : Settings.StrokeColor;
			textPaint.TextAlign = Paint.Align.Center;
			textPaint.TextSize = Settings.TextSize;
			textPaint.SetTypeface(Settings.Typeface);
			textPaint.AntiAlias = true;

			// Text stroke paint
			Paint strokePaint = new Paint();
			strokePaint.Color = Settings.StrokeColor;
			strokePaint.TextAlign = Paint.Align.Center;
			strokePaint.TextSize = Settings.TextSize;
			strokePaint.SetTypeface(Settings.Typeface);
			strokePaint.SetStyle(Paint.Style.Stroke);
			strokePaint.StrokeWidth = Settings.StrokeTextWidth;
			strokePaint.AntiAlias = true;

			// Background bounds
			Rect local = new Rect();
			this.GetLocalVisibleRect(local);
			RectF bounds = new RectF(local);
			bounds.Top += Settings.StrokeBorderWidth/2;
			bounds.Left += Settings.StrokeBorderWidth/2;
			bounds.Right -= Settings.StrokeBorderWidth/2;
			bounds.Bottom -= Settings.StrokeBorderWidth/2;

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
			while(r.Width() + Settings.StrokeTextWidth >= bounds.Width())
			{
				Settings.SetTextSize(ComplexUnitType.Px, Settings.TextSize-1);
				textPaint.TextSize = Settings.TextSize;
				strokePaint.TextSize = Settings.TextSize;
				strokePaint.GetTextBounds(Text, 0, Text.Length, r);
			}

			float x=0, y=0;
			switch (Settings.Gravity)
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
			switch (Settings.Gravity)
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
			if(Settings.IsTextStroked)
				unpressedCanvas.DrawText(Text, x, y, strokePaint);
			unpressedCanvas.DrawText(Text, x, y, textPaint);

			// Change colors
			fillBackPaint.Color = Settings.StrokeColor;
			strokeBackPaint.Color = Settings.FillColor;
			strokePaint.Color = Settings.FillColor;
			textPaint.Color = Settings.IsTextStroked ? Settings.StrokeColor : Settings.FillColor;

			// Draw pressed
			DrawBackground(pressedCanvas, bounds, fillBackPaint, strokeBackPaint);
			if(Settings.IsTextStroked)
				pressedCanvas.DrawText(Text, x, y, strokePaint);
			pressedCanvas.DrawText(Text, x, y, textPaint);

			// Set images for states
			StateListDrawable states = new StateListDrawable();
			states.AddState(new int[] {Android.Resource.Attribute.StatePressed}, new BitmapDrawable(_pressedImage));
			states.AddState(new int[] {Android.Resource.Attribute.StateFocused}, new BitmapDrawable(_pressedImage));
			states.AddState(new int[] {Android.Resource.Attribute.StateSelected}, new BitmapDrawable(_pressedImage));
			states.AddState(new int[] { }, new BitmapDrawable(_unpressedImage));
			SetBackgroundDrawable(states);

			strokePaint.Dispose();
			textPaint.Dispose();
			strokeBackPaint.Dispose();
			fillBackPaint.Dispose();
			unpressedCanvas.Dispose();
			pressedCanvas.Dispose();
		}

		private void DrawBackground(Canvas canvas, RectF bounds, Paint fillBackPaint, Paint strokeBackPaint)
		{
			switch(Settings.Shape)
			{
			case ButtonUI.ButtonShape.BottomTop:
				bounds.Left = 0;
				bounds.Right = Width;
				canvas.DrawRect(bounds, fillBackPaint);
				canvas.DrawLine(bounds.Left, bounds.Top, bounds.Right, bounds.Top, strokeBackPaint);
				canvas.DrawLine(bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom, strokeBackPaint);
				break;
			case ButtonUI.ButtonShape.LeftRight:
				bounds.Top = 0;
				bounds.Bottom = Height;
				canvas.DrawRect(bounds, fillBackPaint);
				canvas.DrawLine(bounds.Left, bounds.Top, bounds.Left, bounds.Bottom, strokeBackPaint);
				canvas.DrawLine(bounds.Right, bounds.Top, bounds.Right, bounds.Bottom, strokeBackPaint);
				break;
			case ButtonUI.ButtonShape.Rectangle:
				canvas.DrawRect(bounds, strokeBackPaint);
				canvas.DrawRect(bounds, fillBackPaint);
				break;
			default:
				canvas.DrawRoundRect(bounds, Settings.RadiusOut, Settings.RadiusOut, strokeBackPaint);
				canvas.DrawRoundRect(bounds, Settings.RadiusIn, Settings.RadiusIn, fillBackPaint);
				break;
			}
		}
	}
}

