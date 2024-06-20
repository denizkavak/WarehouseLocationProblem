using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace WpfApp2
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string resultText;

        public string ResultText
        {
            get { return resultText; }
            set
            {
                resultText = value;
                OnPropertyChanged();
            }
        }

        public ICommand SolveCommand { get; }

        private int depoSayisi;
        private int musteriSayisi;
        private double[] depoKapasiteleri;
        private double[] depoKurulumMaliyetleri;
        private double[] musteriTalepleri;
        private double[,] musteriDepoMaliyetleri;
        private string dosya_yolu = "";
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            SolveCommand = new RelayCommand(Solve);
        }

        private void Solve(object parameter)
        {
            ReadInputFromFile();
            int[] depoSecimleri = SolveWarehouseLocationProblem();
            var optimalMaliyet = CalculateOptimalMaliyet(depoSecimleri);

            ShowSolution(optimalMaliyet, depoSecimleri);
        }
        private double CalculateOptimalMaliyet(int[] depoSecimleri)
        {
            double optimalMaliyet = 0;

            // Depoların kapasitelerini ve kullanılan depo miktarlarını tutacak diziler
            double[] depoKalanKapasiteler = new double[depoSayisi];
            int[] depoMusteriSayilari = new int[depoSayisi];

            // Depo kapasitelerini başlangıç değerleriyle doldur
            for (int i = 0; i < depoSayisi; i++)
            {
                depoKalanKapasiteler[i] = depoKapasiteleri[i];
                depoMusteriSayilari[i] = 0;
            }

            // Müşteri taleplerini depolara atama işlemi
            for (int i = 0; i < musteriSayisi; i++)
            {
                int depoIndex = depoSecimleri[i];
                double maliyet = musteriDepoMaliyetleri[i, depoIndex] + depoKurulumMaliyetleri[depoIndex];

                // Depo kapasitesini kontrol et
                if (musteriTalepleri[i] <= depoKalanKapasiteler[depoIndex])
                {
                    optimalMaliyet += maliyet;
                    depoKalanKapasiteler[depoIndex] -= musteriTalepleri[i];
                    depoMusteriSayilari[depoIndex]++;
                }
            }

            // Optimal maliyeti düzenle
            for (int i = 0; i < depoSayisi; i++)
            {
                if (depoMusteriSayilari[i] == 0)
                {
                    optimalMaliyet += depoKurulumMaliyetleri[i];
                }
            }

            return optimalMaliyet;
        }

        private int[] SolveWarehouseLocationProblem()
        {
            int[] depoSecimleri = new int[musteriSayisi];

            for (int i = 0; i < musteriSayisi; i++)
            {
                int enUygunDepo = -1;
                double enDusukMaliyet = double.MaxValue;

                for (int j = 0; j < depoSayisi; j++)
                {
                    double maliyet = musteriDepoMaliyetleri[i, j] + depoKurulumMaliyetleri[j];

                    if (musteriTalepleri[i] <= depoKapasiteleri[j] && maliyet < enDusukMaliyet)
                    {
                        enDusukMaliyet = maliyet;
                        enUygunDepo = j;
                    }
                }

                // Müşteriye en uygun depoyu atama yap
                depoSecimleri[i] = enUygunDepo;
            }

            return depoSecimleri;
        }
        private void ReadInputFromFile()
        {
            try
            {
                string[] lines = File.ReadAllLines(dosya_yolu); 
                depoSayisi = int.Parse(lines[0].Split(' ')[0]);
                musteriSayisi = int.Parse(lines[0].Split(' ')[1]);

                depoKapasiteleri = new double[depoSayisi];
                depoKurulumMaliyetleri = new double[depoSayisi];
                musteriTalepleri = new double[musteriSayisi];
                musteriDepoMaliyetleri = new double[musteriSayisi, depoSayisi];

                int lineIndex = 1;

                for (int i = 0; i < depoSayisi; i++)
                {
                    string[] depoInfo = lines[lineIndex].Split(' ');
                    depoKapasiteleri[i] = double.Parse(depoInfo[0]);
                    depoKurulumMaliyetleri[i] = double.Parse(depoInfo[1]);
                    lineIndex++;
                }

                for (int i = 0; i < musteriSayisi; i++)
                {
                    string[] talepInfo = lines[lineIndex].Split(' ');
                    musteriTalepleri[i] = double.Parse(talepInfo[0]);
                    lineIndex++;

                    string[] maliyetInfo = lines[lineIndex].Split(' ');
                    for (int j = 0; j < depoSayisi; j++)
                    {
                        musteriDepoMaliyetleri[i, j] = double.Parse(maliyetInfo[j]);
                    }
                    lineIndex++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Giriş verileri okunurken bir hata oluştu:\n" + ex.Message);
            }
        }

        private bool CanSolve(object parameter)
        {
           
            if (depoSayisi <= 0 || musteriSayisi <= 0)
            {
                return false;
            }

            if (depoKapasiteleri == null || depoKurulumMaliyetleri == null || musteriTalepleri == null || musteriDepoMaliyetleri == null)
            {
                return false;
            }

            if (depoKapasiteleri.Length != depoSayisi || depoKurulumMaliyetleri.Length != depoSayisi)
            {
                return false;
            }

            if (musteriTalepleri.Length != musteriSayisi || musteriDepoMaliyetleri.GetLength(0) != musteriSayisi || musteriDepoMaliyetleri.GetLength(1) != depoSayisi)
            {
                return false;
            }
            
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void ShowSolution(double optimalMaliyet, int[] depoSecimleri)
        {
            ResultText = $"Optimal Maliyet: {optimalMaliyet}\n";
            for (int i = 0; i < depoSecimleri.Length; i++)
            {
                ResultText += $"{depoSecimleri[i]} ";
            }
            for (int i = 0; i < depoSecimleri.Length; i++)
            {
                ResultText += $"{i + 1}: {depoSecimleri[i]} Müşteriye atanan depo no\n";
            }
        }
       
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            dosya_yolu = @"C:\Users\maybu\Desktop\WpfApp2\16.txt";
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            dosya_yolu = @"C:\Users\maybu\Desktop\WpfApp2\200.txt";
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            dosya_yolu = @"C:\Users\maybu\Desktop\WpfApp2\500.txt";
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            execute(parameter);
        }
    }
}
