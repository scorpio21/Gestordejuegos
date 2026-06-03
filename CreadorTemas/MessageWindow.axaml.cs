using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CreadorTemas
{
    public partial class MessageWindow : Window
    {
        public MessageWindow()
        {
            InitializeComponent();
        }

        public MessageWindow(string title, string message)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtMessage.Text = message;
            Title = title;
        }

        private void BtnClose_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
