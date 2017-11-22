using CoreTweet;
using CoreTweet.Streaming;
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

        // ストリームのリソース
        private IDisposable filteredStream;
        private IDisposable userStream;

        // ストリームの状態
        private bool isStreaming;
        private bool isSearching;
        string word = "";

        // テキストブロックにクリックイベントがないのはなぜだ
        private bool mouseLeftButtonDown;

        public MainWindow()
        {
            InitializeComponent();
            consumerKey = Properties.Resources.ConsumerKey;
            consumerSecret = Properties.Resources.ConsumerSecret;
            accessToken = Properties.Settings.Default.AccessToken;
            accessTokenSecret = Properties.Settings.Default.AccessTokenSecret;
            // タスクトレイのアイコン設定
            TaskTrayIcon.Icon = Properties.Resources.Icon;
            // Jumpモードの設定
            if (Properties.Settings.Default.IsJumpMode == true)
            {
                JumpModeMenu.Header = "ツイートを開く を無効にする";
            }
            else
            {
                JumpModeMenu.Header = "ツイートを開く を有効にする";
            }
            // 認証されていない時
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(accessTokenSecret))
            {
                // 認証設定へ
                Authenticate();
            }
            else
            {
                // トークン生成
                tokens = Tokens.Create(consumerKey, consumerSecret,accessToken, accessTokenSecret);
                // プロフィールの取得
                GetUserProfile();
            }
            // ストリーミングの開始
            StartUserStreaming();
            isStreaming = true;
        }

        // 認証
        private void Authenticate()
        {
            // 認証用のURL
            session = OAuth.Authorize(consumerKey, consumerSecret);
            Uri url = session.AuthorizeUri;
            // ブラウザを起動
            System.Diagnostics.Process.Start(url.ToString());
            // 取得
            string pinCode = Interaction.InputBox("PINコードを入力", "認証設定", "", -1, -1);
            try
            {
                // トークンを取得して保存
                tokens = OAuth.GetTokens(session, pinCode);
                Properties.Settings.Default.AccessToken = tokens.AccessToken.ToString();
                Properties.Settings.Default.AccessTokenSecret = tokens.AccessTokenSecret.ToString();
                Properties.Settings.Default.Save();
                GetUserProfile();
                MessageBox.Show("認証設定を保存");
            }
            catch
            {
                MessageBox.Show("入力エラー");
            }
        }

        // 認証のリセット
        private void ResetButtonClick(object sender, RoutedEventArgs e)
        {
            UserName.Text = "TLN2\n未認証";
            Properties.Settings.Default.AccessToken = null;
            Properties.Settings.Default.AccessTokenSecret = null;
            Properties.Settings.Default.Save();
            MessageBox.Show("認証設定をリセット");
            // ストリーミングの切断
            userStream.Dispose();
            Authenticate();
        }

        // プロフィールを取得
        private void GetUserProfile()
        {
            var task = Task.Run(() =>
            {
                UserResponse profile = tokens.Account.VerifyCredentials();
                Dispatcher.Invoke(new Action(() =>
                {
                    UserName.Text = $"TLN2\nNow {profile.Name}@{profile.ScreenName}";
                }));
            });
        }

        // ストリーミング
        private void StartUserStreaming()
        {
            var stream = tokens.Streaming.UserAsObservable().Publish();
            // ツイート・リツイートのみ取得
            stream.OfType<StatusMessage>().Subscribe(x => CreateTextBlock(x.Status),
                                                    onError: ex => ErrorUserStreaming());
            userStream = stream.Connect();
        }

        // フィルターストリーミング
        private void StartFilterStreaming(string word)
        {
            var stream = tokens.Streaming.FilterAsObservable(track => word).Publish();
            // ツイート・リツイートのみ取得
            stream.OfType<StatusMessage>().Subscribe(x => CreateTextBlock(x.Status),
                                                    onError: ex => ErrorFilterStreaming());
            filteredStream = stream.Connect();
        }

        // ストリーミングの再接続
        private void ErrorUserStreaming()
        {
            // 再接続
            userStream.Dispose();
            StartUserStreaming();
        }

        // ユーザーストリームの開始と停止
        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            // ストリームを利用しているなら
            if (isStreaming == true)
            {
                var task = Task.Run(() =>
                {
                    MessageBox.Show("ユーザーストリーミングを停止しました");
                });
                // 切断
                userStream.Dispose();
                isStreaming = false;
                StreamMenu.Header = "ユーザーストリーミングを開始";
            }
            else
            {
                var task = Task.Run(() =>
                {
                    MessageBox.Show("ユーザーストリーミングを開始します");
                });
                // 接続
                StartUserStreaming();
                isStreaming = true;
                StreamMenu.Header = "ユーザーストリーミングを停止";
            }
        }

        // フィルターストリーミングの再接続
        private void ErrorFilterStreaming()
        {
            // 再接続
            filteredStream.Dispose();
            StartFilterStreaming(word);
        }

        // フィルターストリーミングの開始と停止
        private async void SerachButtonClick(object sender, RoutedEventArgs e)
        {
            // 利用中なら
            if (isSearching == true)
            {
                var task = Task.Run(() =>
                {
                    MessageBox.Show("検索を停止しました");
                });
                // 切断
                filteredStream.Dispose();
                isSearching = false;
                SearchMenu.Header = "指定ワード検索";
            }
            else
            {
                var task = Task.Run(() =>
                {
                    word = Interaction.InputBox("検索するワードを入力", "検索ワード入力", "", -1, -1);
                });
                // 入力を待つ
                await task;
                // 空欄でないなら
                if (!string.IsNullOrEmpty(word))
                {
                    task = Task.Run(() =>
                    {
                        MessageBox.Show($"\"{word}\" で検索を開始します");
                    });
                    // 接続
                    StartFilterStreaming(word);
                    isSearching = true;
                    SearchMenu.Header = $"\"{word}\" で検索中";
                }
            }
        }

        // テキストブロックの生成
        private void CreateTextBlock(Status status)
        {
            // 日本圏のみ
            if (status.User.Language == "ja")
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    // ツイートからURLを抽出
                    var r = new Regex(@"s?https?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+", RegexOptions.IgnoreCase);
                    Match m = r.Match(status.Text);
                    string linkUrl = m.Value;
                    // ツイートからURL,改行を削除
                    status.Text = Regex.Replace(status.Text, @"s?https?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+", "").Replace("\r", "").Replace("\n", "");
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
                    // Jumpモードが有効なら
                    if (Properties.Settings.Default.IsJumpMode == true)
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

        // ツイートのアニメーション
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

        // 終了ボタン
        private void QuitButtonClick(object sender, RoutedEventArgs e)
        {
            // ストリーミングの切断
            userStream.Dispose();
            filteredStream.Dispose();
            Close();
        }

        // 閉じる前
        private void WindowClosed(object sender, EventArgs e)
        {
            // ストリーミングの切断
            userStream.Dispose();
            filteredStream.Dispose();
        }

        // クリックでツイートのページを開くかどうか
        private void JumpModeMenuClick(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.IsJumpMode == true)
            {
                Properties.Settings.Default.IsJumpMode = false;
                var task = Task.Run(() =>
                {
                    MessageBox.Show("ツイートを開く を無効にしました");
                });
                JumpModeMenu.Header = "ツイートを開く を有効にする";
            }
            else
            {
                Properties.Settings.Default.IsJumpMode = true;
                var task = Task.Run(() =>
                {
                    MessageBox.Show("ツイートを開く を有効にしました");
                });
                JumpModeMenu.Header = "ツイートを開く を無効にする";
            }
            Properties.Settings.Default.Save();
        }
    }
}
