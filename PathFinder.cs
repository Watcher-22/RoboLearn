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
            this.gridSize = gameField.Field.GetLength(0);
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
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    if (gameField.Field[x, y] == FieldType.Goal)
                    {
                        return new Point(x, y);
                    }
                }
            }
            return Point.Empty;
        }

        public List<(Point position, Direction facing)> FindOptimalPath()
        {
            var path = new List<(Point position, Direction facing)>();
            var currentPos = new Point(gameField.Bot.X, gameField.Bot.Y);
            var currentDir = gameField.Bot.Facing;
            var remainingCoins = new List<Point>(coins);

            while (remainingCoins.Count > 0 || currentPos != goal)
            {
                if (remainingCoins.Count > 0)
                {
                    // Finde nächste Münze
                    var nextCoin = FindNearestPoint(currentPos, remainingCoins);
                    var coinPath = FindPathTo(currentPos, nextCoin, currentDir);
                    path.AddRange(coinPath);

                    
                    currentPos = nextCoin;
                    currentDir = path[path.Count - 1].facing;
                    remainingCoins.Remove(nextCoin);
                }
                else
                {
                    // Zum Ziel gehen
                    var goalPath = FindPathTo(currentPos, goal, currentDir);
                    path.AddRange(goalPath);
                    break;
                }
            }

            return path;
        }

        private Point FindNearestPoint(Point start, List<Point> targets)
        {
            return targets.OrderBy(t =>
                Math.Abs(t.X - start.X) + Math.Abs(t.Y - start.Y)).First();
        }

        private List<(Point position, Direction facing)> FindPathTo(
            Point start, Point target, Direction startFacing)
        {
            var path = new List<(Point position, Direction facing)>();
            var visited = new HashSet<Point>();
            var queue = new PriorityQueue();

            queue.Enqueue(new PathNode(start, startFacing, 0, new List<(Point, Direction)>()));
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current.Position == target)
                {
                    return current.Path;
                }

                foreach (var next in GetNextMoves(current))
                {
                    if (!visited.Contains(next.Position))
                    {
                        visited.Add(next.Position);
                        queue.Enqueue(next);
                    }
                }
            }

            return path;
        }

        private List<PathNode> GetNextMoves(PathNode node)
        {
            var moves = new List<PathNode>();
            var directions = Enum.GetValues(typeof(Direction)).Cast<Direction>();

            foreach (var dir in directions)
            {
                
                int turnCost = CalculateTurnCost(node.Facing, dir);

                
                var newPos = GetNextPosition(node.Position, dir);
                if (IsValidMove(newPos))
                {
                    var newPath = new List<(Point, Direction)>(node.Path);

                    // Füge Drehungen hinzu wenn nötig
                    if (turnCost > 0)
                    {
                        newPath.Add((node.Position, dir));
                    }

                    // Füge Bewegung hinzu
                    newPath.Add((newPos, dir));

                    moves.Add(new PathNode(
                        newPos,
                        dir,
                        node.Cost + turnCost + 1,
                        newPath
                    ));
                }
            }

            return moves;
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

        private bool IsValidMove(Point pos)
        {
            return pos.X >= 0 && pos.X < gridSize &&
                   pos.Y >= 0 && pos.Y < gridSize &&
                   gameField.Field[pos.X, pos.Y] != FieldType.Wall;
        }

        private class PathNode : IComparable<PathNode>
        {
            public Point Position { get; }
            public Direction Facing { get; }
            public int Cost { get; }
            public List<(Point position, Direction facing)> Path { get; }

            public PathNode(Point pos, Direction facing, int cost,
                List<(Point, Direction)> path)
            {
                Position = pos;
                Facing = facing;
                Cost = cost;
                Path = path;
            }

            public int CompareTo(PathNode other)
            {
                return Cost.CompareTo(other.Cost);
            }
        }
    }
}
