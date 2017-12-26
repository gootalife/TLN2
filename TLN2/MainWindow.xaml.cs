using CoreTweet;
using CoreTweet.Streaming;
using FNF.Utility;
using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace TLN2
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        // 各種Key
        private string consumerKey;
        private string consumerSecret;
        private string accessToken;
        private string accessTokenSecret;

        // トークン関連
        private Tokens tokens;
        private OAuth.OAuthSession session;
        public UserResponse profile;

        // ストリームのリソース
        private IDisposable filterStream;
        private IDisposable userStream;

        // ストリームの状態
        private bool isStreaming;
        private bool isFilterStreaming;

        // テキストブロックにクリックイベントがないのはなぜだ
        private bool mouseLeftButtonDown;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ウィンドウが開いたとき
        /// </summary>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            consumerKey = Properties.Resources.ConsumerKey;
            consumerSecret = Properties.Resources.ConsumerSecret;
            accessToken = Properties.Settings.Default.AccessToken;
            accessTokenSecret = Properties.Settings.Default.AccessTokenSecret;
            // タスクトレイのアイコン設定
            TaskTrayIcon.Icon = Properties.Resources.Icon;
            // 認証されていない時
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(accessTokenSecret))
            {
                // 認証設定へ
                Authenticate();
            }
            else
            {
                // トークン生成
                tokens = Tokens.Create(consumerKey, consumerSecret, accessToken, accessTokenSecret);
                // プロフィールの取得
                GetUserProfileAsync();
            }
        }

        /// <summary>
        /// 認証
        /// </summary>
        private async void Authenticate()
        {
            try
            {
                // 認証用のURL
                session = OAuth.Authorize(consumerKey, consumerSecret);
                Uri url = session.AuthorizeUri;
                // ブラウザを起動
                System.Diagnostics.Process.Start(url.ToString());
                // 取得
                string pinCode = "";
                var task = Task.Run(() =>
                {
                    pinCode = Interaction.InputBox("PINコードを入力", "認証設定", "", -1, -1);
                });
                await task;
                // トークンを取得して保存
                tokens = OAuth.GetTokens(session, pinCode);
                Properties.Settings.Default.AccessToken = tokens.AccessToken.ToString();
                Properties.Settings.Default.AccessTokenSecret = tokens.AccessTokenSecret.ToString();
                Properties.Settings.Default.Save();
                GetUserProfileAsync();
                MessageBox.Show("認証設定を保存");
            }
            catch
            {
                MessageBox.Show("入力エラー");
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// 認証のリセット
        /// </summary>
        public void ResetToken()
        {
            Properties.Settings.Default.AccessToken = null;
            Properties.Settings.Default.AccessTokenSecret = null;
            Properties.Settings.Default.Save();
            // ストリーミングの切断
            StopUserStreaming();
            StopFilterStreaming();
            Authenticate();
        }

        /// <summary>
        /// プロフィールの取得
        /// </summary>
        private async void GetUserProfileAsync()
        {
            await Task.Run(() =>
            {
                profile = tokens.Account.VerifyCredentials();
            });
        }

        /// <summary>
        /// ユーザーストリーミング
        /// </summary>
        public void StartUserStreaming()
        {
            var stream = tokens.Streaming.UserAsObservable().Publish();
            // ツイート・リツイートのみ取得
            stream.OfType<StatusMessage>().Subscribe(x => CreateTextBlock(x.Status),
                                                    onError: ex => ErrorUserStreaming());
            userStream = stream.Connect();
            isStreaming = true;
        }

        /// <summary>
        /// フィルターストリーミング
        /// </summary>
        public void StartFilterStreaming()
        {
            var stream = tokens.Streaming.FilterAsObservable(track => Properties.Settings.Default.FilterWord).Publish();
            // ツイート・リツイートのみ取得
            stream.OfType<StatusMessage>().Subscribe(x => CreateTextBlock(x.Status),
                                                    onError: ex => ErrorFilterStreaming());
            filterStream = stream.Connect();
            isFilterStreaming = true;
        }

        /// <summary>
        /// ユーザーストリーミングの再接続
        /// </summary>
        private void ErrorUserStreaming()
        {
            // 再接続
            StopUserStreaming();
            StartUserStreaming();
        }

        /// <summary>
        /// ユーザーストリーミングの停止
        /// </summary>
        public void StopUserStreaming()
        {
            // ストリームを利用しているなら
            if (isStreaming == true)
            {
                // 切断
                userStream.Dispose();
                isStreaming = false;
            }
        }

        /// <summary>
        /// フィルターストリーミングの再接続
        /// </summary>
        public void ErrorFilterStreaming()
        {
            // 再接続
            StopFilterStreaming();
            StartFilterStreaming();
        }

        /// <summary>
        /// フィルターストリーミングの停止
        /// </summary>
        public void StopFilterStreaming()
        {
            // 利用中なら
            if (isFilterStreaming == true)
            {
                // 切断
                filterStream.Dispose();
                isFilterStreaming = false;
            }
        }

        /// <summary>
        /// ツイートからテキストブロックの生成
        /// </summary>
        private void CreateTextBlock(Status status)
        {
            // 日本圏のみ
            if (status.User.Language == "ja")
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    var pattern = @"s?https?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+";
                    // ツイートからURLを抽出
                    var r = new Regex(pattern, RegexOptions.IgnoreCase);
                    Match m = r.Match(status.Text);
                    string linkUrl = m.Value;
                    // ツイートからURL,改行を削除
                    status.Text = Regex.Replace(status.Text, pattern, "").Replace("\r", "").Replace("\n", " ");
                    // 棒読みちゃん
                    if (Properties.Settings.Default.IsBouyomiChanMode)
                    {
                        Bouyomi($"{status.User.Name}、{status.Text}");
                    }
                    // テキストブロックの初期化
                    var tweet = new TextBlock
                    {
                        FontSize = 30,
                        TextWrapping = TextWrapping.NoWrap,
                        Opacity = 0,
                        Text = $"{status.User.Name}@{status.User.ScreenName} : {status.Text} ",
                        Foreground = new SolidColorBrush(Colors.White),
                        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
                        Effect = new DropShadowEffect
                        {
                            ShadowDepth = 3,
                            Opacity = 1,
                            BlurRadius = 0
                        }
                    };
                    // URLの部分
                    var link = new Hyperlink();
                    link.Click += (s, e) =>
                    {
                        System.Diagnostics.Process.Start(linkUrl);
                    };
                    link.Inlines.Add(new Run
                    {
                        Text = linkUrl,
                    });
                    tweet.Inlines.Add(link);
                    // ブラウザで開くモードが有効なら
                    if (Properties.Settings.Default.IsOpenInBrowserMode)
                    {
                        // クリックイベントもどき
                        // テキスト上でボタンを押して離したとき
                        string tweetUrl = $"http://twitter.com/{status.User.ScreenName}/status/{status.Id}";
                        tweet.MouseLeftButtonDown += (s, e) =>
                        {
                            mouseLeftButtonDown = true;
                        };
                        tweet.MouseLeftButtonUp += (s, e) =>
                        {
                            if (mouseLeftButtonDown == true)
                            {
                                var task = Task.Run(() =>
                                {
                                    System.Diagnostics.Process.Start(tweetUrl);
                                });
                            }
                            mouseLeftButtonDown = false;
                        };
                    }
                    // ウィンドウに追加
                    Root.Children.Add(tweet);
                    // 描写が完了してからアニメーション
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MoveTweet(tweet);
                        tweet.Opacity = 100;
                    }), DispatcherPriority.Loaded);
                }));
            }
        }

        /// <summary>
        /// ツイートのアニメーション
        /// </summary>
        private void MoveTweet(TextBlock tweet)
        {
            var random = new Random();
            // ランダムな高さに出現
            Canvas.SetTop(tweet, random.Next((int)tweet.ActualHeight, (int)(Height * 0.9)));
            // ランダムな時間の間流れる
            double time = random.Next(9000, 13000);
            // 右画面外から左画面外へ
            var moveAnimation = new DoubleAnimation
            {
                From = Width,
                To = -1 * tweet.ActualWidth - 10,
                Duration = TimeSpan.FromMilliseconds(time)
            };
            // 流れたら削除する
            var deleteTimer = new DispatcherTimer(DispatcherPriority.SystemIdle)
            {
                Interval = TimeSpan.FromMilliseconds(time),
            };
            deleteTimer.Tick += (s, e) =>
            {
                deleteTimer.Stop();
                Root.Children.Remove(tweet);
            };
            // そぉぃ
            tweet.BeginAnimation(LeftProperty, moveAnimation);
            deleteTimer.Start();
        }

        /// <summary>
        /// 終了ボタン
        /// </summary>
        private void QuitButtonClick(object sender, RoutedEventArgs e)
        {
            // ストリーミングの切断
            userStream.Dispose();
            filterStream.Dispose();
            Close();
        }

        /// <summary>
        /// 閉じる前
        /// </summary>
        private void WindowClosed(object sender, EventArgs e)
        {
            // ストリーミングの切断
            userStream.Dispose();
            filterStream.Dispose();
        }

        /// <summary>
        /// 棒読みちゃんで読み上げ
        /// </summary>
        private void Bouyomi(string text)
        {
            var bc = new BouyomiChanClient();
            bc.AddTalkTask(text);
            bc.Dispose();
        }

        /// <summary>
        /// 設定画面を開く
        /// </summary>
        private void OpenSettingWindowClick(object sender, EventArgs e)
        {
            var window = new SettingWindow(this);
            window.Show();
        }
    }
}
