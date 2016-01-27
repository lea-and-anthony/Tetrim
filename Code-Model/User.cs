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
		private Dictionary<string, string> _friends = new Dictionary<string, string>();
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

		public Dictionary<string, string> Friends
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

		private User(string userName, int highScore, Dictionary<string, string> friends)
		{
			_userName = userName;
			_highScore = highScore;
			_friends = new Dictionary<string, string>(friends);
		}

		public void AddFriend(string macAddress, string name)
		{
			// If it is a valid mac address
			if(macAddress.Length == MacAddressLength)
			{
				// If it is already a friend update his name if needed
				// Otherwise add it
				if(_friends.ContainsKey(macAddress))
				{
					if(_friends[macAddress] != name)
					{
						_friends[macAddress] = name;
						Save();
					}
				}
				else
				{
					_friends.Add(macAddress, name);
					Save();
				}
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
			if(name != _userName && !String.IsNullOrWhiteSpace(name))
			{
				_userName = name;
				Save();
			}
		}

		public static void GiveContext(ref ISharedPreferences sharedPreferences)
		{
			_sharedPreferences = sharedPreferences;
		}

		private string parseFriends()
		{
			string result = "{";
			foreach(string friendKey in _friends.Keys)
			{
				result += string.Format("({0},{1})", friendKey, _friends[friendKey]);
			}
			result += "}";
			return  result;
		}

		private static Dictionary<string, string> parseFriends(string friends)
		{
			Dictionary<string, string> friendsDict = new Dictionary<string, string>();
			if(friends.Length > 2 && friends[0] == '{' && friends[friends.Length -1] == '}')
			{
				friends = friends.Substring(1, friends.Length - 2);
				while(!string.IsNullOrEmpty(friends))
				{
					int start = friends.IndexOf("(");
					int end = friends.IndexOf(")");
					string friend = friends.Substring(start + 1, end - start - 1);
					try
					{
						friends = friends.Substring(end + 1);
					}
					catch(ArgumentOutOfRangeException)
					{
						friends = String.Empty;
					}

					int middle = friend.IndexOf(",");
					string macAddress = friend.Substring(0, middle);
					string name = friend.Substring(middle + 1, friend.Length - (middle + 1));
					friendsDict.Add(macAddress, name);
				}
			}
			return friendsDict;
		}

		private void Save()
		{
			ISharedPreferencesEditor editor = _sharedPreferences.Edit();
			editor.PutString(UserNameKey, _instance._userName);
			editor.PutInt(HighScoreKey, _instance._highScore);
			editor.PutString(FriendsKey, parseFriends());
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

			_instance = new User(userName, highScore, parseFriends(friends));
			_instance._wasUserStored = (userName != String.Empty);
		}
	}
}
