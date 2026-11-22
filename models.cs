using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace BananaApp
{
    public class Upgrade
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public double BaseCost { get; set; }
        public double Value { get; set; }
        public bool IsClickUpgrade { get; set; }
        public int Level { get; set; }
        public double CurrentCost => Math.Ceiling(BaseCost * Math.Pow(1.15, Level));
    }

    public class BananaSkin
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double Price { get; set; }
        public string BaseColor { get; set; } = "#FFDE03";
        public string TipColor { get; set; } = "#8B5A2B";
        public string Pattern { get; set; } = "None";
        public bool IsOwned { get; set; }
    }

    public class GameState : INotifyPropertyChanged
    {
        static readonly string SavePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BananaApp", "save.json");

        public double Bananas { get; set; }
        public double TotalClicks { get; set; }
        public double BananaPerClick { get; set; } = 1;
        public double CPS { get; set; }
        public DateTime BonusUntil { get; set; }
        public List<Upgrade> Upgrades { get; set; } = new();
        public List<BananaSkin> Skins { get; set; } = new();
        public string EquippedSkinId { get; set; } = "classic";

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Notify([CallerMemberName] string n = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public BananaSkin Equipped => Skins.Find(s => s.Id == EquippedSkinId) ?? Skins[0];
        public bool BonusActive => DateTime.Now < BonusUntil;
        public double ClickPower => BananaPerClick * (BonusActive ? 2 : 1);

        public static GameState CreateNew()
        {
            var g = new GameState();
            g.Upgrades = new List<Upgrade>
            {
                new() { Id="c1", Name="Крепче жми", Description="+1 за клик", BaseCost=50, Value=1, IsClickUpgrade=true },
                new() { Id="c2", Name="Двойной тап", Description="+5 за клик", BaseCost=500, Value=5, IsClickUpgrade=true },
                new() { Id="c3", Name="Кулак обезьяны", Description="+25 за клик", BaseCost=5000, Value=25, IsClickUpgrade=true },
                new() { Id="a1", Name="Обезьянка", Description="+1 банан/сек", BaseCost=100, Value=1, IsClickUpgrade=false },
                new() { Id="a2", Name="Конвейер", Description="+5 бананов/сек", BaseCost=1200, Value=5, IsClickUpgrade=false },
                new() { Id="a3", Name="Банановая фабрика", Description="+25 бананов/сек", BaseCost=15000, Value=25, IsClickUpgrade=false },
            };
            g.Skins = new List<BananaSkin>
            {
                new() { Id="classic", Name="Классика", Price=0, IsOwned=true },
                new() { Id="choco", Name="Шоколад", Price=200, BaseColor="#6B4226", TipColor="#3E2723", Pattern="Dots" },
                new() { Id="candy", Name="Конфетка", Price=500, BaseColor="#FF69B4", Pattern="Stripes" },
                new() { Id="gold", Name="Золото", Price=2500, BaseColor="#FFD700", TipColor="#B8860B", Pattern="Stars" },
                new() { Id="ghost", Name="Призрак", Price=6000, BaseColor="#E0E0E0", Pattern="Skull" },
                new() { Id="neon", Name="Неон", Price=12000, BaseColor="#00FF88", Pattern="Stars" },
                new() { Id="lava", Name="Лава", Price=25000, BaseColor="#FF4500", TipColor="#8B0000", Pattern="Stripes" },
                new() { Id="ice", Name="Лёд", Price=40000, BaseColor="#87CEEB", Pattern="Dots" },
                new() { Id="rainbow", Name="Радуга", Price=70000, Pattern="Rainbow" },
                new() { Id="space", Name="Космос", Price=120000, BaseColor="#1A1A40", TipColor="#4B0082", Pattern="Stars" },
                new() { Id="toxic", Name="Токсик", Price=200000, BaseColor="#ADFF2F", Pattern="Skull" },
                new() { Id="diamond", Name="Алмаз", Price=500000, BaseColor="#B9F2FF", Pattern="Dots" },
            };
            return g;
        }

        public void AddBananas(double a) { Bananas += a; Notify(nameof(Bananas)); }
        public void RegisterClick() { TotalClicks++; Notify(nameof(TotalClicks)); }

        public bool BuyUpgrade(Upgrade u)
        {
            if (Bananas < u.CurrentCost) return false;
            Bananas -= u.CurrentCost;
            u.Level++;
            if (u.IsClickUpgrade) BananaPerClick += u.Value; else CPS += u.Value;
            Notify(nameof(Bananas));
            Notify(nameof(BananaPerClick));
            Notify(nameof(CPS));
            Notify(nameof(ClickPower));
            return true;
        }

        public bool BuySkin(BananaSkin s)
        {
            if (s.IsOwned || Bananas < s.Price) return false;
            Bananas -= s.Price;
            s.IsOwned = true;
            EquippedSkinId = s.Id;
            Notify(nameof(Bananas));
            Notify(nameof(Equipped));
            return true;
        }

        public void Equip(BananaSkin s)
        {
            if (!s.IsOwned) return;
            EquippedSkinId = s.Id;
            Notify(nameof(Equipped));
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
                File.WriteAllText(SavePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static GameState Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var g = JsonSerializer.Deserialize<GameState>(File.ReadAllText(SavePath));
                    if (g != null && g.Upgrades.Count > 0 && g.Skins.Count > 0) return g;
                }
            }
            catch { }
            return CreateNew();
        }
    }
}