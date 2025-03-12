using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Open_Day
{
    public class Bot
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Direction Facing { get; set; } = Direction.Right;
        public bool HasCoin { get; set; }

        public void MoveForward()
        {
            switch (Facing)
            {
                case Direction.Up: Y--; break;
                case Direction.Right: X++; break;
                case Direction.Down: Y++; break;
                case Direction.Left: X--; break;
            }
        }

        public void TurnLeft()
        {
            Facing = (Direction)(((int)Facing + 3) % 4);
        }

        public void TurnRight()
        {
            Facing = (Direction)(((int)Facing + 1) % 4);
        }
    }
}
