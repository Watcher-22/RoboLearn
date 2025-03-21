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

        private List<Point> CalculateOptimalCoinOrder(Point start, List<Point> remainingCoins)
        {
            if (!remainingCoins.Any())
                return new List<Point>();

            var optimalOrder = new List<Point>();
            var currentPos = start;
            var unvisitedCoins = new List<Point>(remainingCoins);

            while (unvisitedCoins.Count > 0)
            {
                var distances = unvisitedCoins.Select(coin =>
                {
                    var pathLength = FindPathTo(currentPos, coin, gameField.Bot.Facing, false).Count;
                    return (coin, pathLength);
                });

                var nearest = distances.OrderBy(x => x.pathLength).First().coin;
                optimalOrder.Add(nearest);
                currentPos = nearest;
                unvisitedCoins.Remove(nearest);
            }

            return optimalOrder;
        }

        public List<(Point position, Direction facing)> FindOptimalPath()
        {
            var completePath = new List<(Point position, Direction facing)>();
            var startPos = new Point(gameField.Bot.X, gameField.Bot.Y);
            var currentDir = gameField.Bot.Facing;

            
            var optimalCoinOrder = CalculateOptimalCoinOrder(startPos, coins);

            // Optimaler Pfad
            var currentPos = startPos;
            foreach (var coinPos in optimalCoinOrder)
            {
                var pathToCoin = FindPathTo(currentPos, coinPos, currentDir, false);
                completePath.AddRange(pathToCoin);
                currentPos = coinPos;
                currentDir = completePath[completePath.Count - 1].facing;
            }

            // Erst ins Ziel, wenn alle Münzen
            var pathToGoal = FindPathTo(currentPos, goal, currentDir, true);
            completePath.AddRange(pathToGoal);

            return completePath;
        }

        private List<(Point position, Direction facing)> FindPathTo(
            Point start, Point target, Direction startFacing, bool isGoal)
        {
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

                foreach (var next in GetNextMoves(current, isGoal))
                {
                    if (!visited.Contains(next.Position))
                    {
                        visited.Add(next.Position);
                        queue.Enqueue(next);
                    }
                }
            }

            return new List<(Point position, Direction facing)>();
        }

        private List<PathNode> GetNextMoves(PathNode node, bool isGoal)
        {
            var moves = new List<PathNode>();
            var directions = Enum.GetValues(typeof(Direction)).Cast<Direction>();

            foreach (var dir in directions)
            {
                int turnCost = CalculateTurnCost(node.Facing, dir);
                var newPos = GetNextPosition(node.Position, dir);

                if (IsValidMove(newPos, isGoal))
                {
                    var newPath = new List<(Point, Direction)>(node.Path);
                    if (turnCost > 0)
                    {
                        newPath.Add((node.Position, dir));
                    }
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

        private bool IsValidMove(Point pos, bool isGoal)
        {
            if (pos.X < 0 || pos.X >= gridSize || pos.Y < 0 || pos.Y >= gridSize)
                return false;

            if (gameField.Field[pos.X, pos.Y] == FieldType.Wall)
                return false;

            // Ziel wird wie Wand behandelt
            if (!isGoal && gameField.Field[pos.X, pos.Y] == FieldType.Goal)
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
