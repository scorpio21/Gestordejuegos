using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using GestorJuegos.Models;

namespace GestorJuegos.Views.Overlays;

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
            if (achievements == null) return;

            // Ordenar: desbloqueados primero (más recientes primero)
            var ordered = achievements
                .OrderByDescending(a => a.IsUnlocked)
                .ThenByDescending(a => a.UnlockDate ?? DateTime.MinValue)
                .ToList();

            ItemsAchievementsList.ItemsSource = ordered;
        }
    }
