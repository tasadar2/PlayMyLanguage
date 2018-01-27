using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using PlayMyLanguage.Processors;
using PlayMyLanguage.Processors.Support;
using PlayMyLanguage.Translation;
using PlayMyLanguage.Translation.LanguageSwitchers;
using PlayMyLanguage.Translation.Support;
using PlayMyLanguage.Translation.Translators;

namespace PlayMyLanguage
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TranslationProcessor _translationProcessor;

        public MainWindow()
        {
            InitializeComponent();

            GameDirectory.Text = Environment.CurrentDirectory;

            var sourceLanguages = TextTranslator.Languages.ToList();
            sourceLanguages.Insert(0, new Language {Name = "Auto-Detect", Code = "auto"});
            SourceLanguage.ItemsSource = sourceLanguages;
            SourceLanguage.SelectedValue = "auto";

            TargetLanguage.ItemsSource = TextTranslator.Languages.ToList();
            TargetLanguage.SelectedValue = "en";

            var languageSwitcher = new RpgMakerLanguageSwitcher();
            var translator = new GoogleTextTranslator();
            _translationProcessor = new TranslationProcessor(languageSwitcher, translator);
            _translationProcessor.StatusChanged += _translationProcessor_StatusChanged;
            _translationProcessor.TranslationComplete += _translationProcessor_TranslationComplete;
        }

        private void _translationProcessor_StatusChanged(object sender, StatusEventArgs e)
        {
            Dispatcher.Invoke(() => StatusChanged(sender, e));
        }

        private void StatusChanged(object sender, StatusEventArgs e)
        {
            Status.Text = e.Message;
            if (e.Progress > -1)
            {
                Progress.Visibility = Visibility.Visible;
                Progress.Value = e.Progress <= 100 ? e.Progress : 100;
            }
        }

        private void _translationProcessor_TranslationComplete(object sender, TranslationCompleteEventArgs e)
        {
            Dispatcher.Invoke(() => TranslationComplete(sender, e));
        }

        private void TranslationComplete(object sender, TranslationCompleteEventArgs e)
        {
            Status.Text = e.Message;
            Play.IsEnabled = true;
            Progress.Value = 100;
            switch (e.CompletionResult)
            {
                case CompletionResult.Success:
                    Status.Foreground = Brushes.Green;
                    Close();
                    break;
                case CompletionResult.Warn:
                    Status.Foreground = Brushes.Orange;
                    break;
                case CompletionResult.Error:
                    Status.Foreground = Brushes.Red;
                    break;
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            Status.Foreground = Brushes.Black;
            Status.Text = "Changing languages...";
            Play.IsEnabled = false;

            var gamePath = GameDirectory.Text;
            var sourceLanguage = (Language)SourceLanguage.SelectedItem;
            var targetLanguage = (Language)TargetLanguage.SelectedItem;

            Task.Run(() => _translationProcessor.Translate(gamePath, sourceLanguage, targetLanguage));
        }

        private void GameBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = GameDirectory.Text,
                Filter = "game.exe|game.exe|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog(this) == true)
            {
                GameDirectory.Text = Path.GetDirectoryName(dialog.FileName);
            }
        }
    }
}