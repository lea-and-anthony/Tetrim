using System;
using System.Collections.Generic;

using Android.Content;

namespace Tetrim
{
	public class User
	{
		public const string UserFileNameKey = "Tetrim.UserFile";

		private const string UserNameKey = "UserName";
		private const string FriendsKey = "Friends";
		private const string HighScoreKey = "HighScore";
		private const char Separator = ',';
		private const int MacAddressLength = 17;

		private static ISharedPreferences _sharedPreferences;

		private static User _instance = null;
		private string _userName;
		private int _highScore;
		private List<string> _friends = new List<string>();
		private bool _wasUserStored = false;

		public static User Instance
		{
			get
			{
				if(_instance == null)
				{
					Load();
				}

				return _instance;
			}
		}

		public string UserName
		{
			get
			{
				return _userName;
			}
		}

		public int HighScore
		{
			get
			{
				return _highScore;
			}
		}

		public List<string> Friends
		{
			get
			{
				return _friends;
			}
		}

		public bool IsUserStored
		{
			get
			{
				return _wasUserStored;
			}
		}

		private User(string userName, int highScore, List<string> friends)
		{
			_userName = userName;
			_highScore = highScore;
			foreach(string friend in friends)
			{
				_friends.Add(friend);
			}
		}

		public void AddFriend(string macAddress)
		{
			if(macAddress.Length == MacAddressLength && !_friends.Contains(macAddress))
			{
				_friends.Add(macAddress);
				Save();
			}
		}

		public void ClearFriends()
		{
			_friends.Clear();
			Save();
		}

		public void AddHighScore(int highScore)
		{
			if(highScore > _highScore)
			{
				_highScore = highScore;
				Save();
			}
		}

		public void ClearHighScore()
		{
			_highScore = 0;
			Save();
		}

		public void SetName(string name)
		{
			if(name != _userName)
			{
				_userName = name;
				Save();
			}
		}

		public static void GiveContext(ref ISharedPreferences sharedPreferences)
		{
			_sharedPreferences = sharedPreferences;
		}

		private static void Save()
		{
			ISharedPreferencesEditor editor = _sharedPreferences.Edit();
			editor.PutString(UserNameKey, _instance._userName);
			editor.PutInt(HighScoreKey, _instance._highScore);
			editor.PutString(FriendsKey, string.Join(Separator.ToString(), _instance._friends.ToArray()));
			editor.Commit();
		}

		private static void Delete()
		{
			ISharedPreferencesEditor editor = _sharedPreferences.Edit();
			editor.PutString(UserNameKey, String.Empty);
			editor.PutInt(HighScoreKey, 0);
			editor.PutString(FriendsKey, String.Empty);
			editor.Commit();
		}

		private static void Load()
		{
			string userName = _sharedPreferences.GetString(UserNameKey, String.Empty);
			string friends = _sharedPreferences.GetString(FriendsKey, String.Empty);
			int highScore = _sharedPreferences.GetInt(HighScoreKey, 0);

			_instance = new User(userName, highScore, new List<string>(friends.Split(Separator)));
			_instance._wasUserStored = (userName != String.Empty);
		}
	}
}
