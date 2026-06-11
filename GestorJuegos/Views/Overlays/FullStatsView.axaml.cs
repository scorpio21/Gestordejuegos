using System;
using System.Collections.Generic;
using Avalonia.Controls;
using GestorJuegos.Utils;

namespace GestorJuegos.Views.Overlays;

public partial class FullStatsView : UserControl
{
    public event EventHandler? RequestClose;

    public FullStatsView()
    {
        InitializeComponent();
        this.FindControl<Button>("BtnCloseFullStats")!.Click += (s, e) => {
            SoundHelper.PlayBack();
            RequestClose?.Invoke(this, EventArgs.Empty);
        };
    }

    public void UpdateStats(int totalGames, int totalPlatforms, int totalGenres, 
                            IEnumerable<KeyValuePair<string, int>> platformStats,
                            IEnumerable<KeyValuePair<string, int>> regionStats)
    {
        this.FindControl<TextBlock>("FullStatsTotalGames")!.Text = totalGames.ToString();
        this.FindControl<TextBlock>("FullStatsTotalPlatforms")!.Text = totalPlatforms.ToString();
        this.FindControl<TextBlock>("FullStatsTotalGenres")!.Text = totalGenres.ToString();
        
        this.FindControl<ListBox>("FullStatsPlatformList")!.ItemsSource = platformStats;
        this.FindControl<ListBox>("FullStatsRegionList")!.ItemsSource = regionStats;
    }
}
