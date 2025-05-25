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
        private static long nextNodeId = 0;

        private class PriorityQueue
        {
            private SortedSet<PathNode> elements = new SortedSet<PathNode>();
            public int Count => elements.Count;

            public void Enqueue(PathNode item)
            {
                elements.Add(item);
            }

            public PathNode Dequeue()
            {
                if (elements.Count == 0)
                    throw new InvalidOperationException("Queue is empty");
                var item = elements.Min; 
                elements.Remove(item);
                return item;
            }

            public bool IsEmpty => elements.Count == 0;
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

        public List<(Point position, Direction facing)> FindPathWithGreedyStrategy()
        {
            var completePath = new List<(Point position, Direction facing)>();
            Point currentActualPos = new Point(gameField.Bot.X, gameField.Bot.Y);
            Direction currentActualDir = gameField.Bot.Facing;

            var remainingCoins = new List<Point>(this.coins);

            while (remainingCoins.Count > 0)
            {
                Point nearestCoin = Point.Empty;
                List<(Point position, Direction facing)> shortestPathToCoin = null;
                int minPathLength = int.MaxValue;

                foreach (var coinPos in remainingCoins)
                {
                    // false for canEnterGoalTile: don't treat coin as goal if it happens to be on goal tile
                    var pathToCoin = FindPathBetweenPoints(currentActualPos, currentActualDir, coinPos, false); 

                    if (pathToCoin != null && pathToCoin.Count > 0)
                    {
                        if (pathToCoin.Count < minPathLength)
                        {
                            minPathLength = pathToCoin.Count;
                            nearestCoin = coinPos;
                            shortestPathToCoin = pathToCoin;
                        }
                    }
                }

                if (shortestPathToCoin == null || nearestCoin == Point.Empty)
                {
                    // No path to any of the remaining coins
                    break; 
                }

                completePath.AddRange(shortestPathToCoin);
                currentActualPos = nearestCoin;
                // Ensure shortestPathToCoin is not empty before accessing Last()
                if (shortestPathToCoin.Any()) 
                {
                    currentActualDir = shortestPathToCoin.Last().facing;
                }
                remainingCoins.Remove(nearestCoin);
            }

            // After collecting all reachable coins, go to the goal
            // true for canEnterGoalTile: allow entering the goal tile
            var pathToGoal = FindPathBetweenPoints(currentActualPos, currentActualDir, this.goal, true); 
            if (pathToGoal != null && pathToGoal.Count > 0)
            {
                completePath.AddRange(pathToGoal);
            }

            return completePath;
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
            return this.FindPathWithGreedyStrategy();
        }

        private List<(Point position, Direction facing)> FindPathBetweenPoints(
            Point startPos,
            Direction startDir,
            Point targetPos,
            bool canEnterGoalTile) // Used to determine if the targetPos (if it's the main goal) can be entered
        {
            var queue = new PriorityQueue();
            // Initial PathNode: cost 0, empty path, empty (or ignored) coin set
            queue.Enqueue(new PathNode(startPos, startDir, 0, new List<(Point, Direction)>(), new HashSet<Point>()));

            var visited = new HashSet<(Point Position, Direction Facing)>();

            while (!queue.IsEmpty) // Using IsEmpty property
            {
                var current = queue.Dequeue();

                var visitedKey = (current.Position, current.Facing);
                if (visited.Contains(visitedKey))
                {
                    continue;
                }
                visited.Add(visitedKey);

                if (current.Position.Equals(targetPos))
                {
                    return current.Path; // Path found
                }

                // Pass canEnterGoalTile to GetNextMoves, which then passes it to IsValidMove
                // to check if the specific targetPos (if it's the main goal tile) can be stepped on.
                foreach (var nextMove in GetNextMoves(current, canEnterGoalTile))
                {
                    // The visited check at the start of the loop handles redundancy.
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
            public long Id { get; }

            public PathNode(Point pos, Direction facing, int cost,
        List<(Point, Direction)> path, HashSet<Point> collectedCoins)
            {
                Position = pos;
                Facing = facing;
                Cost = cost;
                Path = path;
                CollectedCoins = collectedCoins;
                Id = System.Threading.Interlocked.Increment(ref PathFinder.nextNodeId);
            }

            public int CompareTo(PathNode other)
            {
                if (other == null) return 1;
                int costComparison = Cost.CompareTo(other.Cost);
                if (costComparison != 0)
                {
                    return costComparison;
                }
                return Id.CompareTo(other.Id);
            }
        }
    }
}
