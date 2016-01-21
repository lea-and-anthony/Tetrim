using System;
using System.Collections.Generic;

namespace Tetrim
{
	public class Player
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public string _name { get; private set; }
		public int _score { get; private set; }
		public int _level { get; private set; }
		public int _removedRows { get; private set; }
		public Grid _grid { get; private set; }
		public Piece[] _proposedPieces { get; private set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public Player () : this(Constants.DefaulPlayerName)
		{
		}

		public Player (string name)
		{
			_name = name;
			_score = 0;
			_level = 1;
			_removedRows = 0;
			_grid = new Grid();
			_proposedPieces = new Piece[Constants.NbProposedPiece];
			for(int i = 0; i < Constants.NbProposedPiece; i++)
			{
				_proposedPieces[i] = new Piece((i % (Constants.NbProposedPiece/Constants.NbLinePropPiece))*5, 
													(i / (Constants.NbProposedPiece/Constants.NbLinePropPiece))*5);
				_proposedPieces[i].MoveToZero();
			}
		}

		//--------------------------------------------------------------
		// PUBLICS METHODES
		//--------------------------------------------------------------
		public void UpdatePlayerRemoveRow (int nbRemovedRows)
		{
			_removedRows += nbRemovedRows;
			updateLevel();
			// TODO : choose between original scoring and recent
			switch (nbRemovedRows)
			{
			case 1 :
				_score += Constants.Score1Row * _level;
				break;
			case 2 :
				_score += Constants.Score2Rows * _level;
				break;
			case 3 :
				_score += Constants.Score3Rows * _level;
				break;
			case 4 :
				_score += Constants.Score4Rows * _level;
				break;
			}
   		}

		public void MoveLeft()
		{
			_grid.MoveLeft();
		}

		public void MoveRight()
		{
			_grid.MoveRight();
		}

		public bool MoveDown()
		{
			if(_grid.MoveDown())
			{
				_score += Constants.ScoreMoveDown * _level;
				return true;
			}
			return false;
		}

		public bool MoveBottom()
		{
			int nbDown = _grid.MoveBottom ();
			_score += (Constants.ScoreMoveBottom * nbDown) * _level;
			return nbDown > 0;
		}

		public void TurnLeft()
		{
			_grid.TurnLeft();
		}

		public void TurnRight()
		{
			_grid.TurnRight();
		}

		// Network message to indicate the new position of the piece
		public byte[] getMessagePiece()
		{
			byte[] bytesMessage = new byte[Constants.SizeMessage[Constants.IdMessagePiece]];
			bytesMessage [0] = Constants.IdMessagePiece;
			bytesMessage = _grid.getMessagePiece(bytesMessage, 1);
			return bytesMessage;
		}

		// Network message to communicate the entire grid (should be called
		// each time a piece is placed)
		public byte[] getMessageGrid()
		{
			byte[] bytesMessage = new byte[Constants.SizeMessage[Constants.IdMessageGrid]];
			bytesMessage [0] = Constants.IdMessageGrid;
			bytesMessage = _grid.getMessageGrid(bytesMessage, 1);
			return bytesMessage;
		}

		// Return 0 if an error occurs, 1 if the message is interpreted correctly
		// and it's still the same piece and 2 if it's a new piece
		public int interpretMessage(byte[] message)
		{
			if(message[0] == Constants.IdMessageGrid)
			{
				if(message.Length < Constants.SizeMessage[Constants.IdMessageGrid])
					return 0;

				// The message contains the entire grid
				_grid.interpretGrid(message, 1);
				_grid.interpretPiece(message, Constants.GridSizeX*Constants.GridSizeY + 1);
			}
			else if(message[0] == Constants.IdMessagePiece)
			{
				if(message.Length < Constants.SizeMessage[Constants.IdMessagePiece])
					return 0;

				// The message contains only the position of the piece
				_grid.interpretPiece(message, 1);
			}
			else if(message[0] == Constants.IdMessagePiecePut)
			{
				if(message.Length < Constants.SizeMessage[Constants.IdMessagePiecePut])
					return 0;

				// The message contains the position of the piece
				// we need to put in the grid and the new piece
				_grid.interpretPiece(message, 1);
				_grid.AddPieceToMap(this, false);
				_grid.interpretPiece(message, Constants.SizeMessage[Constants.IdMessagePiece]);
				_grid.UpdateShadowPiece();

				return 2;
			}
			else if(message[0] == Constants.IdMessageNextPiece)
			{
				if(message.Length < Constants.SizeMessage[Constants.IdMessageNextPiece])
					return 0;
				
				_grid.interpretNextPiece(message, 1);
			}

			return 1;
		}

		public bool InterpretScoreMessage(byte[] message)
		{
			if(message.Length < Constants.SizeMessage[Constants.IdMessageScore] || message[0] != Constants.IdMessageScore)
				return false;

			_score = BitConverter.ToInt32(message, 1);
			_level = BitConverter.ToInt32(message, 1 + sizeof(uint));
			_removedRows = BitConverter.ToInt32(message, 1 + 2*sizeof(uint));
			return true;
		}

		public byte[] GetMessageSendNewPiece(int i)
		{
			return _proposedPieces[i].getMessageNextPiece();
		}

		public byte[] GetScoreMessage()
		{
			byte[] message = new byte[Constants.SizeMessage[Constants.IdMessageScore]];

			message[0] = Constants.IdMessageScore;
			completeScoreInMessage(ref message, 1);

			return message;
		}

		public byte[] GetEndMessage()
		{
			byte[] message = new byte[Constants.SizeMessage[Constants.IdMessageEnd]];

			message[0] = Constants.IdMessageEnd;
			completeScoreInMessage(ref message, 1);

			return message;
		}

		public void ChangeProposedPiece(int i)
		{
			_proposedPieces[i] = new Piece((i%(Constants.NbProposedPiece/Constants.NbLinePropPiece)) * 5, 
											(i/(Constants.NbProposedPiece/Constants.NbLinePropPiece)) * 5);
			_proposedPieces[i].MoveToZero();
		}

		//--------------------------------------------------------------
		// PRIVATES METHODES
		//--------------------------------------------------------------
		private void updateLevel()
		{
			while (_removedRows >= _level * 10 && _level < Constants.MaxLevel)
			{
				_level++;
			}
		}

		private void completeScoreInMessage(ref byte[] message, int offset)
		{
			addByteArrayToOverArray(ref message, BitConverter.GetBytes(_score), offset);
			addByteArrayToOverArray(ref message, BitConverter.GetBytes(_level), offset + sizeof(uint));
			addByteArrayToOverArray(ref message, BitConverter.GetBytes(_removedRows), offset + 2*sizeof(uint));
		}

		private void addByteArrayToOverArray(ref byte[] message, byte[] over, int offset)
		{
			for(int i = 0; i < over.Length; i++)
			{
				message[offset + i] = over[i];
			}
		}
	}
}

