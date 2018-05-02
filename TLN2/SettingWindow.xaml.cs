using CoreTweet;
using Microsoft.VisualBasic;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace TLN2
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {
        // 各種Key
        private string consumerKey;
        private string consumerSecret;
        private string accessToken;
        private string accessTokenSecret;

        // プロフィール
        private UserResponse profile;

        // 設定の一時変数
        private bool isUserStreamingMode = Properties.Settings.Default.IsUserStreamingMode;
        private bool isBouyomiChanMode = Properties.Settings.Default.IsBouyomiChanMode;
        private bool isOpenInBrowserMode = Properties.Settings.Default.IsOpenInBrowserMode;

        private MainWindow main;

        public SettingWindow(MainWindow main)
        {
            InitializeComponent();
            // メインウィンドウを取得
            this.main = main;
        }

        /// <summary>
        /// ウィンドウの初期化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // APIKeyの読み取り
            consumerKey = Properties.Settings.Default.ConsumerKey;
            consumerSecret = Properties.Settings.Default.ConsumerSecret;
            accessToken = Properties.Settings.Default.AccessToken;
            accessTokenSecret = Properties.Settings.Default.AccessTokenSecret;
            // チェックボックスの初期化
            FilterWord.Text = Properties.Settings.Default.FilterWord;
            UserStreamingMode.IsChecked = Properties.Settings.Default.IsUserStreamingMode;
            BouyomiChanMode.IsChecked = Properties.Settings.Default.IsBouyomiChanMode;
            OpenInBrowserMode.IsChecked = Properties.Settings.Default.IsOpenInBrowserMode;

            // 各種KEYが存在するならトークンの取得を試みる
            if (!string.IsNullOrEmpty(consumerKey) && !string.IsNullOrEmpty(consumerSecret) && !string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(accessTokenSecret))
            {
                try
                {
                    // トークンの取得
                    main.tokens = Tokens.Create(consumerKey, consumerSecret, accessToken, accessTokenSecret);
                    // プロフィールの取得
                    GetUserProfileAsync();
                    main.isAuthenticated = true;
                }
                catch
                {
                    MessageBox.Show(this, "自動ログインできませんでした。再設定が必要です。");
                }
            }
            if (main.isAuthenticated == true)
            {
                AuthenticateButton.Content = "ログアウト";
            }
        }

        /// <summary>
        /// プロフィールの取得
        /// </summary>
        public async void GetUserProfileAsync()
        {
            // 取得
            var task = Task.Run(() =>
            {
                profile = main.tokens.Account.VerifyCredentials();
            });
            await task;
            UserName.Text = $"{profile.Name}@{profile.ScreenName}";
        }

        /// <summary>
        /// トークンのリセット
        /// </summary>
        private void AuthenticateButton_Click(object sender, RoutedEventArgs e)
        {
            // ログインしているならログアウトする
            if (main.isAuthenticated == true)
            {
                // 設定画面のユーザー名を空白にする
                UserName.Text = "";
                AuthenticateButton.Content = "ログイン";
                // トークンのリセット
                Properties.Settings.Default.AccessToken = "";
                Properties.Settings.Default.AccessTokenSecret = "";
                Properties.Settings.Default.Save();
                // ストリーミングの切断
                main.StopUserStreaming();
                main.StopFilterStreaming();
                main.isAuthenticated = false;
            }
            // ログインしていないならAPISettingWindowを開く
            else
            {
                var apiKeySettingWindow = new APIKeySettingwindow(main, this);
                apiKeySettingWindow.ShowDialog();
                if (main.isAuthenticated == true)
                {
                    AuthenticateButton.Content = "ログアウト";
                }
            }
            
        }

        /// <summary>
        /// 設定の保存とストリーミング関連
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 一時変数から設定に保存
            Properties.Settings.Default.IsUserStreamingMode = isUserStreamingMode;
            Properties.Settings.Default.IsBouyomiChanMode = isBouyomiChanMode;
            Properties.Settings.Default.IsOpenInBrowserMode = isOpenInBrowserMode;
            Properties.Settings.Default.FilterWord = FilterWord.Text;
            Properties.Settings.Default.Save();
            // ログインしているならストリーミングを開始
            if (Properties.Settings.Default.IsUserStreamingMode && main.isAuthenticated == true)
            {
                main.StopUserStreaming();
                main.StartUserStreaming();
            }
            else
            {
                main.StopUserStreaming();
            }
            if (!string.IsNullOrEmpty(Properties.Settings.Default.FilterWord) && main.isAuthenticated == true)
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
        /// ユーザーストリームの設定
        /// </summary>
        private void UserStreamMode_Checked(object sender, RoutedEventArgs e)
        {
            isUserStreamingMode = true;
        }

        /// <summary>
        /// ユーザーストリームの設定
        /// </summary>
        private void UserStreamMode_Unchecked(object sender, RoutedEventArgs e)
        {
            isUserStreamingMode = false;
        }

        /// <summary>
        /// 棒読みちゃんの設定
        /// </summary>
        private void BouyomiChanMode_Checked(object sender, RoutedEventArgs e)
        {
            isBouyomiChanMode = true;
        }

        /// <summary>
        /// 棒読みちゃんの設定
        /// </summary>
        private void BouyomiChanMode_Unchecked(object sender, RoutedEventArgs e)
        {
            isBouyomiChanMode = false;
        }

        /// <summary>
        /// クリックでツイートのページを開くかどうか
        /// </summary>
        private void OpenInBrowserMode_Checked(object sender, RoutedEventArgs e)
        {
            isOpenInBrowserMode = true;
        }

        /// <summary>
        /// クリックでツイートのページを開くかどうか
        /// </summary>
        private void OpenInBrowserMode_Unchecked(object sender, RoutedEventArgs e)
        {
            isOpenInBrowserMode = false;
        }
    }
}
