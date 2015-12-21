using System;

using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Android.Runtime;

using Android.Util;

namespace Tetrim
{
	public class ButtonStroked : ImageButton
	{
		Context context;

		public Color DarkColor = new Color(100, 100, 100, 255);
		public Color LightColor = new Color(255, 255, 255, 255);
		public int StrokeBorderWidth = 40;
		public int StrokeTextWidth = 20;
		public int RadiusIn = 30;
		public int RadiusOut = 20;
		public bool IsTextStroked = true;

		public int TextSize = 30;
		public string Text = "";
		public Typeface Typeface;
		public bool IsSquared = false;

		Bitmap pressedImage = null;
		Bitmap unpressedImage = null;

		public ButtonStroked (Context context, Android.Util.IAttributeSet set) : base(context, set)
		{
			this.context = context;
			this.Typeface = Typeface.CreateFromAsset(context.Assets,"Foo.ttf");
			this.Text = this.Tag == null ? this.Text : this.Tag.ToString();
			this.TextSize = Utils.GetPixelsFromDP(this.context, this.TextSize);
			this.SetScaleType(ScaleType.FitCenter);
			this.SetAdjustViewBounds(true);
		}

		public void SetTypeface(Typeface typeface, TypefaceStyle style)
		{
			this.Typeface = typeface;
		}

		public void SetTextSize(ComplexUnitType unit, int size)
		{
			this.TextSize = Utils.ConvertTextSize(this.context, unit, size);
		}

		protected override void OnSizeChanged (int w, int h, int oldw, int oldh)
		{
			base.OnSizeChanged (w, h, oldw, oldh);
			if((w != 0 && h != 0 && this.unpressedImage == null) || (this.unpressedImage != null && w != this.unpressedImage.Width && h != this.unpressedImage.Height))
			{
				InitializeImages();
			}
		}

		protected void InitializeImages()
		{
			this.unpressedImage = Bitmap.CreateBitmap(this.Width, this.IsSquared ? this.Width : this.Height, Bitmap.Config.Argb8888);
			this.pressedImage = Bitmap.CreateBitmap(this.Width, this.IsSquared ? this.Width : this.Height, Bitmap.Config.Argb8888);
			Canvas unpressedCanvas = new Canvas(this.unpressedImage);
			Canvas pressedCanvas = new Canvas(this.pressedImage);

			// Background fill paint
			Paint fillBackPaint = new Paint();
			fillBackPaint.Color = LightColor;
			fillBackPaint.AntiAlias = true;

			// Background stroke paint
			Paint strokeBackPaint = new Paint();
			strokeBackPaint.Color = DarkColor;
			strokeBackPaint.SetStyle(Android.Graphics.Paint.Style.Stroke);
			strokeBackPaint.StrokeWidth = this.StrokeBorderWidth;
			strokeBackPaint.AntiAlias = true;

			// Text paint
			Paint textPaint = new Paint();
			textPaint.Color = IsTextStroked ? LightColor : DarkColor;
			textPaint.TextAlign = Android.Graphics.Paint.Align.Center;
			textPaint.TextSize = this.TextSize;
			textPaint.SetTypeface(this.Typeface);
			textPaint.AntiAlias = true;

			// Text stroke paint
			Paint strokePaint = new Paint();
			strokePaint.Color = DarkColor;
			strokePaint.TextAlign = Android.Graphics.Paint.Align.Center;
			strokePaint.TextSize = this.TextSize;
			strokePaint.SetTypeface(this.Typeface);
			strokePaint.SetStyle(Android.Graphics.Paint.Style.Stroke);
			strokePaint.StrokeWidth = this.StrokeTextWidth;
			strokePaint.AntiAlias = true;

			// Background bounds
			Rect local = new Rect();
			this.GetLocalVisibleRect(local);
			RectF bounds = new RectF(local);
			bounds.Top += this.StrokeBorderWidth/2;
			bounds.Left += this.StrokeBorderWidth/2;
			bounds.Right -= this.StrokeBorderWidth/2;
			bounds.Bottom -= this.StrokeBorderWidth/2;

			// Text location
			Rect r = new Rect();
			strokePaint.GetTextBounds(this.Text, 0, this.Text.Length, r);
			float x = this.Width/2;
			float y = this.Height / 2f + r.Height() / 2f - r.Bottom;

			// Draw unpressed
			unpressedCanvas.DrawRoundRect(bounds, this.RadiusOut, this.RadiusOut, strokeBackPaint);
			unpressedCanvas.DrawRoundRect(bounds, this.RadiusIn, this.RadiusIn, fillBackPaint);
			if(IsTextStroked)
				unpressedCanvas.DrawText(this.Text, x, y, strokePaint);
			unpressedCanvas.DrawText(this.Text, x, y, textPaint);

			// Change colors
			fillBackPaint.Color = DarkColor;
			strokeBackPaint.Color = LightColor;
			strokePaint.Color = LightColor;
			textPaint.Color = IsTextStroked ? DarkColor : LightColor;

			// Draw pressed
			pressedCanvas.DrawRoundRect(bounds, this.RadiusOut, this.RadiusOut, strokeBackPaint);
			pressedCanvas.DrawRoundRect(bounds, this.RadiusIn, this.RadiusIn, fillBackPaint);
			if(IsTextStroked)
				pressedCanvas.DrawText(this.Text, x, y, strokePaint);
			pressedCanvas.DrawText(this.Text, x, y, textPaint);

			// Set images for states
			StateListDrawable states = new StateListDrawable();
			states.AddState(new int[] {Android.Resource.Attribute.StatePressed}, new BitmapDrawable(this.pressedImage));
			states.AddState(new int[] {Android.Resource.Attribute.StateFocused}, new BitmapDrawable(this.pressedImage));
			states.AddState(new int[] { }, new BitmapDrawable(this.unpressedImage));
			this.SetBackgroundDrawable(states);
		}
	}
}

