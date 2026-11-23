using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace PawShelter
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _db = new DatabaseService();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFiltersAndLists();
            LoadAnimals();
        }

        private void LoadFiltersAndLists()
        {
            try
            {
                var species = _db.GetSpecies();

                var filterSpecies = new List<SpeciesItem> { new SpeciesItem { Id = 0, Name = "Все виды" } };
                filterSpecies.AddRange(species);
                SpeciesFilterComboBox.ItemsSource = filterSpecies;
                SpeciesFilterComboBox.SelectedIndex = 0;

                NewSpeciesComboBox.ItemsSource = species;
                if (species.Count > 0)
                    NewSpeciesComboBox.SelectedIndex = 0;

                StatusFilterComboBox.ItemsSource = new List<string> { "Все", "В приюте", "На лечении", "Передан" };
                StatusFilterComboBox.SelectedIndex = 0;

                NewStatusComboBox.ItemsSource = new List<string> { "В приюте", "На лечении", "Передан" };
                NewStatusComboBox.SelectedIndex = 0;

                var enclosures = new List<EnclosureItem> { new EnclosureItem { Id = 0, Code = "Без вольера" } };
                enclosures.AddRange(_db.GetOpenEnclosures());
                NewEnclosureComboBox.ItemsSource = enclosures;
                NewEnclosureComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки справочников:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAnimals()
        {
            try
            {
                int? selectedSpeciesId = null;
                if (SpeciesFilterComboBox.SelectedValue is int spId && spId > 0)
                    selectedSpeciesId = spId;

                string? selectedStatus = StatusFilterComboBox.SelectedItem as string;
                AnimalsGrid.ItemsSource = _db.GetAnimals(SearchTextBox.Text, selectedSpeciesId, selectedStatus);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка животных:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAnimals();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SpeciesFilterComboBox.SelectedIndex = 0;
            StatusFilterComboBox.SelectedIndex = 0;
            LoadAnimals();
        }

        private void AddAnimalButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NewSpeciesComboBox.SelectedValue is not int speciesId)
                {
                    MessageBox.Show("Выберите вид животного.", "Проверка ввода",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string ageText = NewAgeTextBox.Text.Replace(',', '.').Trim();
                if (!decimal.TryParse(ageText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal age))
                {
                    MessageBox.Show("Введите корректный возраст (например, 2 или 1.5).", "Проверка ввода",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int? enclosureId = null;
                if (NewEnclosureComboBox.SelectedValue is int encId && encId > 0)
                    enclosureId = encId;

                _db.AddAnimalWithEnclosure(NewNameTextBox.Text, speciesId, enclosureId, age);

                NewNameTextBox.Text = string.Empty;
                LoadFiltersAndLists();
                LoadAnimals();

                MessageBox.Show("Животное добавлено в базу данных!", "Успешно",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Операция отклонена",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ChangeStatusButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AnimalsGrid.SelectedItem is not AnimalRow selectedAnimal)
                {
                    MessageBox.Show("Выберите строку с животным в таблице.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (NewStatusComboBox.SelectedItem is not string newStatus)
                {
                    MessageBox.Show("Выберите новый статус.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _db.ChangeAnimalStatus(selectedAnimal.Id, newStatus);
                LoadFiltersAndLists();
                LoadAnimals();

                MessageBox.Show($"Статус животного «{selectedAnimal.Name}» изменён на «{newStatus}».", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Операция отклонена",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ArchiveAnimalButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AnimalsGrid.SelectedItem is not AnimalRow selectedAnimal)
                {
                    MessageBox.Show("Выберите строку с животным в таблице.", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _db.ArchiveAnimal(selectedAnimal.Id);
                LoadFiltersAndLists();
                LoadAnimals();

                MessageBox.Show($"Животное «{selectedAnimal.Name}» отправлено в архив.", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Операция отклонена",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string report = _db.GetSummaryReport();
                MessageBox.Show(report, "Сводный отчёт PawShelter",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчёта:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}