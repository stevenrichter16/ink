using System;

namespace InkSim
{
    public sealed class InkGrid
    {
        public int width { get; private set; }
        public int height { get; private set; }
        public InkCell[] cells { get; private set; }

        public InkGrid(int width, int height, Func<int,int,InkCell> cellFactory)
        {
            this.width = width;
            this.height = height;

            cells = new InkCell[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    cells[x + y * width] = cellFactory(x, y);
            }
        }

        public InkCell Get(int x, int y) { return cells[x + y * width]; }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }
    }
}
