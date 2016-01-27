using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;

namespace Tetrim
{
	[Activity(MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]	
	public class MainActivity : Activity
	{
		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			ISharedPreferences sharedPreferencesUser = ApplicationContext.GetSharedPreferences(User.UserFileNameKey, FileCreationMode.Private);
			User.GiveContext(ref sharedPreferencesUser);

			// Retrieve the fonts
			UtilsUI.TextFont = Typeface.CreateFromAsset(Assets,"Foo.ttf");
			UtilsUI.TitleFont = Typeface.CreateFromAsset(Assets,"Blox.ttf");
			UtilsUI.ArrowFont = Typeface.CreateFromAsset(Assets,"Arrows.otf");
			Utils.SetDefaultFont();

			StartActivity(typeof(MenuActivity));
			Finish();
		}
	}
}

