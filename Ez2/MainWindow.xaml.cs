using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ez2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Queue<string> _queue = new Queue<string>();
        private readonly object _lock = new object();
        private CancellationTokenSource _cancellationTokenSource;
        private int _producedCount = 0;
        private int _consumedCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            UpdateStatus();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _queue.Clear();
            producerListBox.Items.Clear();
            consumerListBox.Items.Clear();
            _producedCount = 0;
            _consumedCount = 0;

            logText.Text = "Система запущена...\n";
            statusText.Text = "СИСТЕМА ЗАПУЩЕНА";

            // Запускаем производителя
            Task.Run(() => Producer(_cancellationTokenSource.Token));

            // Запускаем потребителя
            Task.Run(() => Consumer(_cancellationTokenSource.Token));

            UpdateStatus();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            statusText.Text = "СИСТЕМА ОСТАНОВЛЕНА";
            logText.Text += "Система остановлена.\n";
        }

        private void Producer(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string item = $"Элемент {++_producedCount} (Время: {DateTime.Now:HH:mm:ss.fff})";

                    lock (_lock)
                    {
                        _queue.Enqueue(item);

                        Dispatcher.Invoke(() =>
                        {
                            producerListBox.Items.Add(item);
                            logText.Text += $"[Производитель] Добавлен: {item}\n";
                        });

                        // Уведомляем потребителя, что появились элементы
                        Monitor.Pulse(_lock);
                    }

                    Thread.Sleep(500); // Добавляем элемент каждые 500мс
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void Consumer(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string item = null;

                    lock (_lock)
                    {
                        // Ждем, пока появятся элементы в очереди
                        while (_queue.Count == 0 && !cancellationToken.IsCancellationRequested)
                        {
                            Monitor.Wait(_lock, 1000); // Ждем с таймаутом 1 секунда
                        }

                        if (_queue.Count > 0)
                        {
                            item = _queue.Dequeue();
                            _consumedCount++;
                        }
                    }

                    if (item != null)
                    {
                        // Имитируем обработку
                        Thread.Sleep(300);

                        Dispatcher.Invoke(() =>
                        {
                            consumerListBox.Items.Add($"{item} → Обработан");
                            logText.Text += $"[Потребитель] Обработан: {item}\n";
                            UpdateStatus();
                        });

                        Thread.Sleep(700); // Общая задержка 1000мс между обработками
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void UpdateStatus()
        {
            Dispatcher.Invoke(() =>
            {
                queueStatusText.Text = $"Очередь: {_queue.Count} элементов | " +
                                     $"Произведено: {_producedCount} | " +
                                     $"Обработано: {_consumedCount}";
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            base.OnClosed(e);
        }
    }
}