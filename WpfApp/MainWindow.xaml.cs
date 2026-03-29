using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WpfApp
{
    public class Vocabulary
    {
        public string English { get; set; } = string.Empty;
        public string German { get; set; } = string.Empty;
    }

    public partial class MainWindow : Window
    {
        private List<Vocabulary> _allVocabulary = new();
        private List<Vocabulary> _currentRoundVocab = new();
        private List<Vocabulary> _unknownVocab = new();

        private Vocabulary _currentVocab = new();

        private int _totalInRound = 0;
        private int _knownCount = 0;

        private bool _isCardFlipped = false;
        private bool _isEnglishToGerman = true;
        private bool _isInitialized = false;

        private Random _random = new();

        public MainWindow()
        {
            InitializeComponent();
            InitializeBookAndUnitSelection();
            _isInitialized = true;
            LoadVocabularyForCurrentSelection();
            StartNewSession();
        }

        private void InitializeBookAndUnitSelection()
        {
            try
            {
                string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BlueLine");
                if (Directory.Exists(basePath))
                {
                    var books = Directory.GetDirectories(basePath)
                                         .Select(d => new DirectoryInfo(d).Name)
                                         .OrderBy(n => n)
                                         .ToList();

                    foreach (var book in books)
                    {
                        BookComboBox.Items.Add(book);
                    }

                    if (books.Count > 0)
                    {
                        BookComboBox.SelectedIndex = 0;
                        LoadUnitsForSelectedBook();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Ordnerstruktur: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUnitsForSelectedBook()
        {
            UnitComboBox.Items.Clear();
            if (BookComboBox.SelectedItem == null) return;

            try
            {
                string bookName = BookComboBox.SelectedItem.ToString() ?? "";
                string bookPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BlueLine", bookName);

                if (Directory.Exists(bookPath))
                {
                    var units = Directory.GetFiles(bookPath, "*.csv")
                                         .Select(f => Path.GetFileNameWithoutExtension(f))
                                         .OrderBy(n => n)
                                         .ToList();

                    foreach (var unit in units)
                    {
                        UnitComboBox.Items.Add(unit);
                    }

                    if (units.Count > 0)
                    {
                        UnitComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Units: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadVocabularyForCurrentSelection()
        {
            if (BookComboBox.SelectedItem == null || UnitComboBox.SelectedItem == null) return;

            string bookName = BookComboBox.SelectedItem.ToString() ?? "";
            string unitName = UnitComboBox.SelectedItem.ToString() ?? "";
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BlueLine", bookName, $"{unitName}.csv");

            _allVocabulary.Clear();

            try
            {
                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath);
                    // Skip header line
                    foreach (var line in lines.Skip(1))
                    {
                        var parts = line.Split(';');
                        if (parts.Length >= 2)
                        {
                            _allVocabulary.Add(new Vocabulary { English = parts[0].Trim(), German = parts[1].Trim() });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Vokabeln: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartNewSession()
        {
            if (_allVocabulary.Count == 0) return;

            _currentRoundVocab = new List<Vocabulary>(_allVocabulary);
            ShuffleList(_currentRoundVocab);

            _unknownVocab.Clear();
            _totalInRound = _currentRoundVocab.Count;
            _knownCount = 0;

            UpdateStats();
            ShowNextCard();
        }

        private void StartReviewRound()
        {
            if (_unknownVocab.Count == 0)
            {
                MessageBox.Show("Herzlichen Glückwunsch! Du hast alle Vokabeln gelernt.", "Fertig", MessageBoxButton.OK, MessageBoxImage.Information);
                StartNewSession();
                return;
            }

            MessageBox.Show($"Es geht weiter mit den {_unknownVocab.Count} Vokabeln, die du nicht wusstest.", "Neue Runde", MessageBoxButton.OK, MessageBoxImage.Information);

            _currentRoundVocab = new List<Vocabulary>(_unknownVocab);
            ShuffleList(_currentRoundVocab);

            _unknownVocab.Clear();
            _totalInRound = _currentRoundVocab.Count;
            _knownCount = 0;

            UpdateStats();
            ShowNextCard();
        }

        private void ShowNextCard()
        {
            if (_currentRoundVocab.Count == 0)
            {
                StartReviewRound();
                return;
            }

            _currentVocab = _currentRoundVocab[0];
            _currentRoundVocab.RemoveAt(0);

            _isCardFlipped = false;
            FlashcardWord.Text = _isEnglishToGerman ? _currentVocab.English : _currentVocab.German;
            FlashcardBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);

            EvalButtonsPanel.Visibility = Visibility.Collapsed;
            BtnNext.Visibility = Visibility.Collapsed;
            FlashcardContainer.IsEnabled = true;
        }

        private void UpdateStats()
        {
            StatsText.Text = $"{_knownCount} / {_totalInRound}";
        }

        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private void Flashcard_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isCardFlipped || !FlashcardContainer.IsEnabled) return;

            // Flip Animation
            DoubleAnimation shrinkAnimation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            shrinkAnimation.Completed += (s, ev) =>
            {
                // Update text when fully shrunk
                FlashcardWord.Text = _isEnglishToGerman ? _currentVocab.German : _currentVocab.English;
                FlashcardBorder.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF9C4")); // Light yellow back
                _isCardFlipped = true;

                DoubleAnimation growAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
                growAnimation.Completed += (s2, ev2) =>
                {
                    EvalButtonsPanel.Visibility = Visibility.Visible;
                };
                FlashcardScale.BeginAnimation(ScaleTransform.ScaleYProperty, growAnimation);
            };

            FlashcardScale.BeginAnimation(ScaleTransform.ScaleYProperty, shrinkAnimation);
            FlashcardContainer.IsEnabled = false; // Prevent double clicks
        }

        private void BtnKnown_Click(object sender, RoutedEventArgs e)
        {
            _knownCount++;
            UpdateStats();
            ShowNextCard();
        }

        private void BtnUnknown_Click(object sender, RoutedEventArgs e)
        {
            _unknownVocab.Add(_currentVocab);
            EvalButtonsPanel.Visibility = Visibility.Collapsed;
            BtnNext.Visibility = Visibility.Visible;
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            ShowNextCard();
        }

        private void DirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            var result = MessageBox.Show("Wenn du die Richtung änderst, beginnt der Durchlauf von vorne. Fortfahren?",
                                         "Richtung ändern",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _isEnglishToGerman = DirectionComboBox.SelectedIndex == 0;
                StartNewSession();
            }
            else
            {
                // Revert selection
                DirectionComboBox.SelectionChanged -= DirectionComboBox_SelectionChanged;
                DirectionComboBox.SelectedIndex = _isEnglishToGerman ? 0 : 1;
                DirectionComboBox.SelectionChanged += DirectionComboBox_SelectionChanged;
            }
        }

        private void BookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            LoadUnitsForSelectedBook();
            // Loading units will trigger UnitComboBox_SelectionChanged which handles vocabulary loading and session restart
        }

        private void UnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || UnitComboBox.SelectedItem == null) return;
            LoadVocabularyForCurrentSelection();
            StartNewSession();
        }
    }
}
