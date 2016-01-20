using System;

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

			UtilsUI.SetDialogButton(this, _positiveButton, _field, TetrisColor.Green, Builder.PositiveText, Builder.PositiveAction, true);
			UtilsUI.SetDialogButton(this, _negativeButton, _field, TetrisColor.Orange, Builder.NegativeText, Builder.NegativeAction, false);

			CloseAllDialog += Finish;
		}

		public override void OnBackPressed ()
		{
			// Do nothing = not cancelable
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			CloseAllDialog -= Finish;
   		}

		public void OnGlobalLayout()
		{
			// The view is completely loaded now, so getMeasuredWidth() won't return 0
			InitializeUI();

			// Destroy the onGlobalLayout afterwards, otherwise it keeps changing
			// the sizes non-stop, even though it's already done
			_root.ViewTreeObserver.RemoveGlobalOnLayoutListener(this);
   		}

		//--------------------------------------------------------------
		// PROTECTED METHODES
		//--------------------------------------------------------------
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

		protected static Intent InitializeDialog(Activity activity, DialogBuilder builder, string posText, string negText, EventHandler posAction, EventHandler negAction)
		{
			if(!String.IsNullOrEmpty(posText))
			{
				builder.PositiveText = posText;
			}
			if(posAction != null)
			{
				builder.PositiveAction += posAction;
			}
			if(!String.IsNullOrEmpty(negText))
			{
				builder.NegativeText = negText;
			}
			if(negAction != null)
			{
				builder.NegativeAction += negAction;
			}
			DialogActivity.Builder = builder;
			return new Intent(activity, typeof(DialogActivity));
		}

		protected static string parseId(Activity activity, int id)
		{
			return (id != -1) ? activity.Resources.GetString(id) : String.Empty;
		}

		//--------------------------------------------------------------
		// PUBLIC METHODES
		//--------------------------------------------------------------
		public static Intent CreateYesDialog(Activity activity, int titleId, int messageId, EventHandler posAction, EventHandler negAction)
		{
			return CreateYesNoDialog(activity, titleId, messageId, -1, -1, posAction, negAction);
		}

		public static Intent CreateYesDialog(Activity activity, string title, string message, EventHandler posAction, EventHandler negAction)
		{
			return CreateYesNoDialog(activity, title, message, String.Empty, String.Empty, posAction, negAction);
		}

		public static Intent CreateYesNoDialog(Activity activity, int titleId, int messageId, EventHandler posAction, EventHandler negAction)
		{
			return CreateYesNoDialog(activity, titleId, messageId, Resource.String.yesDialog, Resource.String.noDialog, posAction, negAction);
		}

		public static Intent CreateYesNoDialog(Activity activity, string title, string message, EventHandler posAction, EventHandler negAction)
		{
			return CreateYesNoDialog(activity, title, message,
				activity.Resources.GetString(Resource.String.yesDialog), activity.Resources.GetString(Resource.String.noDialog), posAction, negAction);
		}

		public static Intent CreateYesNoDialog(Activity activity, int titleId, int messageId, int posTextId, int negTextId, EventHandler posAction, EventHandler negAction)
		{
			return CreateBasicDialog(activity, DialogBuilder.DialogRequestCode.PosOrNeg, DialogBuilder.DialogContentType.TextView, titleId, messageId, posTextId, negTextId, posAction, negAction);
		}

		public static Intent CreateYesNoDialog(Activity activity, string title, string message, string posText, string negText, EventHandler posAction, EventHandler negAction)
		{
			return CreateBasicDialog(activity, DialogBuilder.DialogRequestCode.PosOrNeg, DialogBuilder.DialogContentType.TextView, title, message, posText, negText, posAction, negAction);
		}

		public static Intent CreateTextPromptDialog(Activity activity, int titleId, int messageId, int posTextId, int negTextId, EventHandler posAction, EventHandler negAction)
		{
			return CreateBasicDialog(activity, DialogBuilder.DialogRequestCode.Text, DialogBuilder.DialogContentType.EditText, titleId, messageId, posTextId, negTextId, posAction, negAction);
		}

		public static Intent CreateBasicDialog(Activity activity, DialogBuilder.DialogRequestCode request, DialogBuilder.DialogContentType type, int titleId, int messageId, int posTextId, int negTextId, EventHandler posAction, EventHandler negAction)
		{
			string title = parseId(activity, titleId);
			string message = parseId(activity, messageId);
			string posText = parseId(activity, posTextId);
			string negText = parseId(activity, negTextId);
			return CreateBasicDialog(activity, request, type, title, message, posText, negText, posAction, negAction);
		}

		public static Intent CreateBasicDialog(Activity activity, DialogBuilder.DialogRequestCode request, DialogBuilder.DialogContentType type, string title, string message, string posText, string negText, EventHandler posAction, EventHandler negAction)
		{
			DialogBuilder builder = new DialogBuilder(activity.BaseContext);
			builder.RequestCode = request;
			builder.ContentType = type;
			if(!String.IsNullOrEmpty(title))
			{
				builder.Title = title;
			}
			if(!String.IsNullOrEmpty(message))
			{
				builder.Message = message;
			}
			return InitializeDialog(activity, builder, posText, negText, posAction, negAction);
		}

		public static Intent CreateCustomDialog(Activity activity, View[] content, int posTextId, int negTextId, EventHandler posAction, EventHandler negAction)
		{
			string posText = parseId(activity, posTextId);
			string negText = parseId(activity, negTextId);
			return CreateCustomDialog(activity, content, posText, negText, posAction, negAction);
		}

		public static Intent CreateCustomDialog(Activity activity, View[] content, string posText, string negText, EventHandler posAction, EventHandler negAction)
		{
			DialogBuilder builder = new DialogBuilder(activity.BaseContext);
			builder.ContentType = DialogBuilder.DialogContentType.None;
			builder.RequestCode = DialogBuilder.DialogRequestCode.PosOrNeg;
			builder.Content.AddRange(content);
			return InitializeDialog(activity, builder, posText, negText, posAction, negAction);
		}
	}
}