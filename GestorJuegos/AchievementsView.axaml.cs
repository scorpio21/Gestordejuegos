using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using GestorJuegos.Models;

namespace GestorJuegos
{
    public partial class AchievementsView : UserControl
    {
        public event EventHandler? RequestClose;

        public AchievementsView()
        {
            InitializeComponent();
            BtnClose.Click += (s, e) => RequestClose?.Invoke(this, EventArgs.Empty);
        }

        public void Initialize(List<Achievement> achievements)
        {
            ItemsAchievementsList.ItemsSource = achievements;
        }
    }
}
