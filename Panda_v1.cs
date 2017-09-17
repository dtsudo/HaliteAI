/*
	Compile using the C# compiler (csc.exe):
	
		csc.exe /out:PandaV1.exe Panda_v1.cs
*/

namespace PandaV1
{
	using System.Collections.Generic;
	using System;
	using System.IO;
	using System.Globalization;

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
		private Random _random;

		public HaliteAI()
		{
			this._random = new Random();
		}

		public List<Move> GetNextStep(GameState gameState)
		{
			List<Move> moves = new List<Move>();

			for (int i = 0; i < gameState.Board.Width; i++)
				for (int j = 0; j < gameState.Board.Height; j++)
				{
					if (gameState.Cell(i, j).Owner == Owner.Player)
					{
						int rand = this._random.Next(5);
						Direction direction;
						if (rand == 0)
							direction = Direction.Up;
						else if (rand == 1)
							direction = Direction.Down;
						else if (rand == 2)
							direction = Direction.Left;
						else if (rand == 3)
							direction = Direction.Right;
						else if (rand == 4)
							direction = Direction.None;
						else
							throw new Exception();
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

			WriteLine("dtsudo");

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
}
