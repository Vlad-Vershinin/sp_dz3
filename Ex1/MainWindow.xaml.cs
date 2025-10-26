using System.Windows;

namespace Ex1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> _items = new List<string>();
        private readonly object _lock = new object();
        private int _counter = 0;

        public MainWindow()
        {
            InitializeComponent();
            debugText.Text = "Приложение готово к работе.\n";
        }

        private void RaceButton_Click(object sender, RoutedEventArgs e)
        {
            _items.Clear();
            itemsListBox.Items.Clear();
            _counter = 0;
            debugText.Text = "Запуск гонки данных...\n";
            statusText.Text = "Гонка данных запущена";

            Task.Run(() => AddItemsUnsafe("Поток A: ", 250));
            Task.Run(() => AddItemsUnsafe("Поток B: ", 250));
        }

        private void AddItemsUnsafe(string prefix, int count)
        {
            for (int i = 0; i < count; i++)
            {
                _counter++;
                _items.Add($"{prefix}{i} (счетчик: {_counter})");

                Dispatcher.Invoke(() =>
                {
                    itemsListBox.Items.Add($"{prefix}{i} (счетчик: {_counter})");
                    debugText.Text += $"[{Thread.CurrentThread.ManagedThreadId}] Добавлено: {prefix}{i}\n";
                });

                Thread.Sleep(1);
            }

            Dispatcher.Invoke(() =>
            {
                debugText.Text += $"Гонка завершена. Итоговый счетчик: {_counter}\n";
                statusText.Text = $"Гонка завершена. Счетчик: {_counter}, Ожидалось: {count * 2}";
            });
        }

        private void SafeButton_Click(object sender, RoutedEventArgs e)
        {
            _items.Clear();
            itemsListBox.Items.Clear();
            _counter = 0;
            debugText.Text = "Запуск безопасного добавления...\n";
            statusText.Text = "Безопасное добавление запущено";

            Task.Run(() => AddItemsSafe("Поток A: ", 250));
            Task.Run(() => AddItemsSafe("Поток B: ", 250));
        }
        
        private void AddItemsSafe(string prefix, int count)
        {
            for (int i = 0; i < count; i++)
            {
                lock (_lock)
                {
                    _counter++;
                    _items.Add($"{prefix}{i} (счетчик: {_counter})");

                    Dispatcher.Invoke(() =>
                    {
                        itemsListBox.Items.Add($"{prefix}{i} (счетчик: {_counter})");
                        debugText.Text += $"[{Thread.CurrentThread.ManagedThreadId}] Безопасно добавлено: {prefix}{i}\n";
                    });
                }

                Thread.Sleep(1);
            }

            Dispatcher.Invoke(() =>
            {
                debugText.Text += $"Безопасное добавление завершено. Итоговый счетчик: {_counter}\n";
                statusText.Text = $"Безопасное добавление завершено. Счетчик: {_counter}";
            });
        }

        private void MonitorButton_Click(object sender, RoutedEventArgs e)
        {
            debugText.Text = "Тестирование Monitor.TryEnter...\n";
            statusText.Text = "Тестирование Monitor.TryEnter";

            Task.Run(() => TestMonitorTimeout());
        }

        private void TestMonitorTimeout()
        {
            bool lockAcquired = false;

            Task.Run(() =>
            {
                lock (_lock)
                {
                    Dispatcher.Invoke(() => debugText.Text += "[3] Долгая операция начата...\n");
                    Thread.Sleep(3000);
                    Dispatcher.Invoke(() => debugText.Text += "[3] Долгая операция завершена\n");
                }
            });

            Thread.Sleep(100);

            try
            {
                lockAcquired = Monitor.TryEnter(_lock, TimeSpan.FromSeconds(1));

                if (lockAcquired)
                {
                    Dispatcher.Invoke(() =>
                    {
                        debugText.Text += $"[{Thread.CurrentThread.ManagedThreadId}] Блокировка захвачена\n";
                        statusText.Text = "Блокировка успешно захвачена";
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        debugText.Text += $"[{Thread.CurrentThread.ManagedThreadId}] Таймаут! Блокировка не захвачена\n";
                        statusText.Text = "Таймаут - блокировка не захвачена";
                    });
                }
            }
            finally
            {
                if (lockAcquired)
                {
                    Monitor.Exit(_lock);
                }
            }
        }
    }
}