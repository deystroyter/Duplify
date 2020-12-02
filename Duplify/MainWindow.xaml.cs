using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace Duplify
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    partial class MainWindow : Window
    {
        private ObservableCollection<LvObject> LvObjects;
        private readonly DispatcherTimer _timer;
        private bool StoppedByButton = false;
        private bool ThreadIsPaused = false;
        public MainWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();


            LvObjects = new ObservableCollection<LvObject>();
        }

        //Обработка тика Dispatcher.Timer (для обновления UI)
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                //если поток ещё выполняется
                if (ThMainP != null && ThMainP.IsAlive && !ThreadIsPaused)
                {
                    StatusBarTextBlock.Text = GlobalData.StatusBarText;
                    CurrentPercentTextBlock.Text = GlobalData.ProgressBarValue + "%";
                    ProgBar.Value = GlobalData.ProgressBarValue;
                }
                //если поток завершился или был прерван (не был приостановлен)
                else if (!ThreadIsPaused)
                {
                    UIAfterEnd();
                }
                else if (ThreadIsPaused)
                {
                    
                }
            }
            catch (NullReferenceException)
            {
                UIAfterEnd();
            }

        }


        #region DirectionSelect

        //Обработка кнопки FolderChange
        private void FolderChange_Click(object sender, RoutedEventArgs e)
        {
            WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog();
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                @UserPath.Text = @folderDialog.SelectedPath;
            }
            else if (folderDialog.SelectedPath != "")
            {
                UserPath_Error();
            }
            folderDialog.Dispose();
        }

        //Обработка при неверно указанном пути
        private void UserPath_Error()
        {
            MessageBox.Show("Пожалуйста, укажите существующий путь!", "Duplify - Path Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            GlobalData.GlPath = @"D:\";

        }
        #endregion

        //Блокировка элементов UI после начала поиска
        private void UIAfterStart()
        {
            LvObjects.Clear();

            ListViewTemplate.ItemsSource = null;
            ListViewTemplate.Items.Clear();

            GlobalData.FileSizeIgnore = (bool)HiddenFilesIgnoreCheckBox.IsChecked;
            GlobalData.HiddenFilesIgnore = (bool)HiddenFilesIgnoreCheckBox.IsChecked;

            ThreadIsPaused = false;
            //Скрыть/сделать недоступными
            //Checkboxes
            FileNamesIgnoreCheckBox.IsEnabled = false;
            CreationDateIgnoreCheckBox.IsEnabled = false;
            HiddenFilesIgnoreCheckBox.IsEnabled = false;
            FileSizeIgnoreCheckBox.IsEnabled = false;
            //DirectionSelect
            UserPath.IsEnabled = false;
            FolderChange.IsEnabled = false;
            //Buttons
            StartSearch.IsEnabled = false;
            StartSearch.Visibility = Visibility.Hidden;
            DeleteButton.IsEnabled = false;
            DeleteButton.Visibility = Visibility.Hidden;
            //ListView
            ListViewTemplate.IsEnabled = false;

            //Показать/сделать доступными
            PauseButton.Visibility = Visibility.Visible;
            PauseButton.IsEnabled = true;
            StopButton.Visibility = Visibility.Visible;
            StopButton.IsEnabled = true;

            GlobalData.ProgressBarValue = 0;
            ProgBar.Value = 0;
            CurrentPercentTextBlock.Text = "0%";
        }


        //Разблокировка элементов UI после конца поиска (запуск из таймера)
        private void UIAfterEnd()
        {
            _timer.Stop();
            ThreadIsPaused = false;

            //Если была нажата кнопка "Стоп"
            if (StoppedByButton == false)
            {
                ProgBar.Value = 100;
                CurrentPercentTextBlock.Text = "100%";

                StatusBarTextBlock.Text = "Готово!";
            }
            else { StatusBarTextBlock.Text = "Поиск был прерван..."; }

            FillListView();

            //Показать/сделать доступными
            //Checkboxes
            FileNamesIgnoreCheckBox.IsEnabled = true;
            CreationDateIgnoreCheckBox.IsEnabled = true;
            HiddenFilesIgnoreCheckBox.IsEnabled = true;
            FileSizeIgnoreCheckBox.IsEnabled = true;
            //DirectionSelect
            UserPath.IsEnabled = true;
            FolderChange.IsEnabled = true;
            //Buttons
            StartSearch.IsEnabled = true;
            StartSearch.Visibility = Visibility.Visible;
            DeleteButton.IsEnabled = true;
            DeleteButton.Visibility = Visibility.Visible;
            //ListView
            ListViewTemplate.IsEnabled = true;

            //Скрыть/сделать недоступными
            StopButton.Visibility = Visibility.Hidden;
            StopButton.IsEnabled = false;
            PauseButton.Visibility = Visibility.Hidden;
            PauseButton.IsEnabled = false;
            ContinueButton.Visibility = Visibility.Hidden;
            ContinueButton.IsEnabled = false;
        }

        #region Заполнение ListView
        private void FillListView()
        {
            LvObjects = new ObservableCollection<LvObject>();
            ListViewTemplate.ItemsSource = LvObjects;
            foreach (KeyValuePair<String, List<String>> kvp in GlobalData.hashes)
            {
                //Если есть дубликаты
                if (kvp.Value.Count > 1)
                {

                    //Влияние FileNamesIgnoreCheckBox
                    if ((bool)FileNamesIgnoreCheckBox.IsChecked == false)
                    {
                        FileNamesSort(kvp);
                    }
                    //Влияние CreationDateIgnoreCheckBox
                    else if((bool)FileNamesIgnoreCheckBox.IsChecked == true && (bool)CreationDateIgnoreCheckBox.IsChecked == false)
                    {
                        CreationDateSort(kvp);
                    }
                    else
                    {
                        LvObjects.Add(new LvObject(kvp.Key, kvp.Key));

                        foreach (string path in kvp.Value)
                        {
                            FileInfo fileInfo = new FileInfo(path);
                            LvObjects.Add(new LvObject(path, Math.Round((double)(fileInfo.Length / 1024), 2)));

                        }
                    }

                }
            }
        }
        private void CreationDateSort(KeyValuePair<String, List<String>> kvp)
        {

            //Ключ - дата создания файла, значение - список путей
            Dictionary<String, List<String>> HashDatePathes = new Dictionary<string, List<string>>();

            foreach (string path in kvp.Value)
            {
                string CreationDate = System.IO.File.GetCreationTime(path).ToString(@"yyyy-MM-dd");
                //пробуем добавить новое имя файла как ключ
                try
                {
                    HashDatePathes[kvp.Key + " + " + CreationDate].Add(path);

                }
                catch (NullReferenceException)
                {
                    HashDatePathes[kvp.Key + " + " + CreationDate] = new List<String> { };
                    HashDatePathes[kvp.Key + " + " + CreationDate].Add(path);
                }
                //если такого ключа ещё не существует
                catch (KeyNotFoundException)
                {
                    HashDatePathes[kvp.Key + " + " + CreationDate] = new List<String> { };
                    HashDatePathes[kvp.Key + " + " + CreationDate].Add(path);
                }
            }

            foreach (KeyValuePair<String, List<String>> kvpHDP in HashDatePathes)
            {
                if (kvpHDP.Value.Count > 1)
                {
                    LvObjects.Add(new LvObject(kvpHDP.Key, kvpHDP.Key));

                    foreach (string path in kvpHDP.Value)
                    {
                        FileInfo fileInfo = new FileInfo(path);
                        LvObjects.Add(new LvObject(path, Math.Round((double)(fileInfo.Length / 1024), 2)));
                    }
                }
            }
        }
        private void FileNamesSort(KeyValuePair<String, List<String>> kvp)
        {
            //Ключ - имя файла, значение - список путей
            Dictionary<String, List<String>> NamePathes = new Dictionary<string, List<string>>();

            foreach (string path in kvp.Value)
            {
                string FileName = System.IO.Path.GetFileName(path);
                //пробуем добавить новое имя файла как ключ
                try
                {
                    NamePathes[kvp.Key + " + " + FileName].Add(path);

                }
                catch (NullReferenceException)
                {
                    NamePathes[kvp.Key + " + " + FileName] = new List<String> { };
                    NamePathes[kvp.Key + " + " + FileName].Add(path);
                }
                //если такого ключа ещё не существует
                catch (KeyNotFoundException)
                {
                    NamePathes[kvp.Key + " + " + FileName] = new List<String> { };
                    NamePathes[kvp.Key + " + " + FileName].Add(path);
                }
            }
            foreach (KeyValuePair<String, List<String>> kvpNP in NamePathes)
            {
                if (kvpNP.Value.Count > 1 && (bool)CreationDateIgnoreCheckBox.IsChecked == true)
                {
                    LvObjects.Add(new LvObject(kvpNP.Key, kvpNP.Key));

                    foreach (string path in kvpNP.Value)
                    {
                        FileInfo fileInfo = new FileInfo(path);
                        LvObjects.Add(new LvObject(path, Math.Round((double)(fileInfo.Length / 1024), 2)));
                    }
                }
                else if (kvpNP.Value.Count > 1 && (bool)CreationDateIgnoreCheckBox.IsChecked == false)
                {
                    CreationDateSort(kvpNP);
                }
            }
        }
        #endregion

        #region Работа с потоками
        private Thread ThMainP;
        //Старт потока
        private void StartThread()
        {
            if (ThMainP == null || !ThMainP.IsAlive)
            {
                ThMainP = new Thread(DuFinder.MainProgram);
                ThMainP.Start();
            }
        }
        //Остановка потока
        private void StopThread()
        {
            if (ThMainP != null && ThMainP.IsAlive)
            {
                if (ThreadIsPaused)
                {
                    ThMainP.Resume();
                }
                ThMainP.Abort();
                ThMainP = null;
            }
        }
        #endregion

        #region Обработка кликов по кнопкам
        //Обработка клика по кнопке "Начать поиск"
        private void StartSearch_Click(object sender, RoutedEventArgs e)
        {
            ListViewTemplate.ItemsSource = null;
            if (System.IO.Directory.Exists(UserPath.Text))
            {
                GlobalData.GlPath = UserPath.Text;
                UIAfterStart();
                _timer.Tick += Timer_Tick;
                _timer.Interval = TimeSpan.FromMilliseconds(100);

                StartThread();
                _timer.Start();

                Console.Beep(38, 50);

            }
            else UserPath_Error();

        }
        //Обработка клика по кнопке "Удалить выбранные"
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            List<LvObject> DeleteList = new List<LvObject>();
            int tempcount = 0;

            foreach (LvObject s in ListViewTemplate.Items)
            {
                try
                {
                    if (s.IsCheckBox && s.CheckBoxIsChecked)
                    {
                       
                        MessageBox.Show(s.Value);
                        DeleteList.Add(s);
                        //File.Delete(s.Value);
                        tempcount++;
                    }
                }
                catch { MessageBox.Show("Some problems with file: " + s.Value); }
            }


            foreach (LvObject s in DeleteList)
            {
                LvObjects.Remove(s);
            }
            if (tempcount != 0)
            {
                MessageBox.Show("Успешно удалено " + tempcount + " из " + DeleteList.Count + " выбранных файлов.", "Duplify - Delete Completed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (DeleteList.Count == 0) { MessageBox.Show("Похоже, вы забыли выбрать файлы :)", "Duplify - No Files Were Selected", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
        //Обработка клика по кнопке "Возобновить"
        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (ThMainP != null && ThMainP.IsAlive)
            {
                ThreadIsPaused = false;
                ThMainP.Resume();
                ContinueButton.Visibility = Visibility.Hidden;
                ContinueButton.IsEnabled = false;
                PauseButton.Visibility = Visibility.Visible;
                PauseButton.IsEnabled = true;
            }
        }
        //Обработка клика по кнопке "Стоп"
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (ThMainP != null && ThMainP.IsAlive)
            {
                StoppedByButton = true;
                StopThread();
            }
        }
        //Обработка клика по кнопке "Пауза"
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (ThMainP != null && ThMainP.IsAlive)
            {
                ThreadIsPaused = true;
                ThMainP.Suspend();
                StatusBarTextBlock.Text = "Поиск приостановлен...";
                GlobalData.StatusBarText = "Поиск приостановлен...";
                ContinueButton.Visibility = Visibility.Visible;
                ContinueButton.IsEnabled = true;
                PauseButton.Visibility = Visibility.Hidden;
                PauseButton.IsEnabled = false;
            }
        }
        #endregion

    }

}



