﻿using System;
using System.Windows.Forms;

namespace Tetris
{
    public partial class Record : Form
    {
        private string player;

        public string Player
        {
            get { return player; }
        }

        public Record()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            player = playerName.Text;
            this.Close();
        }
    }
}
