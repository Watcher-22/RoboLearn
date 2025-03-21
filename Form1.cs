using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Open_Day
{
    public partial class Form1 : Form
    {
        private GameField gameField;
        internal const int CELL_SIZE = 30;
        internal const int GRID_SIZE = 18;
        private FlowLayoutPanel blockPalette;
        private FlowLayoutPanel codeWorkspace;
        private BufferedGraphics graphicsBuffer;
        private BufferedGraphicsContext context;
        private ListBox thoughtProcessBox;


        // Controls deklarieren
        private Panel gamePanel;
        private ComboBox difficultySelect;
        private Button btnMoveForward;
        private Button btnTurnLeft;
        private Button btnTurnRight;
        private Button btnCollectCoin;
        private Button btnStart;
        private Button btnReset;
        private int delay = 400;

        // Goodies
        private Timer gameTimer;
        private Label timerLabel;
        private Label scoreLabel;
        private int gameTimeSeconds = 0;
        private int collectedCoins = 0;
        private int totalCoins = 0;


        public Form1()
        {
            InitializeComponent();
            InitializeControls();
            InitializeGame();
        }

        #region Setup/Reset
        private void InitializeGame()
        {


            gameField = new GameField(GRID_SIZE, GRID_SIZE);

            if (gameTimer == null)
            {
                gameTimer = new Timer
                {
                    Interval = 1000 // 1 Sekunde
                };
                gameTimer.Tick += GameTimer_Tick;
            }
            gameTimer.Start();

            Random random = new Random();

            // Startposition zufällig setzen (am linken Rand)
            int startY = random.Next(1, GRID_SIZE - 1);
            gameField.SetField(0, startY, FieldType.Start);

            if (gameField.Bot == null)
            {
                gameField.Bot = new Bot();
            }
            gameField.Bot.X = 0;
            gameField.Bot.Y = startY;

            // Ziel zufällig setzen (am rechten Rand)
            int goalY = random.Next(1, GRID_SIZE - 1);
            gameField.SetField(GRID_SIZE - 1, goalY, FieldType.Goal);

            // Wände zufällig setzen (ca. 20% der Felder)
            int numberOfWalls = (GRID_SIZE * GRID_SIZE) / 5;
            for (int i = 0; i < numberOfWalls; i++)
            {
                int x = random.Next(1, GRID_SIZE - 1);
                int y = random.Next(0, GRID_SIZE);
                if (gameField.Field[x, y] == FieldType.Empty)
                {
                    gameField.SetField(x, y, FieldType.Wall);
                }
            }

            // Münzen zufällig setzen (ca. 5% der Felder)
            int numberOfCoins = (GRID_SIZE * GRID_SIZE) / 20;
            for (int i = 0; i < numberOfCoins; i++)
            {
                int x = random.Next(1, GRID_SIZE - 1);
                int y = random.Next(0, GRID_SIZE);
                if (gameField.Field[x, y] == FieldType.Empty)
                {
                    gameField.SetField(x, y, FieldType.Coin);
                }
            }

            CountTotalCoins();

            UpdateScoreDisplay();

            // Generiert das Spielfeld neu
            if (gamePanel != null)
            {
                UpdateGameField();
            }

            gameTimeSeconds = 0;
            collectedCoins = 0;
            CountTotalCoins();
            UpdateScoreDisplay();
            UpdateTimerDisplay();

            if (gameTimer != null)
            {
                gameTimer.Start();
            }
        }

        private void InitializeGameControls()
        {
            timerLabel = new Label
            {
                Location = new Point(gamePanel.Right + 350, 20),
                Size = new Size(150, 25),
                Text = "Zeit: 00:00",
                Font = new Font("Arial", 12, FontStyle.Bold)
            };

            scoreLabel = new Label
            {
                Location = new Point(gamePanel.Right + 200, 20),
                Size = new Size(150, 25),
                Text = "Münzen: 0/0",
                Font = new Font("Arial", 12, FontStyle.Bold)
            };

            gameTimer = new Timer
            {
                Interval = 1000
            };
            gameTimer.Tick += GameTimer_Tick;

            this.Controls.AddRange(new Control[] { timerLabel, scoreLabel });


        }

        private void InitializeControls()
        {
            this.Size = new Size(1250, 650);
            this.Text = "RoboLearn";

            gamePanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(GRID_SIZE * CELL_SIZE, GRID_SIZE * CELL_SIZE),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Double Buffering ... Es funktioniert, also lassen wir es so
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, gamePanel, new object[] { true });

            context = BufferedGraphicsManager.Current;
            context.MaximumBuffer = new Size(GRID_SIZE * CELL_SIZE + 1, GRID_SIZE * CELL_SIZE + 1);
            graphicsBuffer = context.Allocate(gamePanel.CreateGraphics(),
                gamePanel.ClientRectangle);

            gamePanel.Paint += GamePanel_Paint;

            // Schwierigkeitsauswahl
            difficultySelect = new ComboBox
            {
                Location = new Point(gamePanel.Right + 20, 20),
                Size = new Size(150, 25)
            };
            difficultySelect.Items.AddRange(new string[] { "Level 1: Manuell", "Level 2: Logisch", "Level 3: Selfmade", "Level 4: Automatisch" });
            difficultySelect.SelectedIndex = 0;
            difficultySelect.SelectedIndexChanged += DifficultySelect_SelectedIndexChanged;

            // Steuerungsbuttons für Level 1
            btnMoveForward = CreateButton("Vorwärts", gamePanel.Right + 20, 60);
            btnTurnLeft = CreateButton("Links drehen", gamePanel.Right + 20, 100);
            btnTurnRight = CreateButton("Rechts drehen", gamePanel.Right + 20, 140);
            btnCollectCoin = CreateButton("Münze aufheben", gamePanel.Right + 20, 180);
            btnStart = CreateButton("Start", gamePanel.Right + 20, 220);
            btnReset = CreateButton("Zurücksetzen", gamePanel.Right + 20, 260);



            this.Controls.AddRange(new Control[] {
                gamePanel,
                difficultySelect,
                btnMoveForward,
                btnTurnLeft,
                btnTurnRight,
                btnCollectCoin,
                btnStart,
                btnReset,
                blockPalette,  
                codeWorkspace  
            });

          

            // Block-Palette und Workspace für Level 2 (initial versteckt)
            InitializeBlockPanels();

            thoughtProcessBox = new ListBox
            {
                Location = new Point(blockPalette.Right + 20, 60),
                Size = new Size(400, 400),
                Font = new Font("Arial", 10),
                Visible = false
            };

            this.Controls.Add(thoughtProcessBox);

            InitializeGameControls();
        }

        private void InitializeBlockPanels()
        {
            // Block-Palette
            blockPalette = new FlowLayoutPanel
            {
                Location = new Point(gamePanel.Right + 20, 60),
                Size = new Size(200, 400),
                FlowDirection = FlowDirection.TopDown,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Visible = false
            };

            // Programm-Bereich
            codeWorkspace = new FlowLayoutPanel
            {
                Location = new Point(blockPalette.Right + 20, 60),
                Size = new Size(400, 400),
                FlowDirection = FlowDirection.TopDown,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Visible = false
            };

            this.Controls.AddRange(new Control[] { blockPalette, codeWorkspace });
        }

        private Button CreateButton(string text, int x, int y)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(150, 30)
            };
            btn.Click += Button_Click;
            return btn;
        }

        private void CreateCommandButtons()
        {
            // Aktions-Befehle
            CreateCommandButton("Vorwärts", Color.LightBlue);
            CreateCommandButton("Links drehen", Color.LightBlue);
            CreateCommandButton("Rechts drehen", Color.LightBlue);
            CreateCommandButton("Münze aufheben", Color.LightBlue);

            // Schleifen
            CreateCommandButton("Wiederhole bis Wand", Color.LightGreen);
            CreateCommandButton("Wiederhole bis Münze", Color.LightGreen);
            CreateCommandButton("Wiederhole bis Ziel", Color.LightGreen);
            CreateCommandButton("Wiederhole x mal", Color.LightGreen);
        }

        private void CreateCommandButton(string command, Color color)
        {
            Button btn = new Button
            {
                Text = command,
                Size = new Size(180, 30),
                BackColor = color,
                Margin = new Padding(5)
            };

            btn.Click += (s, e) =>
            {
                var newBlock = new CodeBlock(command, color, codeWorkspace)
                {
                    OrderIndex = codeWorkspace.Controls.Count
                };
                codeWorkspace.Controls.Add(newBlock);
            };

            blockPalette.Controls.Add(btn);
        }

        private void GamePanel_Paint(object sender, PaintEventArgs e)
        {
            if (graphicsBuffer != null)
            {
                // Zeichnet in den Buffer
                DrawGrid(graphicsBuffer.Graphics);
                DrawGameElements(graphicsBuffer.Graphics);

                // Kopiert den Buffer auf das Panel
                graphicsBuffer.Render(e.Graphics);
            }
        }

        private void DrawGrid(Graphics g)
        {
            for (int i = 0; i <= GRID_SIZE; i++)
            {
                // Vertikale Linien
                g.DrawLine(Pens.Gray, i * CELL_SIZE, 0, i * CELL_SIZE, GRID_SIZE * CELL_SIZE);
                // Horizontale Linien
                g.DrawLine(Pens.Gray, 0, i * CELL_SIZE, GRID_SIZE * CELL_SIZE, i * CELL_SIZE);
            }
        }

        private void DrawGameElements(Graphics g)
        {
            g.Clear(Color.White);
            DrawGrid(g);
            for (int x = 0; x < GRID_SIZE; x++)
            {
                for (int y = 0; y < GRID_SIZE; y++)
                {
                    var cellRect = new Rectangle(x * CELL_SIZE, y * CELL_SIZE, CELL_SIZE, CELL_SIZE);
                    switch (gameField.Field[x, y])
                    {
                        case FieldType.Wall:
                            g.FillRectangle(Brushes.Gray, cellRect);
                            break;
                        case FieldType.Coin:
                            g.FillEllipse(Brushes.Gold, cellRect);
                            break;
                        case FieldType.Goal:
                            g.FillRectangle(Brushes.Green, cellRect);
                            break;
                        case FieldType.Start:
                            g.FillRectangle(Brushes.Blue, cellRect);
                            break;
                    }
                }
            }

            var botRect = new Rectangle(
            gameField.Bot.X * CELL_SIZE,
            gameField.Bot.Y * CELL_SIZE,
            CELL_SIZE,
            CELL_SIZE
            );
            DrawBot(g, botRect, gameField.Bot.Facing);
        }

        private void DrawBot(Graphics g, Rectangle rect, Direction facing)
        {
            Point[] trianglePoints = new Point[3];
            switch (facing)
            {
                case Direction.Up:
                    trianglePoints[0] = new Point(rect.Left + rect.Width / 2, rect.Top);
                    trianglePoints[1] = new Point(rect.Left, rect.Bottom);
                    trianglePoints[2] = new Point(rect.Right, rect.Bottom);
                    break;
                case Direction.Right:
                    trianglePoints[0] = new Point(rect.Right, rect.Top + rect.Height / 2);
                    trianglePoints[1] = new Point(rect.Left, rect.Top);
                    trianglePoints[2] = new Point(rect.Left, rect.Bottom);
                    break;
                case Direction.Down:
                    trianglePoints[0] = new Point(rect.Left + rect.Width / 2, rect.Bottom);
                    trianglePoints[1] = new Point(rect.Left, rect.Top);
                    trianglePoints[2] = new Point(rect.Right, rect.Top);
                    break;
                case Direction.Left:
                    trianglePoints[0] = new Point(rect.Left, rect.Top + rect.Height / 2);
                    trianglePoints[1] = new Point(rect.Right, rect.Top);
                    trianglePoints[2] = new Point(rect.Right, rect.Bottom);
                    break;
            }
            g.FillPolygon(Brushes.Red, trianglePoints);
        }

        private async void Button_Click(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                switch (button.Text)
                {
                    case "Vorwärts":
                        MoveForward();
                        UpdateGameField();
                        break;

                    case "Links drehen":
                        TurnLeft();
                        UpdateGameField();
                        break;

                    case "Rechts drehen":
                        TurnRight();
                        UpdateGameField();
                        break;

                    case "Münze aufheben":
                        CollectCoin();
                        UpdateGameField();
                        break;

                    case "Start":
                        button.Enabled = false;
                        try
                        {
                            if (difficultySelect.SelectedIndex == 1)
                            {
                                await ExecuteBlockProgram();
                            }
                            else if (difficultySelect.SelectedIndex == 2)
                            {
                                var solution = new UserSolution(this, gameField, gamePanel);
                                await solution.RunCode();
                            }
                            else if (difficultySelect.SelectedIndex == 3)
                            {
                                await ExecuteOptimalPath();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Fehler bei der Ausführung: {ex.Message}",
                                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        finally
                        {
                            button.Enabled = true;
                        }
                        break;

                    case "Zurücksetzen":
                        ResetGame();
                        UpdateGameField();
                        break;
                }
            }
        }

        private void SetupLevel1()
        {
            btnMoveForward.Visible = true;
            btnTurnLeft.Visible = true;
            btnTurnRight.Visible = true;
            btnCollectCoin.Visible = true;
            btnReset.Visible = true;
            blockPalette.Visible = false;
            codeWorkspace.Visible = false;
            btnReset.Location = new Point(gamePanel.Right + 20, 260);
            btnStart.Location = new Point(gamePanel.Right + 20, 220);
        }

        private void SetupLevel2()
        {
            btnMoveForward.Visible = false;
            btnTurnLeft.Visible = false;
            btnTurnRight.Visible = false;
            btnCollectCoin.Visible = false;

            blockPalette.Visible = true;
            codeWorkspace.Visible = true;

            // Blocks erstellen
            blockPalette.Controls.Clear();
            codeWorkspace.Controls.Clear();
            CreateCommandButtons();

            btnStart.Location = new Point(codeWorkspace.Left, codeWorkspace.Bottom + 10);
            btnReset.Location = new Point(btnStart.Right + 10, codeWorkspace.Bottom + 10);
        }

        private void SetupLevel3()
        {
            btnMoveForward.Visible = false;
            btnTurnLeft.Visible = false;
            btnTurnRight.Visible = false;
            btnCollectCoin.Visible = false;
            blockPalette.Visible = false;
            codeWorkspace.Visible = false;

            // Nur Start-Button anzeigen und positionieren
            btnStart.Visible = true;
            btnStart.Location = new Point(gamePanel.Right + 20, 60);
            btnReset.Location = new Point(gamePanel.Right + 20, 100);
        }

        private void SetupLevel4()
        {
            btnMoveForward.Visible = false;
            btnTurnLeft.Visible = false;
            btnTurnRight.Visible = false;
            btnCollectCoin.Visible = false;
            blockPalette.Visible = false;
            codeWorkspace.Visible = false;
            thoughtProcessBox.Visible = true;

            btnStart.Visible = true;
            btnStart.Location = new Point(gamePanel.Right + 20, 60);
            btnReset.Location = new Point(gamePanel.Right + 20, 100);

            // Überschrift für die Gedankenschritte
            Label thoughtLabel = new Label
            {
                Text = "Gedankenschritte des Bots:",
                Location = new Point(gamePanel.Right + 20, 140),
                Size = new Size(300, 20),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            this.Controls.Add(thoughtLabel);
        }

        private void ResetGame()
        {
            gameTimer.Stop();
            gameTimeSeconds = 0;
            UpdateTimerDisplay();
            InitializeGame();

            // UI-Elemente zurücksetzen
            if (difficultySelect.SelectedIndex == 1)
            {
                codeWorkspace.Controls.Clear();
            }

            if (graphicsBuffer != null)
            {
                graphicsBuffer.Graphics.Clear(gamePanel.BackColor);
                DrawGrid(graphicsBuffer.Graphics);
                DrawGameElements(graphicsBuffer.Graphics);
                graphicsBuffer.Render();
            }

            if (difficultySelect.SelectedIndex == 4)
            {
                thoughtProcessBox.Items.Clear();
            }

            gamePanel.Invalidate();

        }

        #endregion

        #region Update
        private void GameTimer_Tick(object sender, EventArgs e)
        {
            gameTimeSeconds++;
            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            int minutes = gameTimeSeconds / 60;
            int seconds = gameTimeSeconds % 60;
            timerLabel.Text = $"Zeit: {minutes:00}:{seconds:00}";
        }    

        private void DifficultySelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (difficultySelect.SelectedIndex)
            {
                case 0: 
                    SetupLevel1();
                    break;
                case 1: 
                    SetupLevel2();
                    break;
                case 2:
                    SetupLevel3();
                    break;
                case 3:
                    SetupLevel4();
                    break;
            }
        }

        private async Task ExecuteBlockProgram()
        {
            foreach (CodeBlock block in codeWorkspace.Controls.Cast<CodeBlock>())
            {
                if (block.BlockType.StartsWith("Wiederhole"))
                {
                    string selectedCommand = block.GetSelectedCommand();
                    if (selectedCommand != "-- Befehl auswählen --")
                    {
                        bool continueLoop = true;
                        int repeatCount = block.GetRepeatCount();
                        int currentRepeat = 0;

                        while (continueLoop)
                        {
                            switch (block.BlockType)
                            {
                                case "Wiederhole bis Wand":
                                    continueLoop = !CheckWallAhead();
                                    break;
                                case "Wiederhole bis Münze":
                                    continueLoop = !CheckForCoin();
                                    break;
                                case "Wiederhole bis Ziel":
                                    continueLoop = !CheckAtGoal();
                                    break;
                                case "Wiederhole x mal":
                                    continueLoop = currentRepeat < repeatCount;
                                    break;
                            }

                            if (continueLoop)
                            {
                                ExecuteCommand(selectedCommand);
                                UpdateGameField();
                                await Task.Delay(delay);
                                currentRepeat++;
                            }
                        }
                    }
                }
                else
                {
                    ExecuteCommand(block.BlockType);
                    UpdateGameField();
                    await Task.Delay(delay);
                }
            }
        }

        private void ExecuteCommand(string command)
        {
            switch (command)
            {
                case "Vorwärts":
                    MoveForward();
                    break;
                case "Links drehen":
                    TurnLeft();
                    break;
                case "Rechts drehen":
                    TurnRight();
                    break;
                case "Münze aufheben":
                    CollectCoin();
                    break;
            }
        }
        private void UpdateScoreDisplay()
        {
            scoreLabel.Text = $"Münzen: {collectedCoins}/{totalCoins}";
        }

        private void UpdateGameField()
        {
            if (gamePanel != null)
            {
                gamePanel.Invalidate(new Rectangle(
                    gameField.Bot.X * CELL_SIZE - CELL_SIZE,
                    gameField.Bot.Y * CELL_SIZE - CELL_SIZE,
                    CELL_SIZE * 3,
                    CELL_SIZE * 3));
            }
        }

        #endregion

        #region Game Logic
        public void MoveForward()
        {
            int newX = gameField.Bot.X;
            int newY = gameField.Bot.Y;
            switch (gameField.Bot.Facing)
            {
                case Direction.Up: newY--; break;
                case Direction.Right: newX++; break;
                case Direction.Down: newY++; break;
                case Direction.Left: newX--; break;
            }

            if (gameField.IsValidMove(newX, newY))
            {
                gameField.Bot.MoveForward();
                CheckWinCondition();
                UpdateGameField(); 
            }
            else
            {
                MessageBox.Show("Diese Bewegung ist nicht möglich!", "Hindernis",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void TurnLeft()
        {
            gameField.Bot.TurnLeft();
        }

        public void TurnRight()
        {
            gameField.Bot.TurnRight();
            
        }

        public void CollectCoin()
        {
            int x = gameField.Bot.X;
            int y = gameField.Bot.Y;

            if (gameField.Field[x, y] == FieldType.Coin)
            {
                gameField.Field[x, y] = FieldType.Empty;
                gameField.Bot.HasCoin = true;
                collectedCoins++;
                UpdateScoreDisplay();
            }
            else
            {
                MessageBox.Show("Hier liegt keine Münze!", "Keine Münze",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public bool CheckWallAhead()
        {
            int x = gameField.Bot.X;
            int y = gameField.Bot.Y;

            switch (gameField.Bot.Facing)
            {
                case Direction.Up:
                    return y <= 0 || gameField.Field[x, y - 1] == FieldType.Wall;
                case Direction.Right:
                    return x >= GRID_SIZE - 1 || gameField.Field[x + 1, y] == FieldType.Wall;
                case Direction.Down:
                    return y >= GRID_SIZE - 1 || gameField.Field[x, y + 1] == FieldType.Wall;
                case Direction.Left:
                    return x <= 0 || gameField.Field[x - 1, y] == FieldType.Wall;
                default:
                    return true;
            }
        }

        public bool CheckForCoin()
        {
            int x = gameField.Bot.X;
            int y = gameField.Bot.Y;
            return gameField.Field[x, y] == FieldType.Coin;
        }

        public bool CheckAtGoal()
        {
            int x = gameField.Bot.X;
            int y = gameField.Bot.Y;
            return gameField.Field[x, y] == FieldType.Goal;
        }

        private void CountTotalCoins()
        {
            totalCoins = 0;
            for (int x = 0; x < GRID_SIZE; x++)
            {
                for (int y = 0; y < GRID_SIZE; y++)
                {
                    if (gameField.Field[x, y] == FieldType.Coin)
                    {
                        totalCoins++;
                    }
                }
            }
        }

        public bool CheckWinCondition()
        {
            if (gameField.Field[gameField.Bot.X, gameField.Bot.Y] == FieldType.Goal && gameField.Bot.HasCoin)
            {
                gameTimer.Stop();
                MessageBox.Show($"Gratulation! Du hast das Level geschafft!\nZeit: {gameTimeSeconds / 60:00}:{gameTimeSeconds % 60:00}\nMünzen: {collectedCoins}/{totalCoins}",
                    "Gewonnen!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            return false;
        }

        private async Task ExecuteOptimalPath()
        {
            thoughtProcessBox.Items.Clear();
            var pathFinder = new PathFinder(gameField);
            var optimalPath = pathFinder.FindOptimalPath();

            Point currentPosition = new Point(gameField.Bot.X, gameField.Bot.Y);

            foreach (var (position, facing) in optimalPath)
            {
                if (gameField.Bot.Facing != facing)
                {
                    Direction currentFacing = gameField.Bot.Facing;
                    thoughtProcessBox.Items.Insert(0, $"Drehe von {GetDirectionName(currentFacing)} nach {GetDirectionName(facing)}");
                    await TurnToDirection(facing);
                }

                if (position != currentPosition)
                {
                    thoughtProcessBox.Items.Insert(0, "Gehe einen Schritt vorwärts");
                    MoveForward();
                    currentPosition = position;
                    gamePanel.Invalidate();
                    await Task.Delay(delay);
                }

                if (gameField.Field[currentPosition.X, currentPosition.Y] == FieldType.Coin)
                {
                    thoughtProcessBox.Items.Insert(0, "Hebe Münze auf");
                    CollectCoin();
                    gamePanel.Invalidate();
                    await Task.Delay(delay);
                }
            }
        }

        private string GetDirectionName(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return "Norden";
                case Direction.Right:
                    return "Osten";
                case Direction.Down:
                    return "Süden";
                case Direction.Left:
                    return "Westen";
                default:
                    return "Unbekannt";
            }
        }


        private async Task TurnToDirection(Direction targetDirection)
        {
            while (gameField.Bot.Facing != targetDirection)
            {
                int diff = ((int)targetDirection - (int)gameField.Bot.Facing + 4) % 4;
                if (diff > 2 || (diff == 2 && gameField.Bot.Facing == Direction.Left))
                {
                    TurnLeft();
                }
                else
                {
                    TurnRight();
                }
                UpdateGameField();
                await Task.Delay(delay);
            }
        }

        #endregion
    }
}