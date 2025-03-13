using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Open_Day
{
    public class CodeBlock : Panel
    {
        public string BlockType { get; private set; }
        public int OrderIndex { get; set; }
        private FlowLayoutPanel parentWorkspace;
        private ComboBox commandDropdown;
        private NumericUpDown repeatCounter; 
        private Color originalColor;
        public List<CodeBlock> NestedBlocks { get; private set; } = new List<CodeBlock>();
        public bool IsSelected { get; private set; }

        public CodeBlock(string type, Color color, FlowLayoutPanel workspace)
        {
            BlockType = type;
            originalColor = color;
            BackColor = color;
            Size = new Size(350, type.StartsWith("Wiederhole") ? 80 : 40);
            BorderStyle = BorderStyle.FixedSingle;
            Margin = new Padding(5);
            parentWorkspace = workspace;

            InitializeComponents();
        }

        private void InitializeComponents()
        {
           
            Label label = new Label
            {
                Text = BlockType,
                AutoSize = false,
                Size = new Size(200, 30),
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(10, 5),
                BackColor = Color.Transparent
            };
            Controls.Add(label);

            // Für Wiederhole-Blöcke
            if (BlockType.StartsWith("Wiederhole"))
            {
                // Dropdown-Menü für Befehle
                commandDropdown = new ComboBox
                {
                    Location = new Point(20, 40),
                    Size = new Size(250, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                
                commandDropdown.Items.AddRange(new string[]
                {
                "Vorwärts",
                "Links drehen",
                "Rechts drehen",
                "Münze aufheben"
                });

                
                commandDropdown.Items.Insert(0, "-- Befehl auswählen --");
                commandDropdown.SelectedIndex = 0;

                Controls.Add(commandDropdown);

                
                if (BlockType == "Wiederhole x mal")
                {
                    
                    repeatCounter = new NumericUpDown
                    {
                        Location = new Point(280, 40),
                        Size = new Size(50, 25),
                        Minimum = 1,
                        Maximum = 20,
                        Value = 1
                    };
                    Controls.Add(repeatCounter);
                }
            }

            
            Button btnDelete = new Button
            {
                Text = "X",
                Size = new Size(30, 30),
                Location = new Point(Width - 40, 5),
                BackColor = Color.LightPink
            };
            btnDelete.Click += (s, e) => DeleteBlock();
            Controls.Add(btnDelete);

            this.Click += (s, e) => Select();
        }
        public int GetRepeatCount()
        {
            return BlockType == "Wiederhole x mal" ? (int)repeatCounter.Value : 1;
        }

        private void DeleteBlock()
        {
            parentWorkspace.Controls.Remove(this);
        }

        // Methode zum Abrufen des ausgewählten Befehls
        public string GetSelectedCommand()
        {
            if (BlockType.StartsWith("Wiederhole") && commandDropdown != null &&
                commandDropdown.SelectedIndex > 0) // Überspringe den "Befehl auswählen" Eintrag
            {
                return commandDropdown.SelectedItem.ToString();
            }
            return BlockType; 
        }

        
        public void Select()
        {
            IsSelected = true;
            this.BackColor = LightenColor(originalColor, 0.3f);

            
            foreach (CodeBlock block in parentWorkspace.Controls.OfType<CodeBlock>())
            {
                if (block != this)
                {
                    block.Deselect();
                }
            }
        }

        public void Deselect()
        {
            IsSelected = false;
            this.BackColor = originalColor;
        }

        private Color LightenColor(Color color, float factor)
        {
            return Color.FromArgb(
                color.A,
                (int)Math.Min(255, color.R + (255 - color.R) * factor),
                (int)Math.Min(255, color.G + (255 - color.G) * factor),
                (int)Math.Min(255, color.B + (255 - color.B) * factor)
            );
        }
    }
}
