/*
	Compile using the C# compiler (csc.exe):
	
		csc.exe /out:GoodGameV1.exe GoodGame_v1.cs
*/

namespace GoodGameV1
{
	using System.Collections.Generic;
	using System;
	using System.IO;
	using System.Globalization;

	public class Constants
	{
		public static string Name = "GoodGameV1";
	}
	
	public class Board
	{
		public int Width { get; private set; }
		public int Height { get; private set; }

		private int[,] _productionArray;

		public Board(int[,] productionArray)
		{
			this.Width = productionArray.GetLength(0);
			this.Height = productionArray.GetLength(1);
			this._productionArray = new int[this.Width, this.Height];
			for (int i = 0; i < this.Width; i++)
				for (int j = 0; j < this.Height; j++)
					this._productionArray[i, j] = productionArray[i, j];
		}

		// Gets the production value at coordinates (x, y)
		// This uses standard math conventions; i.e. (0,0) is at the bottom left
		// of the board.  (1, 0) is to the right of (0, 0).  (0, 1) is above (0, 0).
		public int Production(int x, int y)
		{
			int width = this.Width;
			int height = this.Height;

			if (x < 0)
			{
				int num = (-x) / width;
				x += width * num;

				while (x < 0)
					x += width;
			}

			if (x >= width)
				x = x % width;

			if (y < 0)
			{
				int num = (-y) / height;
				y += height * num;

				while (y < 0)
					y += height;
			}

			if (y >= height)
				y = y % height;

			return this._productionArray[x, y];
		}
	}

	public enum Owner
	{
		Player,
		Unowned,
		// Note that there may be more than one opponent on the board
		// This enum value does not distinguish which opponent is the owner.
		Enemy
	}

	public class Cell
	{
		public Owner Owner { get; private set; }
		public int Strength { get; private set; }
		// Non-null only when Owner = Enemy
		// Then, this value distinguishes which opponent owns this cell.
		public int? EnemyId { get; private set; }

		public Cell(Owner owner, int strength, int? enemyId)
		{
			this.Owner = owner;
			this.Strength = strength;
			
			if (owner != Owner.Enemy && enemyId != null)
				throw new Exception();
			if (owner == Owner.Enemy && enemyId == null)
				throw new Exception();
			
			this.EnemyId = enemyId;
		}
	}

	public class GameState
	{
		public Board Board { get; private set; }
		private Cell[,] _state;

		public GameState(Board board, Cell[,] state)
		{
			this.Board = board;
			
			this._state = new Cell[state.GetLength(0), state.GetLength(1)];
			
			for (int i = 0; i < state.GetLength(0); i++)
				for (int j = 0; j < state.GetLength(1); j++)
					this._state[i, j] = state[i, j];
		}

		// Gets the cell at coordinates (x, y)
		// This uses standard math conventions; i.e. (0,0) is at the bottom left
		// of the board.  (1, 0) is to the right of (0, 0).  (0, 1) is above (0, 0).
		public Cell Cell(int x, int y)
		{
			int width = this.Board.Width;
			int height = this.Board.Height;

			if (x < 0)
			{
				int num = (-x) / width;
				x += width * num;

				while (x < 0)
					x += width;
			}

			if (x >= width)
				x = x % width;

			if (y < 0)
			{
				int num = (-y) / height;
				y += height * num;

				while (y < 0)
					y += height;
			}

			if (y >= height)
				y = y % height;

			return this._state[x, y];
		}
	}

	public enum Direction
	{
		Up,
		Down,
		Left,
		Right,
		None
	}

	public class Move
	{
		// This uses standard math conventions; i.e. (x, y) = (0,0) is at the bottom left
		// of the board.  (1, 0) is to the right of (0, 0).  (0, 1) is above (0, 0).
		public int X { get; private set; }
		public int Y { get; private set; }
		public Direction Direction { get; private set; }

		public Move(int x, int y, Direction direction)
		{
			this.X = x;
			this.Y = y;
			this.Direction = direction;
		}
	}

	public interface IHaliteAI
	{
		List<Move> GetNextStep(GameState gameState);
	}

	public class HaliteAI : IHaliteAI
	{
		private class DistanceToNearestNonPlayerCellArray
		{
			private int[,] _array;
			private int _width;
			private int _height;

			private int? NullableMin(int? a, int? b)
			{
				if (a == null && b == null)
					return null;
				if (a == null)
					return b.Value;
				if (b == null)
					return a.Value;
				if (a.Value < b.Value)
					return a.Value;
				return b.Value;
			}

			public int Distance(int x, int y)
			{
				int width = this._width;
				int height = this._height;

				if (x < 0)
				{
					int num = (-x) / width;
					x += width * num;

					while (x < 0)
						x += width;
				}

				if (x >= width)
					x = x % width;

				if (y < 0)
				{
					int num = (-y) / height;
					y += height * num;

					while (y < 0)
						y += height;
				}

				if (y >= height)
					y = y % height;

				return this._array[x, y];
			}

			public DistanceToNearestNonPlayerCellArray(GameState gameState)
			{
				int width = gameState.Board.Width;
				int height = gameState.Board.Height;

				this._width = width;
				this._height = height;

				List<Tuple<int, int>> list = new List<Tuple<int, int>>();

				for (int i = 0; i < width; i++)
					for (int j = 0; j < height; j++)
					{
						if (gameState.Cell(i, j).Owner != Owner.Player)
							list.Add(new Tuple<int, int>(i, j));
					}

				if (list.Count == 0)
				{
					this._array = new int[width, height];
					for (int i = 0; i < width; i++)
						for (int j = 0; j < height; j++)
							this._array[i, j] = 0;
					return;
				}

				int?[,] array = new int?[width, height];
				for (int i = 0; i < width; i++)
					for (int j = 0; j < height; j++)
						array[i, j] = null;

				while (list.Count > 0)
				{
					List<Tuple<int, int>> newList = new List<Tuple<int, int>>();
					int?[,] newArray = new int?[width, height];
					for (int i = 0; i < width; i++)
						for (int j = 0; j < height; j++)
							newArray[i, j] = array[i, j];

					foreach (Tuple<int, int> cell in list)
					{
						int i = cell.Item1;
						int j = cell.Item2;

						if (array[i, j] != null)
							continue;

						if (newArray[i, j] != null)
							continue;

						int iMinusOne = i - 1;
						if (iMinusOne < 0)
							iMinusOne += width;
						int iPlusOne = i + 1;
						if (iPlusOne >= width)
							iPlusOne -= width;
						int jMinusOne = j - 1;
						if (jMinusOne < 0)
							jMinusOne += height;
						int jPlusOne = j + 1;
						if (jPlusOne >= height)
							jPlusOne -= height;

						int? left = array[iMinusOne, j];
						int? right = array[iPlusOne, j];
						int? up = array[i, jPlusOne];
						int? down = array[i, jMinusOne];

						int? min = this.NullableMin(left, this.NullableMin(right, this.NullableMin(up, down)));

						if (min == null)
							newArray[i, j] = 0;
						else
							newArray[i, j] = min.Value + 1;

						newList.Add(new Tuple<int, int>(iMinusOne, j));
						newList.Add(new Tuple<int, int>(iPlusOne, j));
						newList.Add(new Tuple<int, int>(i, jMinusOne));
						newList.Add(new Tuple<int, int>(i, jPlusOne));
					}

					list = newList;
					array = newArray;
				}

				this._array = new int[width, height];
				for (int i = 0; i < width; i++)
					for (int j = 0; j < height; j++)
						this._array[i, j] = array[i, j].Value;
			}
		}

		private Direction GetMove(int i, int j, GameState gameState, DistanceToNearestNonPlayerCellArray distanceToNearestNonPlayerCellArray)
		{
			Cell cell = gameState.Cell(i, j);

			if (cell.Owner != Owner.Player)
				throw new Exception();

			Cell left = gameState.Cell(i - 1, j);
			Cell right = gameState.Cell(i + 1, j);
			Cell up = gameState.Cell(i, j + 1);
			Cell down = gameState.Cell(i, j - 1);

			if (cell.Strength == 0)
				return Direction.None;

			if (left.Owner != Owner.Player && left.Strength < cell.Strength)
				return Direction.Left;

			if (right.Owner != Owner.Player && right.Strength < cell.Strength)
				return Direction.Right;

			if (up.Owner != Owner.Player && up.Strength < cell.Strength)
				return Direction.Up;

			if (down.Owner != Owner.Player && down.Strength < cell.Strength)
				return Direction.Down;

			if (left.Owner == Owner.Player && right.Owner == Owner.Player && up.Owner == Owner.Player && down.Owner == Owner.Player)
			{
				if (cell.Strength < 5 * gameState.Board.Production(i, j))
					return Direction.None;

				int leftDistance = distanceToNearestNonPlayerCellArray.Distance(i - 1, j);
				int rightDistance = distanceToNearestNonPlayerCellArray.Distance(i + 1, j);
				int upDistance = distanceToNearestNonPlayerCellArray.Distance(i, j + 1);
				int downDistance = distanceToNearestNonPlayerCellArray.Distance(i, j - 1);
				int minDistance = Math.Min(leftDistance, Math.Min(rightDistance, Math.Min(upDistance, downDistance)));

				List<Direction> directions = new List<Direction>();

				if (minDistance == leftDistance)
					directions.Add(Direction.Left);
				if (minDistance == rightDistance)
					directions.Add(Direction.Right);
				if (minDistance == upDistance)
					directions.Add(Direction.Up);
				if (minDistance == downDistance)
					directions.Add(Direction.Down);

				int rand = this._random.Next(directions.Count);
				return directions[rand];
			}

			return Direction.None;
		}

		private Random _random;
		private bool _isInitialized;
		
		// This bot will never attack the location (_safeX, _safeY)
		// This ensures that the opponent will never be completely eliminated
		// (the game will eventually end due to the time limit, and the opponent
		// can still lose due to having less territory)
		//
		// It is possible that _safeX and _safeY might be null, in which case
		// there won't be any safe cell that the bot won't attack.
		private int? _safeX;
		private int? _safeY;
		
		// This bot will attempt to render the text "GG" onto the board.  The text
		// takes up 24 x 7 cells (24 cells wide and 7 cells tall)
		// The text will be rendered starting at (_startOfGGX, _startOfGGY) and ending
		// at (_startOfGGX + 23, _startOfGGY + 6)
		//
		// It is possible that these two values are null, meaning that the text will not
		// be rendered onto the screen.
		private int? _startOfGGX;
		private int? _startOfGGY;

		public HaliteAI(Random random)
		{
			this._random = random;
			this._isInitialized = false;
			this._safeX = null;
			this._safeY = null;
			this._startOfGGX = null;
			this._startOfGGY = null;
		}

		private void InitializeSafeXAndSafeY(GameState gameState)
		{
			int width = gameState.Board.Width;
			int height = gameState.Board.Height;

			if (width * height < 400)
				return;

			int? enemyPlayerId = null;
			for (int i = 0; i < width; i++)
				for (int j = 0; j < height; j++)
				{
					if (gameState.Cell(i, j).Owner == Owner.Enemy)
						enemyPlayerId = gameState.Cell(i, j).EnemyId.Value;
				}

			if (enemyPlayerId == null)
				return;

			int? safeX = null;
			int? safeY = null;
			int? safetyFactor = null;
			for (int i = 0; i < width; i++)
				for (int j = 0; j < height; j++)
				{
					int currentSafetyFactor = 0;
					// 2 is chosen arbitrarily
					// We try to choose an enemy cell that's as guarded as possible
					// (in the best case, it's surrounded by 24 of its allies in a 5x5 area)
					for (int a = i - 2; a <= i + 2; a++)
						for (int b = j - 2; b <= j + 2; b++)
						{
							if (gameState.Cell(a, b).Owner == Owner.Enemy && gameState.Cell(a, b).EnemyId.Value == enemyPlayerId.Value)
								currentSafetyFactor++;
						}

					if (safetyFactor == null || safetyFactor.Value < currentSafetyFactor)
					{
						safeX = i;
						safeY = j;
						safetyFactor = currentSafetyFactor;
					}
				}

			this._safeX = safeX.Value;
			this._safeY = safeY.Value;
		}

		private void InitializeStartOfGGXAndY(GameState gameState)
		{
			if (this._safeX == null || this._safeY == null)
				return;

			int width = gameState.Board.Width;
			int height = gameState.Board.Height;

			int? startOfGGX = null;
			int? startOfGGY = null;
			int? score = null;

			for (int i = 2; i < width; i++)
				for (int j = 2; j < height; j++)
				{
					/*
						24 x 7
					  
						GGGGGGGGGG    GGGGGGGGGG
						G			  G
						G       	  G       
						G    GGGGG	  G    GGGGG
						G        G    G        G
						G        G    G        G
						GGGGGGGGGG    GGGGGGGGGG
					*/
					// This code should ideally do wrap-around bound-checking, 
					// but we'll just not handle this case.  (In such an instance,
					// the GG text will probably be incorrectly rendered, although
					// the bot will not crash or otherwise fail.)
					if (i + 24 <= width && j + 7 <= height)
						if (i >= this._safeX.Value + 2 || i + 23 <= this._safeX.Value - 2 || j >= this._safeY.Value + 2 || j + 6 <= this._safeY.Value - 2)
						{
							int currentScore = 0;
							for (int a = i; a < i + 24; a++)
								for (int b = j; b < j + 7; b++)
								{
									if (gameState.Cell(a, b).Owner == Owner.Player)
										currentScore++;
								}
							for (int a = i - 5; a < i + 24 + 5; a++)
								for (int b = j - 5; b < j + 7 + 5; b++)
								{
									if (gameState.Cell(a, b).Owner == Owner.Player)
										currentScore++;
								}

							if (score == null || score.Value < currentScore)
							{
								startOfGGX = i;
								startOfGGY = j;
								score = currentScore;
							}
						}
				}

			if (score != null)
			{
				this._startOfGGX = startOfGGX.Value;
				this._startOfGGY = startOfGGY.Value;
			}
		}

		private void Initialize(GameState gameState)
		{
			if (this._isInitialized)
				return;

			this._isInitialized = true;

			this.InitializeSafeXAndSafeY(gameState);
			this.InitializeStartOfGGXAndY(gameState);
		}

		private Direction? CheckForViolationOfSafeXAndSafeY(Direction direction, int i, int j, GameState gameState)
		{
			int width = gameState.Board.Width;
			int height = gameState.Board.Height;

			if (this._safeX == null || this._safeY == null)
				return null;

			if (direction == Direction.None)
				return null;

			int di;
			int dj;
			if (direction == Direction.Left)
			{
				di = i - 1;
				dj = j;
			}
			else if (direction == Direction.Right)
			{
				di = i + 1;
				dj = j;
			}
			else if (direction == Direction.Up)
			{
				di = i;
				dj = j + 1;
			}
			else if (direction == Direction.Down)
			{
				di = i;
				dj = j - 1;
			}
			else
				throw new Exception();

			if (di < 0)
				di += width;
			if (di >= width)
				di -= width;
			if (dj < 0)
				dj += height;
			if (dj >= height)
				dj -= height;

			for (int a = this._safeX.Value - 1; a <= this._safeX.Value + 1; a++)
				for (int b = this._safeY.Value - 1; b <= this._safeY.Value + 1; b++)
				{
					int aa = a;
					int bb = b;
					if (aa < 0)
						aa += width;
					if (aa >= width)
						aa -= width;
					if (bb < 0)
						bb += height;
					if (bb >= height)
						bb -= height;

					if (di == aa && dj == bb)
						return Direction.None;
				}

			return null;
		}

		private Direction? CheckForViolationOfGG(Direction direction, int i, int j, GameState gameState, DistanceToNearestNonPlayerCellArray distanceToGGArray)
		{
			if (this._startOfGGX == null || this._startOfGGY == null)
				return null;

			/*
				24 x 7
			  
				GGGGGGGGGG    GGGGGGGGGG
				G			  G
				G       	  G       
				G    GGGGG	  G    GGGGG
				G        G    G        G
				G        G    G        G
				GGGGGGGGGG    GGGGGGGGGG
			*/
			if (this._startOfGGX.Value <= i && i < this._startOfGGX.Value + 24
				&& this._startOfGGY.Value <= j && j < this._startOfGGY.Value + 7)
			{
				if (distanceToGGArray.Distance(i, j) == 0)
					return Direction.None;

				int leftDistance = distanceToGGArray.Distance(i - 1, j);
				int rightDistance = distanceToGGArray.Distance(i + 1, j);
				int upDistance = distanceToGGArray.Distance(i, j + 1);
				int downDistance = distanceToGGArray.Distance(i, j - 1);
				int minDistance = Math.Min(leftDistance, Math.Min(rightDistance, Math.Min(upDistance, downDistance)));

				List<Direction> directions = new List<Direction>();

				if (minDistance == leftDistance)
					directions.Add(Direction.Left);
				if (minDistance == rightDistance)
					directions.Add(Direction.Right);
				if (minDistance == upDistance)
					directions.Add(Direction.Up);
				if (minDistance == downDistance)
					directions.Add(Direction.Down);

				int rand = this._random.Next(directions.Count);
				return directions[rand];
			}

			// This should ideally do wrap-around bounds-checking but we'll just not handle it.
			if (i == this._startOfGGX.Value - 1 && this._startOfGGY.Value - 1 <= j && j < this._startOfGGY.Value + 8)
			{
				return Direction.Left;
			}
			if (i == this._startOfGGX.Value + 24 && this._startOfGGY.Value - 1 <= j && j < this._startOfGGY.Value + 8)
			{
				return Direction.Right;
			}
			if (j == this._startOfGGY.Value - 1 && this._startOfGGX.Value - 1 <= i && i < this._startOfGGX.Value + 25)
			{
				return Direction.Down;
			}
			if (j == this._startOfGGY.Value + 7 && this._startOfGGX.Value - 1 <= i && i < this._startOfGGX.Value + 25)
			{
				return Direction.Up;
			}

			if (i == this._startOfGGX.Value - 2 && this._startOfGGY.Value - 1 <= j && j < this._startOfGGY.Value + 8)
			{
				return Direction.None;
			}
			if (i == this._startOfGGX.Value + 25 && this._startOfGGY.Value - 1 <= j && j < this._startOfGGY.Value + 8)
			{
				return Direction.None;
			}
			if (j == this._startOfGGY.Value - 2 && this._startOfGGX.Value - 1 <= i && i < this._startOfGGX.Value + 25)
			{
				return Direction.None;
			}
			if (j == this._startOfGGY.Value + 8 && this._startOfGGX.Value - 1 <= i && i < this._startOfGGX.Value + 25)
			{
				return Direction.None;
			}

			return null;
		}

		private Direction GetMoveAugmented(
			int i,
			int j,
			GameState gameState,
			DistanceToNearestNonPlayerCellArray distanceToNearestNonPlayerCellArray,
			DistanceToNearestNonPlayerCellArray distanceToGGArray,
			bool isBoardMostlyCaptured)
		{
			Direction direction = this.GetMove(i, j, gameState, distanceToNearestNonPlayerCellArray);

			Direction? check = this.CheckForViolationOfSafeXAndSafeY(direction, i, j, gameState);

			if (check != null)
				return check.Value;

			check = this.CheckForViolationOfGG(direction, i, j, gameState, distanceToGGArray);

			if (check != null && isBoardMostlyCaptured)
				return check.Value;

			return direction;
		}

		private bool IsBoardMostlyCaptured(GameState gameState)
		{
			int width = gameState.Board.Width;
			int height = gameState.Board.Height;

			int numUnowned = 0;
			for (int i = 0; i < width; i++)
				for (int j = 0; j < height; j++)
				{
					if (gameState.Cell(i, j).Owner == Owner.Unowned)
						numUnowned++;
				}

			return numUnowned < 10;
		}

		private DistanceToNearestNonPlayerCellArray CreateDistanceToGGArray(GameState gameState)
		{
			int width = gameState.Board.Width;
			int height = gameState.Board.Height;

			Cell[,] cells = new Cell[width, height];
			for (int i = 0; i < width; i++)
				for (int j = 0; j < height; j++)
					cells[i, j] = new Cell(Owner.Player, 1, null);

			if (this._startOfGGX == null || this._startOfGGY == null)
			{
				GameState fakeGameState = new GameState(gameState.Board, cells);
				return new DistanceToNearestNonPlayerCellArray(fakeGameState);
			}

			/*
				24 x 7
		  
				GGGGGGGGGG    GGGGGGGGGG
				G			  G
				G       	  G       
				G    GGGGG	  G    GGGGG
				G        G    G        G
				G        G    G        G
				GGGGGGGGGG    GGGGGGGGGG
			*/
			int[][] ggArray = new int[24][];
			ggArray[0] = new int[] { 1, 1, 1, 1, 1, 1, 1 };
			ggArray[1] = new int[] { 1, 0, 0, 0, 0, 0, 1 };
			ggArray[2] = new int[] { 1, 0, 0, 0, 0, 0, 1 };
			ggArray[3] = new int[] { 1, 0, 0, 0, 0, 0, 1 };
			ggArray[4] = new int[] { 1, 0, 0, 0, 0, 0, 1 };
			ggArray[5] = new int[] { 1, 0, 0, 1, 0, 0, 1 };
			ggArray[6] = new int[] { 1, 0, 0, 1, 0, 0, 1 };
			ggArray[7] = new int[] { 1, 0, 0, 1, 0, 0, 1 };
			ggArray[8] = new int[] { 1, 0, 0, 1, 0, 0, 1 };
			ggArray[9] = new int[] { 1, 1, 1, 1, 0, 0, 1 };
			ggArray[10] = new int[] { 0, 0, 0, 0, 0, 0, 0 };
			ggArray[11] = new int[] { 0, 0, 0, 0, 0, 0, 0 };
			ggArray[12] = new int[] { 0, 0, 0, 0, 0, 0, 0 };
			ggArray[13] = new int[] { 0, 0, 0, 0, 0, 0, 0 };
			ggArray[14] = new int[] { 1, 1, 1, 1, 1, 1, 1 };
			ggArray[15] = new int[] { 1, 0, 0, 0, 0, 0, 1 };
			ggArray[16] = new int[] { 1, 0, 0, 0, 0, 0, 1 };
			ggArray[17] = new int[] { 1, 0, 0, 0, 0, 0, 1 };
			ggArray[18] = new int[] { 1, 0, 0, 0, 0, 0, 1 };
			ggArray[19] = new int[] { 1, 0, 0, 1, 0, 0, 1 };
			ggArray[20] = new int[] { 1, 0, 0, 1, 0, 0, 1 };
			ggArray[21] = new int[] { 1, 0, 0, 1, 0, 0, 1 };
			ggArray[22] = new int[] { 1, 0, 0, 1, 0, 0, 1 };
			ggArray[23] = new int[] { 1, 1, 1, 1, 0, 0, 1 };

			for (int i = 0; i < ggArray.Length; i++)
				for (int j = 0; j < ggArray[i].Length; j++)
				{
					cells[this._startOfGGX.Value + i, this._startOfGGY.Value + j] =
						ggArray[i][j] == 1
							? new Cell(Owner.Unowned, 1, null)
							: new Cell(Owner.Player, 1, null);
				}

			return new DistanceToNearestNonPlayerCellArray(new GameState(gameState.Board, cells));
		}

		public List<Move> GetNextStep(GameState gameState)
		{
			this.Initialize(gameState);

			List<Move> moves = new List<Move>();

			bool isBoardMostlyCaptured = this.IsBoardMostlyCaptured(gameState);

			DistanceToNearestNonPlayerCellArray distanceToNearestNonPlayerCellArray = new DistanceToNearestNonPlayerCellArray(gameState);
			DistanceToNearestNonPlayerCellArray distanceToGGArray = this.CreateDistanceToGGArray(gameState);

			for (int i = 0; i < gameState.Board.Width; i++)
				for (int j = 0; j < gameState.Board.Height; j++)
				{
					Cell cell = gameState.Cell(i, j);

					if (cell.Owner == Owner.Player)
					{
						Direction direction = this.GetMoveAugmented(i, j, gameState, distanceToNearestNonPlayerCellArray, distanceToGGArray, isBoardMostlyCaptured);

						moves.Add(new Move(i, j, direction));
					}
				}

			return moves;
		}
	}

	public class MainClass
	{
		// Reads in a line of input from the standard input stream.
		// The returned string will NOT contain the newline character.
		//
		// Returns null if we reach the end of the input.
		private static string ReadLine()
		{
			TextReader stdIn = Console.In;

			List<char> line = new List<char>();

			while (true)
			{
				int i = stdIn.Read();

				if (i == -1)
				{
					if (line.Count > 0)
						return new string(line.ToArray());
					else
						return null;
				}

				char c = (char) i;
				if (c == '\r')
					continue;
				if (c == '\n')
				{
					return new string(line.ToArray());
				}

				line.Add(c);
			}
		}

		// Writes the string to the standard output stream.
		//
		// This function doesn't flush the standard output stream,
		// so the caller is responsible for making sure that happens
		// if necessary.
		private static void Write(string s)
		{
			TextWriter stdOut = Console.Out;

			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				stdOut.Write(c);
			}
		}
		
		// Writes the string to the standard output stream, and then
		// appends a newline character.
		//
		// This function will also flush the standard output stream afterwards.
		private static void WriteLine(string s)
		{
			Write(s);
			TextWriter stdOut = Console.Out;
			stdOut.Write('\n');
			stdOut.Flush();
		}
		
		private static void WriteLine()
		{
			WriteLine("");
		}

		private static Cell[,] GetCells(string s, int width, int height, int playerId)
		{
			LinkedList<string> tokens = new LinkedList<string>(s.Split(' '));
			int counterSum = 0;

			int[,] ownerArray = new int[width, height];
			int currentI = 0;
			int currentJ = height - 1;
			while (true)
			{
				if (counterSum == width * height)
					break;
				int counter = int.Parse(tokens.First.Value, CultureInfo.InvariantCulture);
				tokens.RemoveFirst();
				int owner = int.Parse(tokens.First.Value, CultureInfo.InvariantCulture);
				tokens.RemoveFirst();

				counterSum += counter;

				for (int z = 0; z < counter; z++)
				{
					ownerArray[currentI, currentJ] = owner;
					currentI++;
					if (currentI == width)
					{
						currentI = 0;
						currentJ--;
					}
				}
			}

			if (currentI != 0 || currentJ != -1)
				throw new Exception();

			List<string> tokensAsList = new List<string>(tokens);
				
			int[,] strengthArray = new int[width, height];
			int x = 0;
			for (int j = height - 1; j >= 0; j--)
				for (int i = 0; i < width; i++)
				{
					strengthArray[i, j] = int.Parse(tokensAsList[x], CultureInfo.InvariantCulture);
					x++;
				}

			Cell[,] cells = new Cell[width, height];
			for (int i = 0; i < width; i++)
				for (int j = 0; j < height; j++)
				{
					Owner owner;
					if (ownerArray[i, j] == 0)
						owner = Owner.Unowned;
					else if (ownerArray[i, j] == playerId)
						owner = Owner.Player;
					else
						owner = Owner.Enemy;

					int? enemyId;
					if (owner == Owner.Enemy)
						enemyId = ownerArray[i, j];
					else
						enemyId = null;
					
					cells[i, j] = new Cell(
						owner: owner,
						strength: strengthArray[i, j],
						enemyId: enemyId);
				}

			return cells;
		}
		
		private static int[,] GetProductionArray(string s, int width, int height)
		{
			string[] tokens = s.Split(' ');
			
			if (tokens.Length != width * height)
				throw new Exception();
		
			int x = 0;
			int[,] productionArray = new int[width, height];
			for (int j = height - 1; j >= 0; j--)
				for (int i = 0; i < width; i++)
				{
					productionArray[i, j] = int.Parse(tokens[x], CultureInfo.InvariantCulture);
					x++;
				}
			
			return productionArray;
		}

		public static void Main(string[] args)
		{
			string line1 = ReadLine().TrimEnd(' ');
			int playerId = int.Parse(line1, CultureInfo.InvariantCulture);
			
			string[] line2 = ReadLine().TrimEnd(' ').Split(' ');
			int width = int.Parse(line2[0], CultureInfo.InvariantCulture);
			int height = int.Parse(line2[1], CultureInfo.InvariantCulture);

			string line3 = ReadLine().TrimEnd(' ');
			int[,] productionArray = GetProductionArray(line3, width, height);
			Board board = new Board(productionArray);

			string line4 = ReadLine().TrimEnd(' ');
			Cell[,] cells = GetCells(line4, width, height, playerId);

			GameState gameState = new GameState(board, cells);

			WriteLine(Constants.Name);

			IHaliteAI ai = new HaliteAI(new Random());
			while (true)
			{
				string s = ReadLine();
				if (s == null)
					break;
					
				s = s.TrimEnd(' ');
				
				if (s.Length == 0)
					break;
					
				Cell[,] currentCells = GetCells(s, width, height, playerId);
				gameState = new GameState(board, currentCells);
				List<Move> moves = ai.GetNextStep(gameState);

				List<string> output = new List<string>();
				foreach (Move move in moves)
				{
					int x = move.X;
					int y = height - move.Y - 1;
					string direction;
					if (move.Direction == Direction.Up)
						direction = "1";
					else if (move.Direction == Direction.Down)
						direction = "3";
					else if (move.Direction == Direction.Left)
						direction = "4";
					else if (move.Direction == Direction.Right)
						direction = "2";
					else if (move.Direction == Direction.None)
						direction = "0";
					else
						throw new Exception();

					output.Add(x.ToString(CultureInfo.InvariantCulture));
					output.Add(y.ToString(CultureInfo.InvariantCulture));
					output.Add(direction);
				}

				for (int z = 0; z < output.Count; z++)
				{
					if (z != 0)
						Write(" ");
					Write(output[z]);
				}
				WriteLine();
			}
		}
	}
}
