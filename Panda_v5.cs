/*
	Compile using the C# compiler (csc.exe):
	
		csc.exe /out:PandaV5.exe Panda_v5.cs
*/

namespace PandaV5
{
	using System.Collections.Generic;
	using System;
	using System.IO;
	using System.Globalization;

	public class Constants
	{
		public static string Name = "PandaV5";
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

			IHaliteAI ai = new HaliteAI();
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
	
	public class HaliteAI : IHaliteAI
	{
		private class DistanceToNearestNonPlayerCellArray
		{
			private int[,] _array;
			private int[,] _aggregateArray;
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

			private void ComputeAggregateArray()
			{
				if (this._aggregateArray != null)
					return;

				this._aggregateArray = new int[this._width, this._height];

				for (int i = 0; i < this._width; i++)
					for (int j = 0; j < this._height; j++)
					{
						int sum = 0;
						for (int a = i - 2; a <= i + 2; a++)
							for (int b = j - 2; b <= j + 2; b++)
								sum += this.Distance(a, b);

						this._aggregateArray[i, j] = sum;
					}
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

			public int AggregateDistance(int x, int y)
			{
				this.ComputeAggregateArray();

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

				return this._aggregateArray[x, y];
			}

			public DistanceToNearestNonPlayerCellArray(GameState gameState)
			{
				int width = gameState.Board.Width;
				int height = gameState.Board.Height;

				this._width = width;
				this._height = height;
				this._aggregateArray = null;

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

		private Random _random;

		public HaliteAI()
		{
			this._random = new Random();
		}

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

		private int? AmountOfAdditionalStrengthRequired(
			int i,
			int j,
			GameState gameState)
		{
			Cell cell = gameState.Cell(i, j);

			if (cell.Owner != Owner.Player)
				return null;

			Cell left = gameState.Cell(i - 1, j);
			Cell right = gameState.Cell(i + 1, j);
			Cell up = gameState.Cell(i, j + 1);
			Cell down = gameState.Cell(i, j - 1);

			int? amount = null;

			if (left.Owner == Owner.Unowned)
				amount = this.NullableMin(amount, left.Strength - cell.Strength - gameState.Board.Production(i, j));
			if (right.Owner == Owner.Unowned)
				amount = this.NullableMin(amount, right.Strength - cell.Strength - gameState.Board.Production(i, j));
			if (up.Owner == Owner.Unowned)
				amount = this.NullableMin(amount, up.Strength - cell.Strength - gameState.Board.Production(i, j));
			if (down.Owner == Owner.Unowned)
				amount = this.NullableMin(amount, down.Strength - cell.Strength - gameState.Board.Production(i, j));

			if (amount != null && amount.Value <= 0)
				return null;

			return amount;
		}


		private Direction GetMove(
			int i,
			int j,
			GameState gameState,
			DistanceToNearestNonPlayerCellArray distanceToNearestNonPlayerCellArray,
			NewStrengthArray newStrengthArray)
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

			// Note that we handle the edge case where the other cell is at strength 255
			if (left.Owner == Owner.Unowned && left.Strength > 0 && (left.Strength < cell.Strength || cell.Strength == 255 && left.Strength == 255))
				return Direction.Left;

			if (right.Owner == Owner.Unowned && right.Strength > 0 && (right.Strength < cell.Strength || cell.Strength == 255 && right.Strength == 255))
				return Direction.Right;

			if (up.Owner == Owner.Unowned && up.Strength > 0 && (up.Strength < cell.Strength || cell.Strength == 255 && up.Strength == 255))
				return Direction.Up;

			if (down.Owner == Owner.Unowned && down.Strength > 0 && (down.Strength < cell.Strength || cell.Strength == 255 && down.Strength == 255))
				return Direction.Down;

			if (left.Owner != Owner.Enemy
				&& right.Owner != Owner.Enemy
				&& up.Owner != Owner.Enemy
				&& down.Owner != Owner.Enemy
				&& (
					left.Owner == Owner.Unowned && left.Strength > 0
						||
					right.Owner == Owner.Unowned && right.Strength > 0
						||
					up.Owner == Owner.Unowned && up.Strength > 0
						||
					down.Owner == Owner.Unowned && down.Strength > 0))
			{
				bool noEnemyNearby = true;
				for (int a = i - 2; a <= i + 2; a++)
					for (int b = j - 2; b <= j + 2; b++)
					{
						if (gameState.Cell(a, b).Owner == Owner.Enemy)
							noEnemyNearby = false;
					}

				if ((i + j) % 2 == 0 && noEnemyNearby)
				{
					int? requiredStrength = null;
					if (left.Owner == Owner.Unowned && left.Strength > 0)
						requiredStrength = this.NullableMin(requiredStrength, left.Strength);
					if (right.Owner == Owner.Unowned && right.Strength > 0)
						requiredStrength = this.NullableMin(requiredStrength, right.Strength);
					if (up.Owner == Owner.Unowned && up.Strength > 0)
						requiredStrength = this.NullableMin(requiredStrength, up.Strength);
					if (down.Owner == Owner.Unowned && down.Strength > 0)
						requiredStrength = this.NullableMin(requiredStrength, down.Strength);

					if (requiredStrength == null)
						throw new Exception();

					int estimatedNumTurns;
					if (gameState.Board.Production(i, j) == 0)
						estimatedNumTurns = 999;
					else
						estimatedNumTurns = (requiredStrength.Value - cell.Strength) / gameState.Board.Production(i, j) + 1;

					if (estimatedNumTurns >= 3 && cell.Strength >= 4 * gameState.Board.Production(i, j))
					{
						if (gameState.Cell(i - 1, j).Owner == Owner.Player
								&& distanceToNearestNonPlayerCellArray.Distance(i - 1, j) == 1
								&& this.AmountOfAdditionalStrengthRequired(i - 1, j, gameState) <= cell.Strength)
							return Direction.Left;
						if (gameState.Cell(i + 1, j).Owner == Owner.Player
								&& distanceToNearestNonPlayerCellArray.Distance(i + 1, j) == 1
								&& this.AmountOfAdditionalStrengthRequired(i + 1, j, gameState) <= cell.Strength)
							return Direction.Right;
						if (gameState.Cell(i, j + 1).Owner == Owner.Player
								&& distanceToNearestNonPlayerCellArray.Distance(i, j + 1) == 1
								&& this.AmountOfAdditionalStrengthRequired(i, j + 1, gameState) <= cell.Strength)
							return Direction.Up;
						if (gameState.Cell(i, j - 1).Owner == Owner.Player
								&& distanceToNearestNonPlayerCellArray.Distance(i, j - 1) == 1
								&& this.AmountOfAdditionalStrengthRequired(i, j - 1, gameState) <= cell.Strength)
							return Direction.Down;

						return Direction.None;
					}
				}
			}

			List<Direction> enemyDirections = new List<Direction>();
			if (left.Owner == Owner.Enemy || left.Owner == Owner.Unowned && left.Strength == 0)
				enemyDirections.Add(Direction.Left);

			if (right.Owner == Owner.Enemy || right.Owner == Owner.Unowned && right.Strength == 0)
				enemyDirections.Add(Direction.Right);

			if (up.Owner == Owner.Enemy || up.Owner == Owner.Unowned && up.Strength == 0)
				enemyDirections.Add(Direction.Up);

			if (down.Owner == Owner.Enemy || down.Owner == Owner.Unowned && down.Strength == 0)
				enemyDirections.Add(Direction.Down);

			if (enemyDirections.Count > 0)
			{
				Direction? preferredDirection = null;
				int? preferredDistance = null;
				foreach (Direction direction in enemyDirections)
				{
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

					int distance = distanceToNearestNonPlayerCellArray.AggregateDistance(di, dj);
					if (preferredDistance == null || preferredDistance.Value > distance)
					{
						preferredDistance = distance;
						preferredDirection = direction;
					}
				}

				return preferredDirection.Value;
			}

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

				if (minDistance == leftDistance
					&& (newStrengthArray.NewStrength(i - 1, j) ?? 0) + cell.Strength <= 400)
					directions.Add(Direction.Left);
				if (minDistance == rightDistance
					&& (newStrengthArray.NewStrength(i + 1, j) ?? 0) + cell.Strength <= 400)
					directions.Add(Direction.Right);
				if (minDistance == upDistance
					&& (newStrengthArray.NewStrength(i, j + 1) ?? 0) + cell.Strength <= 400)
					directions.Add(Direction.Up);
				if (minDistance == downDistance
					&& (newStrengthArray.NewStrength(i, j - 1) ?? 0) + cell.Strength <= 400)
					directions.Add(Direction.Down);

				if (directions.Count == 0)
				{
					if ((newStrengthArray.NewStrength(i - 1, j) ?? 0) + cell.Strength <= 400)
						directions.Add(Direction.Left);
					if ((newStrengthArray.NewStrength(i + 1, j) ?? 0) + cell.Strength <= 400)
						directions.Add(Direction.Right);
					if ((newStrengthArray.NewStrength(i, j + 1) ?? 0) + cell.Strength <= 400)
						directions.Add(Direction.Up);
					if ((newStrengthArray.NewStrength(i, j - 1) ?? 0) + cell.Strength <= 400)
						directions.Add(Direction.Down);

					if ((newStrengthArray.NewStrength(i, j) ?? 0) + cell.Strength <= 255)
						return Direction.None;

					if (directions.Count > 0)
						return directions[this._random.Next(directions.Count)];

					return Direction.None;
				}

				Direction? preferredDirection = null;
				int? preferredDistance = null;
				foreach (Direction direction in directions)
				{
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

					int distance = distanceToNearestNonPlayerCellArray.AggregateDistance(di, dj);
					if (preferredDistance == null || preferredDistance.Value > distance)
					{
						preferredDistance = distance;
						preferredDirection = direction;
					}
				}

				return preferredDirection.Value;
			}

			return Direction.None;
		}

		private class NewStrengthArray
		{
			private int?[,] _array;
			private int _width;
			private int _height;

			public NewStrengthArray(GameState gameState)
			{
				int width = gameState.Board.Width;
				int height = gameState.Board.Height;

				this._width = width;
				this._height = height;

				this._array = new int?[width, height];
				for (int i = 0; i < width; i++)
					for (int j = 0; j < height; j++)
						this._array[i, j] = null;
			}

			public int? NewStrength(int x, int y)
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

			public void ApplyMove(GameState gameState, Move move)
			{
				int i = move.X;
				int j = move.Y;
				Direction direction = move.Direction;

				if (gameState.Cell(i, j).Owner != Owner.Player)
					throw new Exception();

				int width = gameState.Board.Width;
				int height = gameState.Board.Height;

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
				else if (direction == Direction.None)
				{
					di = i;
					dj = j;
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

				if (this._array[di, dj] == null)
					this._array[di, dj] = gameState.Cell(i, j).Strength;
				else
					this._array[di, dj] = this._array[di, dj].Value + gameState.Cell(i, j).Strength;

				if (direction == Direction.None)
					this._array[di, dj] = this._array[di, dj].Value + gameState.Board.Production(i, j);
			}
		}

		public List<Move> GetNextStep(GameState gameState)
		{
			int width = gameState.Board.Width;
			int height = gameState.Board.Height;

			List<Move> moves = new List<Move>();

			DistanceToNearestNonPlayerCellArray distanceToNearestNonPlayerCellArray = new DistanceToNearestNonPlayerCellArray(gameState);

			NewStrengthArray newStrengthArray = new NewStrengthArray(gameState);

			for (int i = 0; i < width; i++)
				for (int j = 0; j < height; j++)
				{
					Cell cell = gameState.Cell(i, j);

					if (cell.Owner == Owner.Player)
					{
						Direction direction = this.GetMove(i, j, gameState, distanceToNearestNonPlayerCellArray, newStrengthArray);
						Move move = new Move(i, j, direction);
						moves.Add(move);
						newStrengthArray.ApplyMove(gameState, move);
					}
				}

			return moves;
		}
	}
}
