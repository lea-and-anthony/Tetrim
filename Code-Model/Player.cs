using System;
using System.Collections.Generic;

namespace Tetris
{
	public class Player
	{
		//--------------------------------------------------------------
		// ATTRIBUTES
		//--------------------------------------------------------------
		public string m_name { get; private set; }
		public uint m_score { get; private set; }
		public uint m_level { get; private set; }
		public int m_removedRows { get; private set; }
		public Grid m_grid { get; private set; }
		public Piece[] m_proposedPieces { get; private set; }

		//--------------------------------------------------------------
		// CONSTRUCTORS
		//--------------------------------------------------------------
		public Player () : this(Constants.DefaulPlayerName)
		{
		}

		public Player (string name)
		{
			m_name = name;
			m_score = 0;
			m_level = 1;
			m_removedRows = 0;
			m_grid = new Grid();
			m_proposedPieces = new Piece[Constants.NbProposedPiece];
			for(int i = 0; i < Constants.NbProposedPiece; i++)
			{
				m_proposedPieces[i] = new Piece((i % (Constants.NbProposedPiece/Constants.NbLinePropPiece))*5, 
													(i / (Constants.NbProposedPiece/Constants.NbLinePropPiece))*5);
			}
		}

		//--------------------------------------------------------------
		// METHODES
		//--------------------------------------------------------------
		public void UpdatePlayerRemoveRow (int nbRemovedRows)
		{
			m_removedRows += nbRemovedRows;
			updateLevel();
			switch (nbRemovedRows)
			{
			case 1 :
				m_score += Constants.Score1Row * m_level;
				break;
			case 2 :
				m_score += Constants.Score2Rows * m_level;
				break;
			case 3 :
				m_score += Constants.Score3Rows * m_level;
				break;
			case 4 :
				m_score += Constants.Score4Rows * m_level;
				break;
			}
		}

		private void updateLevel()
		{
			while (m_removedRows >= m_level * 10 && m_level < Constants.MaxLevel)
			{
				m_level++;
			}
		}

		public void MoveLeft()
		{
			m_grid.MoveLeft();
		}

		public void MoveRight()
		{
			m_grid.MoveRight();
		}

		public void MoveDown()
		{
			m_grid.MoveDown();
			m_score += Constants.ScoreMoveDown * m_level;
		}

		public void MoveBottom()
		{
			int nbDown = m_grid.MoveBottom ();
			m_score += (uint) (Constants.ScoreMoveBottom * nbDown) * m_level;
		}

		public void TurnLeft()
		{
			m_grid.TurnLeft();
		}

		public void TurnRight()
		{
			m_grid.TurnRight();
		}

		// Network message to indicate the new position of the piece
		public byte[] getMessagePiece()
		{
			byte[] bytesMessage = new byte[Constants.SizeMessagePiece];
			bytesMessage [0] = Constants.IdMessagePiece;
			bytesMessage = m_grid.getMessagePiece(bytesMessage, 1);
			return bytesMessage;
		}

		// Network message to communicate the entire grid (should be called
		// each time a piece is placed)
		public byte[] getMessageGrid()
		{
			byte[] bytesMessage = new byte[Constants.SizeMessageGrid];
			bytesMessage [0] = Constants.IdMessageGrid;
			bytesMessage = m_grid.getMessageGrid(bytesMessage, 1);
			return bytesMessage;
		}

		// Return 0 if an error occurs, 1 if the message is interpreted correctly
		// and it's still the same piece and 2 if it's a new piece
		public int interpretMessage(byte[] message)
		{
			if(message[0] == Constants.IdMessageGrid)
			{
				if(message.Length < Constants.SizeMessageGrid)
					return 0;

				// The message contains the entire grid
				m_grid.interpretGrid(message, 1);
				m_grid.interpretPiece(message, Constants.GridSizeX*Constants.GridSizeY + 1);
			}
			else if(message[0] == Constants.IdMessagePiece)
			{
				if(message.Length < Constants.SizeMessagePiece)
					return 0;

				// The message contains only the position of the piece
				m_grid.interpretPiece(message, 1);
			}
			else if(message[0] == Constants.IdMessagePiecePut)
			{
				if(message.Length < Constants.SizeMessagePiecePut)
					return 0;

				// The message contains the position of the piece
				// we need to put in the grid and the new piece
				m_grid.interpretPiece(message, 1);
				m_grid.AddPieceToMap(this);
				m_grid.interpretPiece(message, Constants.SizeMessagePiece);
				m_grid.UpdateShadowPiece();

				return 2;
			}
			else if(message[0] == Constants.IdMessageNextPiece)
			{
				if(message.Length < Constants.SizeMessageNextPiece)
					return 0;
				
				m_grid.interpretNextPiece(message, 1);
			}

			return 1;
		}

		public byte[] GetMessageSendNewPiece(int i)
		{
			return m_proposedPieces[i].getMessageNextPiece();
		}

		public void ChangeProposedPiece(int i)
		{
			m_proposedPieces[i] = new Piece((i%(Constants.NbProposedPiece/Constants.NbLinePropPiece)) * 5, 
											(i/(Constants.NbProposedPiece/Constants.NbLinePropPiece)) * 5);
		}
	}
}

