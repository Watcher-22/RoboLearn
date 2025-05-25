using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Open_Day
{
    public class PathFinder
    {
        private class PriorityQueue
        {
            private List<PathNode> elements = new List<PathNode>();
            public int Count => elements.Count;

            public void Enqueue(PathNode item)
            {
                elements.Add(item);
                elements.Sort((a, b) => a.Cost.CompareTo(b.Cost));
            }

            public PathNode Dequeue()
            {
                if (elements.Count == 0)
                    throw new InvalidOperationException("Queue is empty");
                var item = elements[0];
                elements.RemoveAt(0);
                return item;
            }
        }

        private GameField gameField;
        private List<Point> coins;
        private Point goal;
        private int gridSize;

        public PathFinder(GameField gameField)
        {
            this.gameField = gameField;
            this.gridSize = Form1.GRID_SIZE;
            this.coins = FindCoins();
            this.goal = FindGoal();
        }

        private List<Point> FindCoins()
        {
            var coins = new List<Point>();
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    if (gameField.Field[x, y] == FieldType.Coin)
                    {
                        coins.Add(new Point(x, y));
                    }
                }
            }
            return coins;
        }

        private Point FindGoal()
        {
                for (int y = 0; y < gridSize; y++)
                {
                    if (gameField.Field[Form1.GRID_SIZE - 1, y] == FieldType.Goal)
                {
                        return new Point(Form1.GRID_SIZE - 1, y);
                    }
                }
            return Point.Empty;
        }

        public List<(Point position, Direction facing)> FindOptimalPath()
        {
            var startPos = new Point(gameField.Bot.X, gameField.Bot.Y);
            var currentDir = gameField.Bot.Facing;

            var queue = new PriorityQueue();
            var initialCoins = new HashSet<Point>();
            // Check if starting position is a coin
            if (gameField.Field[startPos.X, startPos.Y] == FieldType.Coin)
            {
                initialCoins.Add(startPos);
            }
            queue.Enqueue(new PathNode(startPos, currentDir, 0, new List<(Point, Direction)>(), initialCoins));

            var visited = new HashSet<(Point Position, Direction Facing, string uniqueCoinRepresentation)>();

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                var hashableCollectedCoinsKey = GetHashableCoinsString(current.CollectedCoins);
                var visitedKey = (current.Position, current.Facing, hashableCollectedCoinsKey);

                if (visited.Contains(visitedKey))
                {
                    continue;
                }
                visited.Add(visitedKey);

                // Goal Condition
                bool allCoinsCollected = this.coins.All(c => current.CollectedCoins.Contains(c));
                if (current.Position.Equals(this.goal) && allCoinsCollected)
                {
                    return current.Path;
                }

                // Expand Node
                // The isGoal parameter for GetNextMoves determines if the goal tile can be entered.
                // It should only be true if all coins are collected.
                foreach (var nextMove in GetNextMoves(current, allCoinsCollected))
                {
                    queue.Enqueue(nextMove);
                }
            }

            return new List<(Point position, Direction facing)>(); // No path found
        }

        private List<PathNode> GetNextMoves(PathNode node, bool canEnterGoal)
        {
            var moves = new List<PathNode>();
            var directions = Enum.GetValues(typeof(Direction)).Cast<Direction>();

            foreach (var dir in directions)
            {
                int turnCost = CalculateTurnCost(node.Facing, dir);
                var newPos = GetNextPosition(node.Position, dir);

                if (IsValidMove(newPos, canEnterGoal))
                {
                    var newPath = new List<(Point, Direction)>(node.Path);
                    if (turnCost > 0)
                    {
                        newPath.Add((node.Position, dir));
                    }
                    newPath.Add((newPos, dir));
                    var newCollectedCoins = new HashSet<Point>(node.CollectedCoins);
                    if (gameField.Field[newPos.X, newPos.Y] == FieldType.Coin)
                    {
                        newCollectedCoins.Add(newPos);
                    }

                    moves.Add(new PathNode(
                        newPos,
                        dir,
                        node.Cost + turnCost + 1,
                        newPath,
                        newCollectedCoins
                    ));
                }
            }
            return moves;
        }

        private bool IsValidMove(Point pos, bool canEnterGoalTile)
        {
            if (pos.X < 0 || pos.X >= gridSize || pos.Y < 0 || pos.Y >= gridSize)
                return false;

            if (gameField.Field[pos.X, pos.Y] == FieldType.Wall)
                return false;

            // Goal tile is treated as a wall if not all coins are collected OR if canEnterGoalTile is false
            if (gameField.Field[pos.X, pos.Y] == FieldType.Goal && !canEnterGoalTile)
                return false;

            return true;
        }

        private int CalculateTurnCost(Direction current, Direction target)
        {
            if (current == target) return 0;
            int diff = ((int)target - (int)current + 4) % 4;
            if (diff > 2) diff = 4 - diff;
            return diff;
        }

        private Point GetNextPosition(Point pos, Direction dir)
        {
            switch (dir)
            {
                case Direction.Up: return new Point(pos.X, pos.Y - 1);
                case Direction.Right: return new Point(pos.X + 1, pos.Y);
                case Direction.Down: return new Point(pos.X, pos.Y + 1);
                case Direction.Left: return new Point(pos.X - 1, pos.Y);
                default: return pos;
            }
        }

        private string GetHashableCoinsString(HashSet<Point> coins)
        {
            if (coins == null || coins.Count == 0) return "";
            // Order by X then Y to ensure consistent string representation
            return string.Join(";", coins.OrderBy(p => p.X).ThenBy(p => p.Y).Select(p => $"{p.X},{p.Y}"));
        }

        private class PathNode : IComparable<PathNode>
        {
            public Point Position { get; }
            public Direction Facing { get; }
            public int Cost { get; }
            public List<(Point position, Direction facing)> Path { get; }
    public HashSet<Point> CollectedCoins { get; }

            public PathNode(Point pos, Direction facing, int cost,
        List<(Point, Direction)> path, HashSet<Point> collectedCoins)
            {
                Position = pos;
                Facing = facing;
                Cost = cost;
                Path = path;
        CollectedCoins = collectedCoins;
            }

            public int CompareTo(PathNode other)
            {
                return Cost.CompareTo(other.Cost);
            }
        }
    }
}
