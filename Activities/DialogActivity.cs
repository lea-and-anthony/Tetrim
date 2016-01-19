﻿using System;

using Android.Runtime;
using Android.Util;

using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Tetrim
{
	[Activity(Label = "", Icon = "@drawable/icon", Theme = "@android:style/Theme.Dialog", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]		
	public class DialogActivity : Activity, ViewTreeObserver.IOnGlobalLayoutListener
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		public enum Result
		{
			Postitive,
			Negative,
			Text
		}

		public delegate void StandardDelegate();

		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		LinearLayout _root, _buttonLayout, _content;
		TextView _title;
		TextView _message;
		EditText _field;
		ButtonStroked _positiveButton, _negativeButton;

		public static DialogBuilder Builder;
		public static StandardDelegate CloseAllDialog = null;

		//--------------------------------------------------------------
		// EVENT CATCHING METHODES
		//--------------------------------------------------------------
		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			RequestWindowFeature(WindowFeatures.NoTitle);
			Window.SetBackgroundDrawable(new ColorDrawable(Android.Graphics.Color.Transparent));
			SetContentView(Resource.Layout.Dialog);

			_root = FindViewById<LinearLayout>(Resource.Id.alertContainer);
			_root.SetPadding(Builder.StrokeBorderWidth, Builder.StrokeBorderWidth, Builder.StrokeBorderWidth, Builder.StrokeBorderWidth);
			_root.SetMinimumWidth(WindowManager.DefaultDisplay.Width * 3 / 4);
			_root.SetMinimumHeight(WindowManager.DefaultDisplay.Height * 1 / 10);

			if(_root.ViewTreeObserver.IsAlive)
			{
				_root.ViewTreeObserver.AddOnGlobalLayoutListener(this);
			}

			Typeface niceFont = Typeface.CreateFromAsset(Assets,"Foo.ttf");

			_title = FindViewById<TextView>(Resource.Id.alertTitle);
			_title.SetTextColor(Utils.getAndroidLightColor(TetrisColor.Blue));
			_title.SetTypeface(niceFont, TypefaceStyle.Normal);
			if(String.IsNullOrEmpty(Builder.Title))
			{
				_title.Visibility = ViewStates.Gone;
			}
			else
			{
				_title.Text = Builder.Title;
			}

			_content = FindViewById<LinearLayout>(Resource.Id.alertContent);
			switch (Builder.ContentType)
			{
			case DialogBuilder.DialogContentType.TextView:
				if(!String.IsNullOrEmpty(Builder.Message))
				{
					_message = new TextView(this.BaseContext);
					_message.SetTypeface(niceFont, TypefaceStyle.Normal);
					_message.Text = Builder.Message;
					_message.TextSize = Utils.GetPixelsFromDP(this.BaseContext, 7);
					_content.AddView(_message, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent));
				}
				break;
			case DialogBuilder.DialogContentType.EditText:
				_field = new EditText(this.BaseContext);
				_field.SetTypeface(niceFont, TypefaceStyle.Normal);
				_field.Hint = Builder.Message;
				_field.TextSize = Utils.GetPixelsFromDP(this.BaseContext, 7);
				_content.AddView(_field, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent));
				break;
			case DialogBuilder.DialogContentType.None:
				foreach(View view in Builder.Content)
				{
					_content.AddView(view, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent));
				}
				break;
			default:
				break;
			}

			_buttonLayout = FindViewById<LinearLayout>(Resource.Id.buttonLayout);
			LinearLayout.LayoutParams lp = (LinearLayout.LayoutParams) _buttonLayout.LayoutParameters;
			lp.TopMargin = Builder.StrokeBorderWidth;
			_buttonLayout.LayoutParameters = lp;

			_positiveButton = FindViewById<ButtonStroked>(Resource.Id.positiveButton);
			_negativeButton = FindViewById<ButtonStroked>(Resource.Id.negativeButton);

			if(String.IsNullOrEmpty(Builder.PositiveText))
			{
				Builder.PositiveText = Resources.GetString(Resource.String.ok);
			}

			if(String.IsNullOrEmpty(Builder.NegativeText))
			{
				_negativeButton.Visibility = ViewStates.Gone;
				LinearLayout.LayoutParams lpPos = (LinearLayout.LayoutParams) _positiveButton.LayoutParameters;
				lpPos.Weight = 2;
				_positiveButton.LayoutParameters = lpPos;
			}
			else
			{
				LinearLayout.LayoutParams lpPos = (LinearLayout.LayoutParams) _positiveButton.LayoutParameters;
				lpPos.LeftMargin = Builder.StrokeBorderWidth/2;
				_positiveButton.LayoutParameters = lpPos;
				LinearLayout.LayoutParams lpNeg = (LinearLayout.LayoutParams) _negativeButton.LayoutParameters;
				lpNeg.RightMargin = Builder.StrokeBorderWidth/2;
				_negativeButton.LayoutParameters = lpNeg;
			}

			CreateButton(_positiveButton, TetrisColor.Green, Builder.PositiveText, Builder.PositiveAction, true);
			CreateButton(_negativeButton, TetrisColor.Orange, Builder.NegativeText, Builder.NegativeAction, false);

			CloseAllDialog += Finish;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			CloseAllDialog -= Finish;
		}

		private void CreateButton(ButtonStroked button, TetrisColor color, string text, EventHandler action, bool answer)
		{
			if(!String.IsNullOrEmpty(text))
			{
				button.StrokeColor = Utils.getAndroidDarkColor(color);
				button.FillColor = Utils.getAndroidColor(color);
				button.Text = text;
				button.TextSize = Utils.GetPixelsFromDP(BaseContext, 20);
				button.Click += delegate {
					Builder.ReturnText = (Builder.RequestCode == DialogBuilder.DialogRequestCode.Text ) ? _field.Text : null;
				};
				button.Click += action;
				button.Click += delegate {
					Intent intent = new Intent();
					switch(Builder.RequestCode)
					{
					case DialogBuilder.DialogRequestCode.PosOrNeg:
						intent.PutExtra(Builder.RequestCode.ToString(), answer);
						SetResult(Android.App.Result.Ok, intent);
						Finish();
						break;
					case DialogBuilder.DialogRequestCode.Text:
						if(answer)
						{
							if(!String.IsNullOrEmpty(_field.Text))
							{
								intent.PutExtra(Builder.RequestCode.ToString(), _field.Text);
								SetResult(Android.App.Result.Ok, intent);
								Finish();
							}
						}
						else
						{
							SetResult(Android.App.Result.Canceled);
							Finish();
						}
						break;
					default:
						break;
					}
				};
			}
		}

		public void OnGlobalLayout()
		{
			// The view is completely loaded now, so getMeasuredWidth() won't return 0
			InitializeUI();

			// Destroy the onGlobalLayout afterwards, otherwise it keeps changing
			// the sizes non-stop, even though it's already done
			_root.ViewTreeObserver.RemoveGlobalOnLayoutListener(this);
		}

		protected void InitializeUI()
		{
			_buttonLayout.SetMinimumHeight(_positiveButton.Height);

			// Initialize background
			// TODO : sometimes bug because "width and height must be > 0"
			if(_root.Width <= 0 || _root.Height <= 0)
				return;

			Bitmap backgroundImage = Bitmap.CreateBitmap(_root.Width, _root.Height, Bitmap.Config.Argb8888);
			Canvas canvas = new Canvas(backgroundImage);

			Rect local = new Rect();
			_root.GetLocalVisibleRect(local);
			RectF bounds = new RectF(local);
			bounds.Top += Builder.StrokeBorderWidth/2;
			bounds.Left += Builder.StrokeBorderWidth/2;
			bounds.Right -= Builder.StrokeBorderWidth/2;
			bounds.Bottom -= Builder.StrokeBorderWidth/2;

			// Background fill paint
			Paint fillBackPaint = new Paint();
			fillBackPaint.Color = Builder.FillColor;
			fillBackPaint.AntiAlias = true;

			// Background stroke paint
			Paint strokeBackPaint = new Paint();
			strokeBackPaint.Color = Builder.StrokeColor;
			strokeBackPaint.SetStyle(Paint.Style.Stroke);
			strokeBackPaint.StrokeWidth = Builder.StrokeBorderWidth;
			strokeBackPaint.AntiAlias = true;

			canvas.DrawRoundRect(bounds, Builder.RadiusOut, Builder.RadiusOut, strokeBackPaint);
			canvas.DrawRoundRect(bounds, Builder.RadiusIn, Builder.RadiusIn, fillBackPaint);

			_root.SetBackgroundDrawable(new BitmapDrawable(backgroundImage));
		}

		public void SetPositiveButton(string text, EventHandler action)
		{
			_positiveButton.Text = text;
			_positiveButton.Click += action;
		}

		public void SetNegativeButton(string text, EventHandler action)
		{
			_negativeButton.Text = text;
			_negativeButton.Click += action;
   		}
	}
}