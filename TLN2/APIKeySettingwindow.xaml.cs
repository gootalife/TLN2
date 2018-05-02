using CoreTweet;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace TLN2
{
    /// <summary>
    /// APIKeySettingwindow.xaml の相互作用ロジック
    /// </summary>
    public partial class APIKeySettingwindow : Window
    {
        private MainWindow main;
        private SettingWindow settingWindow;

        public APIKeySettingwindow(MainWindow main, SettingWindow settingWindow)
        {
            InitializeComponent();
            this.main = main;
            this.settingWindow = settingWindow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ConsumerKeyTextBox.Text = Properties.Settings.Default.ConsumerKey;
            ConsumerSecretTextBox.Text = Properties.Settings.Default.ConsumerSecret;
        }

        private void IssueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 認証用のURL
                main.session = OAuth.Authorize(ConsumerKeyTextBox.Text, ConsumerSecretTextBox.Text);
                Uri url = main.session.AuthorizeUri;
                // ブラウザを起動
                System.Diagnostics.Process.Start(url.ToString());
            }
            catch
            {
                MessageBox.Show(this, "APIKeyの入力に誤りがあります。", "入力値エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                // PINコード取得
                string PINCode = "";
                PINCode = Interaction.InputBox("PINコードを入力", "認証設定", "", -1, -1);
                // トークンを取得して保存
                main.tokens = OAuth.GetTokens(main.session, PINCode);
                Properties.Settings.Default.ConsumerKey = ConsumerKeyTextBox.Text;
                Properties.Settings.Default.ConsumerSecret = ConsumerSecretTextBox.Text;
                Properties.Settings.Default.AccessToken = main.tokens.AccessToken;
                Properties.Settings.Default.AccessTokenSecret = main.tokens.AccessTokenSecret;
                Properties.Settings.Default.Save();
                MessageBox.Show(this, "認証設定を保存");
                main.isAuthenticated = true;
                settingWindow.GetUserProfileAsync();

            }
            catch
            {
                MessageBox.Show(this, "PINコードの入力に誤りがあります。", "ログイン失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
