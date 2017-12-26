using System.Windows;

namespace TLN2
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {
        // 設定の一時変数
        private bool isUserStreamingMode = Properties.Settings.Default.IsUserStreamingMode;
        private bool isBouyomiChanMode = Properties.Settings.Default.IsBouyomiChanMode;
        private bool isOpenInBrowserMode = Properties.Settings.Default.IsOpenInBrowserMode;

        private MainWindow main;

        public SettingWindow(MainWindow main)
        {
            InitializeComponent();
            // 画面中央に表示
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            // メインウィンドウを取得
            this.main = main;
            if (!string.IsNullOrEmpty(main.profile.Name) && !string.IsNullOrEmpty(main.profile.ScreenName))
            {
                UserName.Text = $"{main.profile.Name}@{main.profile.ScreenName}";
            }
            // チェックボックスの初期化
            FilterWord.Text = Properties.Settings.Default.FilterWord;
            UserStreamingMode.IsChecked = Properties.Settings.Default.IsUserStreamingMode;
            BouyomiChanMode.IsChecked = Properties.Settings.Default.IsBouyomiChanMode;
            OpenInBrowserMode.IsChecked = Properties.Settings.Default.IsOpenInBrowserMode;
        }

        /// <summary>
        /// ユーザーストリームの設定
        /// </summary>
        private void UserStreamModeChecked(object sender, RoutedEventArgs e)
        {
            isUserStreamingMode = true;
        }

        /// <summary>
        /// ユーザーストリームの設定
        /// </summary>
        private void UserStreamModeUnchecked(object sender, RoutedEventArgs e)
        {
            isUserStreamingMode = false;
        }

        /// <summary>
        /// 棒読みちゃんの設定
        /// </summary>
        private void BouyomiChanModeChecked(object sender, RoutedEventArgs e)
        {
            isBouyomiChanMode = true;
        }

        /// <summary>
        /// 棒読みちゃんの設定
        /// </summary>
        private void BouyomiChanModeUnchecked(object sender, RoutedEventArgs e)
        {
            isBouyomiChanMode = false;
        }

        /// <summary>
        /// クリックでツイートのページを開くかどうか
        /// </summary>
        private void OpenInBrowserModeChecked(object sender, RoutedEventArgs e)
        {
            isOpenInBrowserMode = true;
        }

        /// <summary>
        /// クリックでツイートのページを開くかどうか
        /// </summary>
        private void OpenInBrowserModeUnchecked(object sender, RoutedEventArgs e)
        {
            isOpenInBrowserMode = false;
        }

        /// <summary>
        /// トークンのリセット
        /// </summary>
        private void ResetButtonClick(object sender, RoutedEventArgs e)
        {
            // 設定画面のユーザー名を空白にする
            UserName.Text = "";
            // トークンのリセット
            main.ResetToken();
            // 新しいユーザー名
            if (!string.IsNullOrEmpty(main.profile.Name) && !string.IsNullOrEmpty(main.profile.ScreenName))
            {
                UserName.Text = $"{main.profile.Name}@{main.profile.ScreenName}";
            }
        }

        /// <summary>
        /// 設定の保存とストリーミング関連
        /// </summary>
        private void OKButtonClick(object sender, RoutedEventArgs e)
        {
            // 一時変数から設定に保存
            Properties.Settings.Default.IsUserStreamingMode = isUserStreamingMode;
            Properties.Settings.Default.IsBouyomiChanMode = isBouyomiChanMode;
            Properties.Settings.Default.IsOpenInBrowserMode = isOpenInBrowserMode;
            Properties.Settings.Default.FilterWord = FilterWord.Text;
            Properties.Settings.Default.Save();
            if (Properties.Settings.Default.IsUserStreamingMode)
            {
                main.StopUserStreaming();
                main.StartUserStreaming();
            }
            else
            {
                main.StopUserStreaming();
            }
            if (!string.IsNullOrEmpty(Properties.Settings.Default.FilterWord))
            {
                main.StopFilterStreaming();
                main.StartFilterStreaming();
            }
            else
            {
                main.StopFilterStreaming();
            }
            Close();
        }

        /// <summary>
        /// キャンセルボタンを押したとき
        /// </summary>
        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
