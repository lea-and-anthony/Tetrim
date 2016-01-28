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

			// Initialize the Shared Preferences
			ISharedPreferences sharedPreferencesUser = ApplicationContext.GetSharedPreferences(User.UserFileNameKey, FileCreationMode.Private);
			User.GiveContext(ref sharedPreferencesUser);

			// Retrieve the fonts
			UtilsUI.TextFont = Typeface.CreateFromAsset(Assets,"Foo.ttf");
			UtilsUI.TitleFont = Typeface.CreateFromAsset(Assets,"Blox.ttf");
			UtilsUI.ArrowFont = Typeface.CreateFromAsset(Assets,"Arrows.otf");

			// Set the fonts
			Utils.SetDefaultFont();

			// Initialize the settings of the buttons
			ButtonUI.GiveContext(ApplicationContext);
			UtilsUI.MenuButtonUI = UtilsUI.CreateMenuButtonSettings();
			UtilsUI.IconButtonUI = UtilsUI.CreateIconButtonSettings();
			UtilsUI.ArrowButtonUI = UtilsUI.CreateArrowButtonSettings();
			UtilsUI.DialogButtonUI = UtilsUI.CreateDialogButtonSettings();
			UtilsUI.DeviceButtonUI = UtilsUI.CreateDeviceButtonSettings();
			UtilsUI.DeviceMenuButtonUI = UtilsUI.CreateDeviceMenuButtonSettings();

			StartActivity(typeof(MenuActivity));
			Finish();
		}
	}
}

// TODO list :
// add sound and music
// suggest game with two players with a highscore limit or with a timeout
// send malus when one player make a good move
// add french and possibility to change language
// add wifi direct