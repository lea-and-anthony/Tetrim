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

		Color darkColor = new Color(100, 100, 100, 255);
		Color lightColor = new Color(255, 255, 255, 255);

		public int TextSize = 30;
		public string Text = "";
		public Typeface Typeface;
		public bool IsSquared = false;

		Bitmap pressedImage = null;
		Bitmap unpressedImage = null;

		public ButtonStroked (Context context, Android.Util.IAttributeSet set) : base(context, set)
		{
			this.context = context;
			this.Typeface = Typeface.CreateFromAsset(context.Assets,"Blox.ttf");
			this.Text = this.Tag == null ? this.Text : this.Tag.ToString();
			this.TextSize = getPixelsFromDP(this.TextSize);
			this.SetScaleType(ScaleType.FitCenter);
			this.SetAdjustViewBounds(true);
		}

		public void SetTypeface(Typeface typeface, TypefaceStyle style)
		{
			this.Typeface = typeface;
		}

		public void SetTextSize(ComplexUnitType unit, int size)
		{
			switch(unit)
			{
			case ComplexUnitType.Px:
				TextSize = size;
				break;
			default:
				TextSize = getPixelsFromDP(size);
				break;
			}
		}

		protected override void OnSizeChanged (int w, int h, int oldw, int oldh)
		{
			base.OnSizeChanged (w, h, oldw, oldh);
			if((w != 0 && h != 0 && this.unpressedImage == null) || (this.unpressedImage != null && w != this.unpressedImage.Width && h != this.unpressedImage.Height))
			{
				InitializeImages();
			}
		}

		int getPixelsFromDP(int dp) {
			DisplayMetrics metrics = new DisplayMetrics();
			IWindowManager windowManager = this.context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
			windowManager.DefaultDisplay.GetMetrics(metrics);
			return (int)Math.Round(dp*metrics.Density);
		}

		int getDPFromPixels(int pixels) {
			DisplayMetrics metrics = new DisplayMetrics();
			IWindowManager windowManager = this.context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
			windowManager.DefaultDisplay.GetMetrics(metrics);
			return (int)Math.Round(pixels/metrics.Density);
		}

		protected void InitializeImages()
		{
			this.unpressedImage = Bitmap.CreateBitmap(this.Width, this.IsSquared ? this.Width : this.Height, Bitmap.Config.Argb8888);
			Canvas unpressedCanvas = new Canvas(this.unpressedImage);

			Paint strokeBackPaint = new Paint();
			strokeBackPaint.SetARGB(darkColor.A, darkColor.R, darkColor.G, darkColor.B);
			strokeBackPaint.SetStyle(Android.Graphics.Paint.Style.Stroke);
			strokeBackPaint.StrokeWidth = this.IsSquared ? 20 : 40;
			strokeBackPaint.AntiAlias = true;

			Paint strokePaint = new Paint();
			strokePaint.SetARGB(darkColor.A, darkColor.R, darkColor.G, darkColor.B);
			strokePaint.TextAlign = Android.Graphics.Paint.Align.Center;
			strokePaint.TextSize = this.TextSize;
			strokePaint.SetTypeface(this.Typeface);
			strokePaint.SetStyle(Android.Graphics.Paint.Style.Stroke);
			strokePaint.StrokeWidth = this.IsSquared ? 15 : 20;
			strokePaint.AntiAlias = true;

			Paint textPaint = new Paint();
			textPaint.SetARGB(lightColor.A, lightColor.R, lightColor.G, lightColor.B);
			textPaint.TextAlign = Android.Graphics.Paint.Align.Center;
			textPaint.TextSize = this.TextSize;
			textPaint.SetTypeface(this.Typeface);
			textPaint.AntiAlias = true;

			Rect local = new Rect();
			this.GetLocalVisibleRect(local);
			RectF bounds = new RectF(local);
			bounds.Top += strokePaint.StrokeWidth;
			bounds.Left += strokePaint.StrokeWidth;
			bounds.Right -= strokePaint.StrokeWidth;
			bounds.Bottom -= strokePaint.StrokeWidth;

			unpressedCanvas.DrawRoundRect(bounds, this.IsSquared ? 15 :20, this.IsSquared ? 15 : 20, strokeBackPaint);
			unpressedCanvas.DrawRoundRect(bounds, this.IsSquared ? 20 : 30, this.IsSquared ? 20 : 30, textPaint);

			int cHeight = this.Height;
			Rect r = new Rect();
			float x = this.Width/2;

			strokePaint.GetTextBounds(this.Text, 0, this.Text.Length, r);
			float y = cHeight / 2f + r.Height() / 2f - r.Bottom;
			unpressedCanvas.DrawText(this.Text, x, y, strokePaint);

			textPaint.GetTextBounds(this.Text, 0, this.Text.Length, r);
			y = cHeight / 2f + r.Height() / 2f - r.Bottom;
			unpressedCanvas.DrawText(this.Text, x, y, textPaint);

			this.pressedImage = Bitmap.CreateBitmap(this.Width, this.Height, Bitmap.Config.Argb8888);
			Canvas pressedCanvas = new Canvas(this.pressedImage);

			strokeBackPaint.Color = lightColor;
			strokePaint.Color = lightColor;
			textPaint.Color = darkColor;

			pressedCanvas.DrawRoundRect(bounds, this.IsSquared ? 15 : 20, this.IsSquared ? 15 : 20, strokeBackPaint);
			pressedCanvas.DrawRoundRect(bounds, this.IsSquared ? 20 : 30, this.IsSquared ? 20 : 30, textPaint);
			pressedCanvas.DrawText(this.Text, x, y, strokePaint);
			pressedCanvas.DrawText(this.Text, x, y, textPaint);

			StateListDrawable states = new StateListDrawable();
			states.AddState(new int[] {Android.Resource.Attribute.StatePressed}, new BitmapDrawable(this.pressedImage));
			states.AddState(new int[] {Android.Resource.Attribute.StateFocused}, new BitmapDrawable(this.pressedImage));
			states.AddState(new int[] { }, new BitmapDrawable(this.unpressedImage));
			this.SetBackgroundDrawable(states);
		}
	}
}

