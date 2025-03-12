using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Open_Day
{
    public class GameField
    {
        public FieldType[,] Field { get; private set; }
        public Bot Bot { get; set; }
        private int width;
        private int height;

        public GameField(int width, int height)
        {
            this.width = width;
            this.height = height;
            Field = new FieldType[width, height];
            Bot = new Bot();

            // Initialisiere alle Felder als leer
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Field[x, y] = FieldType.Empty;
                }
            }
        }

        public void SetField(int x, int y, FieldType type)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                Field[x, y] = type;
            }
        }

        public bool IsValidMove(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;
            return Field[x, y] != FieldType.Wall;
        }
    }
}
