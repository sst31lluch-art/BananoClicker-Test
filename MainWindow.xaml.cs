using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BananaApp
{
    public partial class MainWindow : Window
    {
        GameState G;
        DispatcherTimer _gameTimer, _uiTimer;
        int _page = 0;
        string _tab = "inventory";
        const int PerPage = 15;
        readonly Random _rnd = new();

        static string FormatNum(double d)
        {
            if (d >= 1e9) return (d / 1e9).ToString("0.##") + "B";
            if (d >= 1e6) return (d / 1e6).ToString("0.##") + "M";
            if (d >= 1e3) return (d / 1e3).ToString("0.##") + "K";
            return Math.Floor(d).ToString();
        }

        public MainWindow()
        {
            InitializeComponent();
            G = GameState.Load();
            SetBanana();
            UpdateStats();
            UpdateBonusButton();

            _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _gameTimer.Tick += (s, e) =>
            {
                if (G.CPS > 0) G.AddBananas(G.CPS / 10);
                UpdateStats();
                UpdateBonusButton();
            };
            _gameTimer.Start();

            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _uiTimer.Tick += (s, e) => { if (ShopPage.Visibility == Visibility.Visible) BuildShop(); };
            _uiTimer.Start();

            Closing += (s, e) => G.Save();
        }

        // Обновление всей статистики напрямую
        void UpdateStats()
        {
            BananasText.Text = FormatNum(G.Bananas);
            ShopBananasText.Text = FormatNum(G.Bananas);
            ClickText.Text = FormatNum(G.ClickPower);
            CpsText.Text = G.CPS.ToString("0.#");
        }

        void UpdateBonusButton()
        {
            if (G.BonusActive)
            {
                int left = (int)Math.Ceiling((G.BonusUntil - DateTime.Now).TotalSeconds);
                BonusBtn.Content = $"🔥 ×2  {left}с";
                BonusBtn.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
            else
            {
                BonusBtn.Content = "⏳ Бонус ×2";
                BonusBtn.Background = new SolidColorBrush(Color.FromRgb(232, 132, 44));
            }
        }

        void SetBanana() => BananaBtn.Content = BananaVisual.Create(G.Equipped, 300);

        void Banana_Click(object sender, MouseButtonEventArgs e)
        {
            double gain = G.ClickPower;
            G.AddBananas(gain);
            G.RegisterClick();
            UpdateStats();

            var p = e.GetPosition(FloatLayer);
            SpawnFloat("+" + FormatNum(gain), p.X, p.Y);

            var st = new ScaleTransform(1, 1);
            BananaBtn.RenderTransform = st;
            BananaBtn.RenderTransformOrigin = new Point(0.5, 0.5);
            var anim = new DoubleAnimationUsingKeyFrames
            {
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new EasingDoubleKeyFrame(0.85, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60))),
                    new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180)))
                }
            };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        void SpawnFloat(string text, double x, double y)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 30,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 222, 3)),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, BlurRadius = 6, ShadowDepth = 1 }
            };
            Canvas.SetLeft(tb, x + _rnd.Next(-30, 30));
            Canvas.SetTop(tb, y - 20);
            FloatLayer.Children.Add(tb);

            var move = new DoubleAnimation(y - 110, TimeSpan.FromMilliseconds(900));
            var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(900));
            move.Completed += (s, e) => FloatLayer.Children.Remove(tb);
            tb.BeginAnimation(Canvas.TopProperty, move);
            tb.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        void Bonus_Click(object sender, RoutedEventArgs e)
        {
            if (G.BonusActive) return;
            G.BonusUntil = DateTime.Now.AddSeconds(30);
            UpdateStats();
            UpdateBonusButton();
        }

        // ===== Навигация =====
        void Shop_Click(object sender, RoutedEventArgs e) => OpenShop("inventory");
        void Home_Click(object sender, RoutedEventArgs e)
        {
            ShopPage.Visibility = Visibility.Collapsed;
            GamePage.Visibility = Visibility.Visible;
            UpdateStats();
        }

        void OpenShop(string tab)
        {
            _tab = tab;
            _page = 0;
            GamePage.Visibility = Visibility.Collapsed;
            ShopPage.Visibility = Visibility.Visible;
            BuildShop();
        }

        void TabInventory_Click(object s, RoutedEventArgs e) => OpenShop("inventory");
        void TabExchange_Click(object s, RoutedEventArgs e) => OpenShop("exchange");
        void TabStack_Click(object s, RoutedEventArgs e) => OpenShop("stack");

        void Prev_Click(object s, RoutedEventArgs e) { if (_page > 0) { _page--; BuildShop(); } }
        void Next_Click(object s, RoutedEventArgs e) { _page++; BuildShop(); }

        void BuildShop()
        {
            PagerPanel.Visibility = _tab == "inventory" ? Visibility.Visible : Visibility.Collapsed;
            switch (_tab)
            {
                case "inventory": BuildInventory(); break;
                case "exchange": BuildExchange(); break;
                case "stack": BuildStack(); break;
            }
        }

        void BuildInventory()
        {
            var grid = new UniformGrid { Rows = 3, Columns = 5 };
            int total = Math.Max(1, (int)Math.Ceiling(G.Skins.Count / (double)PerPage));
            if (_page >= total) _page = total - 1;
            PageLabel.Text = $"Page {_page + 1} of {total}";
            PrevBtn.IsEnabled = _page > 0;
            NextBtn.IsEnabled = _page < total - 1;

            int start = _page * PerPage;
            int end = Math.Min(start + PerPage, G.Skins.Count);

            for (int i = start; i < end; i++)
            {
                var skin = G.Skins[i];
                bool equipped = skin.Id == G.EquippedSkinId;
                var cell = new Border
                {
                    Margin = new Thickness(6),
                    Padding = new Thickness(8),
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(Color.FromRgb(123, 75, 34)),
                    BorderBrush = new SolidColorBrush(equipped ? Color.FromRgb(255, 222, 3) : Color.FromRgb(62, 39, 35)),
                    BorderThickness = new Thickness(equipped ? 3 : 1),
                    Opacity = skin.IsOwned ? 1 : 0.75
                };
                var sp = new StackPanel();
                var vb = BananaVisual.Create(skin, 70);
                vb.HorizontalAlignment = HorizontalAlignment.Center;
                sp.Children.Add(vb);
                sp.Children.Add(new TextBlock
                {
                    Text = skin.Name,
                    Foreground = Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 4)
                });

                var btn = new Button
                {
                    Content = equipped ? "✓ Надето" : skin.IsOwned ? "Надеть" : "🍌 " + FormatNum(skin.Price),
                    IsEnabled = !equipped && (skin.IsOwned || G.Bananas >= skin.Price),
                    Padding = new Thickness(8, 4, 8, 4),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Background = new SolidColorBrush(Color.FromRgb(232, 132, 44)),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Template = (ControlTemplate)Resources["EmptyBtn"]
                };
                btn.Click += (s, e) =>
                {
                    if (skin.IsOwned) G.Equip(skin);
                    else if (G.BuySkin(skin)) SetBanana();
                    UpdateStats();
                    BuildShop();
                };
                sp.Children.Add(btn);
                cell.Child = sp;
                grid.Children.Add(cell);
            }
            ShopContent.Content = grid;
        }

        void BuildExchange()
        {
            var panel = new StackPanel { MaxWidth = 600 };
            foreach (var u in G.Upgrades)
            {
                bool can = G.Bananas >= u.CurrentCost;
                var card = new Border
                {
                    Margin = new Thickness(0, 0, 0, 12),
                    Padding = new Thickness(16),
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(Color.FromRgb(123, 75, 34)),
                    BorderBrush = new SolidColorBrush(can ? Color.FromRgb(255, 222, 3) : Color.FromRgb(62, 39, 35)),
                    BorderThickness = new Thickness(2)
                };
                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var info = new StackPanel();
                info.Children.Add(new TextBlock
                {
                    Text = $"{u.Name}   (ур. {u.Level})",
                    Foreground = Brushes.White,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold
                });
                info.Children.Add(new TextBlock
                {
                    Text = u.Description,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 224, 178)),
                    FontSize = 14
                });
                Grid.SetColumn(info, 0);
                g.Children.Add(info);

                var buy = new Button
                {
                    Content = "🍌 " + FormatNum(u.CurrentCost),
                    IsEnabled = can,
                    Padding = new Thickness(14, 8, 14, 8),
                    Background = new SolidColorBrush(can ? Color.FromRgb(232, 132, 44) : Color.FromRgb(90, 60, 40)),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 15,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center,
                    Template = (ControlTemplate)Resources["EmptyBtn"]
                };
                buy.Click += (s, e) =>
                {
                    if (G.BuyUpgrade(u))
                    {
                        UpdateStats();
                        BuildShop();
                    }
                };
                Grid.SetColumn(buy, 1);
                g.Children.Add(buy);
                card.Child = g;
                panel.Children.Add(card);
            }
            ShopContent.Content = panel;
        }

        void BuildStack()
        {
            var sp = new StackPanel { MaxWidth = 500 };
            void Stat(string label, string value)
            {
                var b = new Border
                {
                    Margin = new Thickness(0, 0, 0, 12),
                    Padding = new Thickness(20),
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(Color.FromRgb(123, 75, 34))
                };
                var row = new StackPanel { Orientation = Orientation.Horizontal };
                row.Children.Add(new TextBlock { Text = label, Foreground = Brushes.White, FontSize = 20, Width = 220 });
                row.Children.Add(new TextBlock { Text = value, Foreground = new SolidColorBrush(Color.FromRgb(255, 222, 3)), FontSize = 20, FontWeight = FontWeights.Bold });
                b.Child = row;
                sp.Children.Add(b);
            }

            Stat("Всего бананов:", FormatNum(G.Bananas));
            Stat("Всего кликов:", FormatNum(G.TotalClicks));
            Stat("За клик:", FormatNum(G.ClickPower));
            Stat("В секунду:", G.CPS.ToString("0.#"));
            Stat("Скинов куплено:", $"{G.Skins.FindAll(s => s.IsOwned).Count} / {G.Skins.Count}");
            ShopContent.Content = sp;
        }
    }
}