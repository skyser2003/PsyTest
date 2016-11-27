using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Collections;
using System.Windows.Threading;

namespace Experiment {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public enum ExperimentLanguage {
            English,
            Vietnamese
        }

        public class ImageWordPair {
            public ExperimentLanguage lang;
            public BitmapImage image;
            public string word;
        }

        private static Random rng = new Random();

        private List<BitmapImage> imageList = new List<BitmapImage>();
        private List<string> engList = new List<string>();
        private List<string> vietList = new List<string>();

        private DispatcherTimer timer;
        private ExperimentLanguage lang;
        private IEnumerator next;
        private List<ImageWordPair> testCase = new List<ImageWordPair>();
        private List<ImageWordPair> randCase = new List<ImageWordPair>();
        private List<CheckBox> rememberList = new List<CheckBox>();
        private DateTime now;

        public MainWindow()
        {
            InitializeComponent();

            Init();
        }

        public void Init()
        {
            var files = new List<string>();
            var count = Directory.GetFiles("image").Length;

            for (int i = 1; i <= count; ++i) {
                var filename = Directory.GetCurrentDirectory() + "\\image\\" + i + ".";

                if (File.Exists(filename + "png")) {
                    filename += "png";
                }
                else if (File.Exists(filename + "jpg")) {
                    filename += "jpg";
                }
                else if (File.Exists(filename + "bmp")) {
                    filename += "bmp";
                }
                else {
                    MessageBox.Show("지원하지 않는 이미지 포맷이 있습니다. " + filename);
                    Application.Current.Shutdown();
                }

                files.Add(filename);
            }

            foreach (var file in files) {
                var bitmap = new BitmapImage(new Uri(file));
                imageList.Add(bitmap);
            }

            engList.AddRange(File.ReadAllLines("eng_list.txt"));
            vietList.AddRange(File.ReadAllLines("viet_list.txt"));

            now = DateTime.Now;

            intro.Content = "언어를 선택해주세요.";

            // Elements
            image.Visibility = Visibility.Hidden;
            label.Visibility = Visibility.Hidden;
            button_end.Visibility = Visibility.Hidden;
            word1.Visibility = Visibility.Hidden;
            button_remember_done.Visibility = Visibility.Hidden;
        }

        public void Start()
        {
            intro.Visibility = Visibility.Hidden;
            button_english.Visibility = Visibility.Hidden;
            button_vietnamese.Visibility = Visibility.Hidden;

            image.Visibility = Visibility.Visible;
            label.Visibility = Visibility.Visible;

            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Tick += new EventHandler(Next);
            timer.Start();

            Next(null, null);
        }

        public void Remember()
        {
            timer.Stop();

            image.Visibility = Visibility.Hidden;
            label.Visibility = Visibility.Hidden;
            button_remember_done.Visibility = Visibility.Visible;

            word1.Visibility = Visibility.Visible;
            word1.Content = testCase[0].word;

            rememberList.Add(word1);

            for (int i = 1; i < testCase.Count; ++i) {
                var word = new CheckBox();
                word.Content = testCase[i].word;
                word.Visibility = Visibility.Visible;

                var margin = word1.Margin;
                margin.Top += i * 20;
                word.Margin = margin;
                word.Height = word1.Height;
                word.Width = word1.Width;
                word.HorizontalAlignment = HorizontalAlignment.Left;

                RootGrid.Children.Add(word);

                rememberList.Add(word);
            }
        }

        public void End()
        {
            // Save result
            var remembered = new List<string>();

            for (int i = 0; i < rememberList.Count; ++i) {
                if (rememberList[i].IsChecked == true) {
                    var pair = testCase[i];
                    remembered.Add(pair.word);
                }
            }

            var sb = new StringBuilder();
            foreach (var word in remembered) {
                sb.AppendLine(word);
            }

            var filename = "result_";
            filename += now.Year + "-";
            filename += now.Month + "-";
            filename += now.Day + "---";
            filename += now.Hour + "-";
            filename += now.Minute + "-";
            filename += now.Second;
            filename += ".txt";

            File.WriteAllText(filename, sb.ToString());

            // Element
            foreach (var word in rememberList) {
                word.Visibility = Visibility.Hidden;
            }
            button_remember_done.Visibility = Visibility.Hidden;

            intro.Visibility = Visibility.Visible;
            button_end.Visibility = Visibility.Visible;

            image.Visibility = Visibility.Hidden;
            label.Visibility = Visibility.Hidden;

            intro.Content = "수고하셨습니다";
        }

        public void Next(object sender, EventArgs arg)
        {
            if (next.MoveNext() == false) {
                Remember();
                return;
            }

            var pair = next.Current as ImageWordPair;
            image.Source = pair.image;
            label.Content = pair.word;
        }

        private BitmapSource CreateBitmap(Bitmap bitmap)
        {
            return Imaging.CreateBitmapSourceFromHBitmap
            (
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );
        }

        private void button_english_Click(object sender, RoutedEventArgs e)
        {
            lang = ExperimentLanguage.English;
            for (int i = 0; i < imageList.Count; ++i) {
                var pair = new ImageWordPair();
                pair.lang = lang;
                pair.image = imageList[i];
                pair.word = engList[i];

                testCase.Add(pair);
            }

            randCase = Shuffle(testCase);
            next = randCase.GetEnumerator();

            Start();
        }

        private void button_vietnamese_Click(object sender, RoutedEventArgs e)
        {
            lang = ExperimentLanguage.Vietnamese;
            for (int i = 0; i < imageList.Count; ++i) {
                var pair = new ImageWordPair();
                pair.lang = lang;
                pair.image = imageList[i];
                pair.word = vietList[i];

                testCase.Add(pair);
            }

            testCase = Shuffle(testCase);
            next = testCase.GetEnumerator();

            Start();
        }

        public static List<T> Shuffle<T>(IList<T> list)
        {
            var ret = new List<T>(list);

            int n = ret.Count;
            while (n > 1) {
                n--;
                int k = rng.Next(n + 1);
                T value = ret[k];
                ret[k] = ret[n];
                ret[n] = value;
            }

            return ret;
        }

        private void button_end_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void button_remember_done_Click(object sender, RoutedEventArgs e)
        {
            End();
        }
    }
}
