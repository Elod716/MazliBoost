using System.Windows;

namespace MazliBoost
{
    public partial class SaveChangesWindow : Window
    {
        public SaveChangesWindow(
            string title,
            string message,
            string saveText,
            string discardText)
        {
            InitializeComponent();

            Title = title;
            MessageText.Text = message;
            SaveButton.Content = saveText;
            DiscardButton.Content = discardText;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
