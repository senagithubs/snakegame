using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var zaman = TimeSpan.FromMilliseconds(100);
        var snakeGame = new SnakeGame();

        using (var cts = new CancellationTokenSource())
        {
            async Task tusgirisleri()
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var tus = Console.ReadKey(intercept: true).Key;
                        snakeGame.OnKeyPress(tus);
                    }

                    await Task.Delay(10);
                }
            }

            var monitorKeyPresses = tusgirisleri();

            do
            {
                snakeGame.OnGameTick();
                snakeGame.Render();
                await Task.Delay(zaman);
            }
            while (!snakeGame.GameOver);

            cts.Cancel();
            await monitorKeyPresses;
        }
    }

    enum yon
    {
        yukarı,
        asagı,
        sol,
        sag
    }

    interface IRenderable
    {
        void Render();
    }

    readonly struct Position
    {
        public Position(int top, int left)
        {
            Top = top;
            Left = left;
        }
        public int Top { get; }
        public int Left { get; }

        public Position RightBy(int n) => new Position(Top, Left + n);
        public Position DownBy(int n) => new Position(Top + n, Left);
    }

    class Apple : IRenderable
    {
        public Apple(Position position)
        {
            Position = position;
        }

        public Position Position { get; }

        public void Render()
        {
            Console.SetCursorPosition(Position.Left, Position.Top);
            Console.Write("*");
        }
    }

    class Snake : IRenderable
    {
        private List<Position> _body;
        private int _growthSpurtsRemaining;

        public Snake(Position spawnLocation, int initialSize = 1)
        {
            _body = new List<Position> { spawnLocation };
            _growthSpurtsRemaining = Math.Max(0, initialSize - 1);
            Dead = false;
        }

        public bool Dead { get; private set; }
        public Position Head => _body.First();
        private IEnumerable<Position> Body => _body.Skip(1);

        public void Move(yon direction)
        {
            if (Dead) return; // Oyun bittiğinde hareket etmeyi engelle

            Position newHead;

            switch (direction)
            {
                case yon.yukarı:
                    newHead = Head.DownBy(-1);
                    break;

                case yon.sol:
                    newHead = Head.RightBy(-1);
                    break;

                case yon.asagı:
                    newHead = Head.DownBy(1);
                    break;

                case yon.sag:
                    newHead = Head.RightBy(1);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_body.Contains(newHead) || !PositionIsValid(newHead))
            {
                Dead = true;
                return;
            }

            _body.Insert(0, newHead);

            if (_growthSpurtsRemaining > 0)
            {
                _growthSpurtsRemaining--;
            }
            else
            {
                _body.RemoveAt(_body.Count - 1);
            }
        }

        public void Grow()
        {
            if (Dead) return; // Oyun bittiğinde büyümeyi engelle

            _growthSpurtsRemaining++;
        }

        public void Render()
        {
            Console.SetCursorPosition(Head.Left, Head.Top);
            Console.Write("0");

            foreach (var position in Body)
            {
                Console.SetCursorPosition(position.Left, position.Top);
                Console.Write("*");
            }
        }

        private static bool PositionIsValid(Position position) =>
            position.Top >= 0 && position.Left >= 0;
    }

    class SnakeGame : IRenderable
    {
        private static readonly Position Origin = new Position(0, 0);

        private yon _currentDirection;
        private yon _nextDirection;
        private Snake _snake;
        private Apple _apple;

        public SnakeGame()
        {
            _snake = new Snake(Origin, initialSize: 5);
            _apple = CreateApple();
            _currentDirection = yon.sag;
            _nextDirection = yon.sag;
        }

        public bool GameOver => _snake.Dead;

        public void OnKeyPress(ConsoleKey key)
        {
            yon newDirection;

            switch (key)
            {
                case ConsoleKey.W:
                    newDirection = yon.yukarı;
                    break;

                case ConsoleKey.A:
                    newDirection = yon.sol;
                    break;

                case ConsoleKey.S:
                    newDirection = yon.asagı;
                    break;

                case ConsoleKey.D:
                    newDirection = yon.sag;
                    break;

                default:
                    return;
            }

            // Snake cannot turn 180 degrees.
            if (newDirection == OppositeDirectionTo(_currentDirection))
            {
                return;
            }

            _nextDirection = newDirection;
        }

        public void OnGameTick()
        {
            if (GameOver) return; // Oyun bittiğinde oyun döngüsünü devre dışı bırak

            _currentDirection = _nextDirection;
            _snake.Move(_currentDirection);

            // Yılanın başı elma ile aynı konuma geldiğinde yılanı büyüt
            if (_snake.Head.Equals(_apple.Position))
            {
                _snake.Grow();
                _apple = CreateApple();
            }
        }

        public void Render()
        {
            Console.Clear();
            _snake.Render();
            _apple.Render();
            Console.SetCursorPosition(0, 0);
        }

        private static yon OppositeDirectionTo(yon direction)
        {
            switch (direction)
            {
                case yon.yukarı: return yon.asagı;
                case yon.sol: return yon.sag;
                case yon.sag: return yon.sol;
                case yon.asagı: return yon.yukarı;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static Apple CreateApple()
        {
            const int numberOfRows = 20;
            const int numberOfColumns = 20;

            var random = new Random();
            var top = random.Next(0, numberOfRows + 1);
            var left = random.Next(0, numberOfColumns + 1);
            var position = new Position(top, left);
            var apple = new Apple(position);

            return apple;
        }
    }
}
