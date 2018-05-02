using CoreTweet;
using CoreTweet.Streaming;
using FNF.Utility;
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
        // トークン関連
        public Tokens tokens;
        public OAuth.OAuthSession session;

        // ストリームのリソース
        private IDisposable filterStream;
        private IDisposable userStream;

        // ストリームの状態
        private bool isUserStreaming;
        private bool isFilterStreaming;

        // ログインしているかどうか
        public bool isAuthenticated = false;

        // テキストブロックにクリックイベントがないのはなぜだなぜだなぜだ
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
            // タスクトレイのアイコン設定
            TaskTrayIcon.Icon = Properties.Resources.Icon;
            // 設定画面を開く
            var settingWindow = new SettingWindow(this);
            settingWindow.ShowDialog();
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
            isUserStreaming = true;
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
            if (isUserStreaming == true)
            {
                // 切断
                userStream.Dispose();
                isUserStreaming = false;
            }
        }

        /// <summary>
        /// フィルターストリーミングの再接続
        /// </summary>
        private void ErrorFilterStreaming()
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
                    var m = r.Match(status.Text);
                    var linkUrl = m.Value;
                    // ツイートからURLを除去、改行を半角スペースに置換
                    status.Text = Regex.Replace(status.Text, pattern, "").Replace("\r", "").Replace("\n", " ");
                    // 棒読みちゃんに渡す文字列
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
                        Text = $@"{status.User.Name}@{status.User.ScreenName} : {status.Text} ",
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
                        // クリックイベントもどき なんでクリックイベントがないねん せや！作ったろ！
                        // テキスト上でクリックして離したとき
                        var tweetUrl = $@"http://twitter.com/{status.User.ScreenName}/status/{status.Id}";
                        tweet.MouseLeftButtonDown += (s, e) =>
                        {
                            mouseLeftButtonDown = true;
                        };
                        tweet.MouseLeftButtonUp += (s, e) =>
                        {
                            if (mouseLeftButtonDown == true)
                            {
                                System.Diagnostics.Process.Start(tweetUrl);
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
            var time = random.Next(10000, 13000);
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
        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 閉じる前
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            // ストリーミングの切断
            if (isUserStreaming == true)
            {
                userStream.Dispose();
            }
            if (isFilterStreaming == true)
            {
                filterStream.Dispose();
            }
        }

        /// <summary>
        /// 棒読みちゃんで読み上げ
        /// </summary>
        private void Bouyomi(string text)
        {
            var bouyomiChanClient = new BouyomiChanClient();
            bouyomiChanClient.AddTalkTask(text);
            bouyomiChanClient.Dispose();
        }

        /// <summary>
        /// 設定画面を開く
        /// </summary>
        private void OpenSettingWindow_Click(object sender, EventArgs e)
        {
            var window = new SettingWindow(this);
            window.ShowDialog();
        }
    }
}
