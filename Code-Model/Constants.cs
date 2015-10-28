using System;
using System.Collections.Generic;

namespace Tetrim
{
	public static class Constants
	{
		public static Random Rand = new Random();
		public const int GridSizeXmin = 0;
		public const int GridSizeXmax = 9;
		public const int GridSizeX = GridSizeXmax - GridSizeXmin + 1;
		public const int GridSizeYmin = 0;
		public const int GridSizeYmax = 19;
		public const int GridSizeY = GridSizeYmax - GridSizeYmin + 1;
		public const uint BlockSize = 10;
		public const uint BlockPerPiece = 4;
		public const string DefaulPlayerName = "Anonymous";
		public const string ScorePrint = "Score : ";
		public const string LevelPrint = "Level : ";
		public const string RowsPrint = "Rows cleared : ";
		public const int ShapeMax = (int) Shape.ShapeMax;
		public const int ColorMax = (int) TetrisColor.ColorMax;

		public const int MaxLevel = 30;
		public static uint Score1Row = 100;
		public static uint Score2Rows = 300;
		public static uint Score3Rows = 500;
		public static uint Score4Rows = 800;
		public const int ScoreMoveDown = 1;
		public const int ScoreMoveBottom = 2;

		public const byte IdMessagePiece = 1;
		public const byte IdMessageGrid = 2;
		public const byte IdMessageStart = 3;
		public const byte IdMessagePause = 4;
		public const byte IdMessageResume = 5;
		public const byte IdMessagePiecePut = 6;
		public const byte IdMessageNextPiece = 7;

		public const uint SizeMessagePiece = 2+1+1 + 1;
		public const uint SizeMessagePiecePut = 2*SizeMessagePiece + 1 - 1;
		public const uint SizeMessageGrid = GridSizeX*GridSizeY+SizeMessagePiece;
		public const uint SizeMessageNextPiece = 1 + 1;
		public const uint SizeMessagePause = 1;
		public const uint SizeMessageResume = 1;
		public const uint SizeMaxBluetoothMessage = 512;

		public const byte NumVersion1 = 1;
		public const byte NumVersion2 = 0;

		public const int NbProposedPiece = 4;
		public const int NbLinePropPiece = 1;
	}

	public enum Shape
	{
		I, J, L, O,	S, T, Z, ShapeMax
	};

	public enum TetrisColor
	{
		Red,
		Orange,
		Yellow,
		Green,
		Cyan,
		Blue,
		Pink,
		ColorMax
	};
}
