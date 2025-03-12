using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Open_Day
{
    public class UserSolution
    {
        #region Members
        private readonly Form1 form;
        private readonly GameField gameField;
        private readonly Panel gamePanel;
        private readonly Action moveForward;
        private readonly Action turnLeft;
        private readonly Action turnRight;
        private readonly Action collectCoin;
        private readonly Func<bool> checkWallAhead;
        private readonly Func<bool> checkForCoin;
        private readonly Func<bool> checkAtGoal;
        #endregion

        #region Constructor
        public UserSolution(Form1 form, GameField gameField, Panel gamePanel)
        {
            this.form = form;
            this.gameField = gameField;
            this.gamePanel = gamePanel;
            this.moveForward = form.MoveForward;
            this.turnLeft = form.TurnLeft;
            this.turnRight = form.TurnRight;
            this.collectCoin = form.CollectCoin;
            this.checkWallAhead = form.CheckWallAhead;
            this.checkForCoin = form.CheckForCoin;
            this.checkAtGoal = form.CheckAtGoal;
        }
        #endregion

        public async Task RunCode()
        {
            // VERFÜGBARE BEFEHLE:

            // Bewegung:
            // await MoveForward();     - Einen Schritt nach vorne
            // await TurnLeft();        - Nach links drehen
            // await TurnRight();       - Nach rechts drehen
            // await CollectCoin();     - Münze aufheben

            // Bedingungen:
            // CheckWallAhead()   - Prüft ob eine Wand vor dem Bot ist
            // CheckForCoin()     - Prüft ob eine Münze auf dem aktuellen Feld liegt
            // CheckAtGoal()      - Prüft ob das Ziel erreicht wurde

            // SCHREIBE DEINEN CODE HIER:
            
        }

        #region Basismethoden
        // Basismethoden mit eingebauter Verzögerung
        private async Task MoveForward()
        {
            moveForward();
            gamePanel.Invalidate();
            await Task.Delay(500);
        }

        private async Task TurnLeft()
        {
            turnLeft();
            gamePanel.Invalidate();
            await Task.Delay(500);
        }

        private async Task TurnRight()
        {
            turnRight();
            gamePanel.Invalidate();
            await Task.Delay(500);
        }

        private async Task CollectCoin()
        {
            collectCoin();
            gamePanel.Invalidate();
            await Task.Delay(500);
        }

        // Prüfmethoden bleiben unverändert
        private bool CheckWallAhead() => checkWallAhead();
        private bool CheckForCoin() => checkForCoin();
        private bool CheckAtGoal() => checkAtGoal();
        #endregion
    }

}
