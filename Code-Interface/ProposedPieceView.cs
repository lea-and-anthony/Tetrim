﻿using System;
using System.Collections.Generic;

using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace Tetrim
{
	public class ProposedPieceView : View
	{
		//--------------------------------------------------------------
		// CONSTANTS
		//--------------------------------------------------------------
		private const int StrokeWidthBorder = 4;
		private static Paint GridPaint = Utils.createPaintWithStyle(
			new Paint {AntiAlias = true, Color = Utils.getAndroidDarkColor(TetrisColor.Red)},
			Paint.Style.Fill);
		
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		private Player _player; // Instance of the player to whom the pieces are proposed
		private PieceView[] _proposedPieces = new PieceView[Constants.NbProposedPiece]; // Array of the views of the proposed pieces
		private int _selectedPiece = 0; // Selected piece by the player
		private int _nbPieceByLine = 0;
		private Point _offset = null;
		private int _blockSize = 0; // Size of the blocks in pixels according to the screen resolution
		private Dictionary<TetrisColor, Bitmap> _blockImages = new Dictionary<TetrisColor, Bitmap>(); // Images of the blocks

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public ProposedPieceView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			_nbPieceByLine = (int)Math.Ceiling(Constants.NbProposedPiece*1.0/Constants.NbLinePropPiece);
		}

		//--------------------------------------------------------------
		// PUBLIC METHODS
		//--------------------------------------------------------------
		public void RemoveBitmaps()
		{
				foreach(KeyValuePair<TetrisColor, Bitmap> entry in _blockImages)
				{
					entry.Value.Recycle();
					entry.Value.Dispose();
				}
				_blockImages.Clear();
		}

		public void SetPlayer(Player player)
		{
			// Associate the new instance
			_player = player;

			// Recreate the proposed pieces
			for(int i = 0; i < Constants.NbProposedPiece; i++)
			{
				_proposedPieces[i] = new PieceView(player._proposedPieces[i], false);
			}

			// Now we can select one piece to send to the other player
			selectPiece(0);
		}

		public void ChangeProposedPiece()
		{
			if(_selectedPiece < Constants.NbProposedPiece)
			{
				// Change it in the model
				_player.ChangeProposedPiece(_selectedPiece);

				// Change it in the view
				_proposedPieces[_selectedPiece] = new PieceView(_player._proposedPieces[_selectedPiece], false);

				// Select a new piece in the same place
				selectPiece(_selectedPiece);

				Invalidate();
			}
		}

		//--------------------------------------------------------------
		// PRIVATE METHODS
		//--------------------------------------------------------------
		private void selectPiece(int piece)
		{
			if(Network.Instance.Connected && _player != null)
			{
				// Notify the other player of the newly selected piece
				Network.Instance.CommunicationWay.Write(_player.GetMessageSendNewPiece(piece));

				// Select the piece on our side
				_selectedPiece = piece;
			}
		}

		//--------------------------------------------------------------
		// EVENT METHODS
		//--------------------------------------------------------------
		// Draw every proposed piece in the accorded space
		// each piece have the space for 5 blocks to draw itself on the width
		// On the Height, each piece have 4 blocks to draw itself and there is one pixel to separate each one of them (because there is less space on the height)
		protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);

			// If it is the first draw, calculate the size of the block according to the size of the canvas
			if(_blockSize == 0)
			{
				// Calculate the size of the block, Space for each piece set to 5 blocks (except for the last one)
				_blockSize = Math.Min((Width - (_nbPieceByLine - 1) * StrokeWidthBorder)/(_nbPieceByLine*5), 
										(Height - (Constants.NbLinePropPiece - 1) * StrokeWidthBorder)/(Constants.NbLinePropPiece*5));

				// Create the blocks images with the right size
				foreach(TetrisColor color in Enum.GetValues(typeof(TetrisColor)))
				{
					Bitmap image = BlockView.CreateImage(_blockSize, color);
					if(image != null)
					{
						_blockImages.Add(color, image);
					}
				}

				_offset = new Point((Width - _nbPieceByLine*5*_blockSize - (_nbPieceByLine - 1) * StrokeWidthBorder) / 2,
									(Height - Constants.NbLinePropPiece*5*_blockSize - (Constants.NbLinePropPiece - 1) * StrokeWidthBorder) / 2);
			}

			// Draw the pieces and highlight the selected one
			for(int i = 0; i < Constants.NbProposedPiece; i++)
			{
				if(_proposedPieces[i] != null)
				{
					// Show the selected piece
					if(i == _selectedPiece)
					{
						int left = ((i % _nbPieceByLine) == 0) ? 0 : _offset.X;
						int right = (((i + 1) % _nbPieceByLine) == 0) ? Width : _offset.X;
						int top = ((i / _nbPieceByLine) == 0) ? 0 : _offset.Y;
						int bottom = (i >= (_nbPieceByLine * (Constants.NbLinePropPiece - 1))) ? Height : _offset.Y;

						RectF rect = new RectF((i % _nbPieceByLine) * _blockSize * 5 + (i % _nbPieceByLine) * StrokeWidthBorder + left, 
												(_blockSize * 5) * (i / _nbPieceByLine) + (i / _nbPieceByLine) * StrokeWidthBorder + top, 
												((i % _nbPieceByLine) + 1) * _blockSize * 5 + (i % _nbPieceByLine) * StrokeWidthBorder + right,
												(_blockSize * 5) * (1 + i / _nbPieceByLine) + (i / _nbPieceByLine) * StrokeWidthBorder + bottom);

						Paint paint = new Paint {AntiAlias = true, Color = Utils.getAndroidReallyLightColor(TetrisColor.Red)};
						//canvas.DrawRoundRect(rect, Constants.RadiusHighlight, Constants.RadiusHighlight, paint);
						canvas.DrawRect(rect, paint);
						paint.Dispose();
					}
					float xSize = 0;
					float ySize = 0;
					_proposedPieces[i].GetDrawnSize(_blockSize, ref xSize, ref ySize);

					// Draw each piece
					_proposedPieces[i].Draw(canvas, _blockSize, _blockImages, 
						(i % _nbPieceByLine) * _blockSize * 5 + (_blockSize * 5 - xSize) / 2 + _offset.X + (i % _nbPieceByLine) * StrokeWidthBorder, 
						Height - ((i / _nbPieceByLine + 1) * _blockSize * 5 - (_blockSize * 5 - ySize) / 2 + _offset.Y + (i / _nbPieceByLine) * StrokeWidthBorder), 
						Height);
				}
			}

			// Grid
			for(int i = 1; i < _nbPieceByLine; i++)
			{
				// Vertical
				int x = _offset.X + i*5*_blockSize + (i - 1) * StrokeWidthBorder;
				canvas.DrawRect(x, 0, x + StrokeWidthBorder, Height, GridPaint);
			}
			for(int i = 1; i < Constants.NbLinePropPiece; i++)
			{
				// Horizontal
				int y = _offset.Y + i*5*_blockSize + (i - 1) * StrokeWidthBorder;
				canvas.DrawRect(0, y, Width, y + StrokeWidthBorder, GridPaint);
			}
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			bool returnValue = base.OnTouchEvent(e);

			// Get the touch position
			int x = ((int) e.GetX())/(_blockSize * 5 + StrokeWidthBorder);
			int y = ((int) e.GetY())/(_blockSize * 5 + StrokeWidthBorder);

			// Lower the value if it is too high
			if(x >= Constants.NbProposedPiece/Constants.NbLinePropPiece)
			{
				x = Constants.NbProposedPiece/Constants.NbLinePropPiece - 1;
			}
			if(y >= Constants.NbLinePropPiece)
			{
				y = Constants.NbLinePropPiece - 1;
			}

			// Get the piece number
			int i = x + y * _nbPieceByLine;

			if(i >= Constants.NbProposedPiece)
			{
				i = Constants.NbProposedPiece - 1;
			}

			// Select the piece
			selectPiece(i);

			Invalidate();
			return returnValue;
		}
	}
}

