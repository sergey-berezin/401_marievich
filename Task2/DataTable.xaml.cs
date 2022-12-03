using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ArcFaceWPF
{
    /// <summary>
    /// Interaction logic for DataTable.xaml
    /// </summary>
    public partial class DataTable : UserControl
    {
        public static DependencyProperty DistancesProperty = DependencyProperty.Register("Distances", typeof(float[,]), typeof(DataTable), new PropertyMetadata(null, OnDistancesPropertyChanged));
        public static DependencyProperty ImagesPathsProperty = DependencyProperty.Register("ImagesPaths", typeof(string[]), typeof(DataTable), new PropertyMetadata(null, OnImagesPathPropertyChanged));
        public static DependencyProperty SimilaritiesProperty = DependencyProperty.Register("Similarities", typeof(float[,]), typeof(DataTable), new PropertyMetadata(null, OnSimilaritiesPropertyChanged));

        private List<BitmapImage> images = new List<BitmapImage>();

        public DataTable()
        {
            InitializeComponent();
        }

        public float[,] Distances
        {
            get => (float[,])GetValue(DistancesProperty);
            set { SetValue(DistancesProperty, value); }
        }

        public string[] ImagesPaths
        {
            get => (string[])GetValue(ImagesPathsProperty);
            set { SetValue(DistancesProperty, value); }
        }

        public float[,] Similarities
        {
            get => (float[,])GetValue(SimilaritiesProperty);
            set { SetValue(SimilaritiesProperty, value); }
        }

        public void Clear()
        {
            table.RowDefinitions.Clear();
            table.ColumnDefinitions.Clear();
            table.Children.Clear();
            images.Clear();
        }

        public void ClearLabels()
        {
            var itemsToRemove = new List<object>();
            foreach (var item in table.Children)
            {
                if (item is Label)
                {
                    itemsToRemove.Add(item);
                }
            }

            foreach (var itemToRemove in itemsToRemove)
            {
                table.Children.Remove((UIElement)itemToRemove);
            }
        }

        private void AddUnitGrid()
        {
            table.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(1, GridUnitType.Star)
            });

            table.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
        }

        private static void OnDistancesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataTable dataTable)
            {
                if(e.NewValue is float[,] && dataTable.Similarities != null)
                {
                    dataTable.DisplayData();
                }
            }
        }

        private static void OnImagesPathPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is DataTable dataTable && e.NewValue != null)
            {
                dataTable.Clear();
                dataTable.DrawGrid();
            }
        }

        private static void OnSimilaritiesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataTable dataTable)
            {
                if (e.NewValue is float[,] && dataTable.Distances != null)
                {
                    dataTable.DisplayData();
                }
            }
        }

        private void DrawGrid()
        {
            AddUnitGrid();
            foreach (var (path, i) in ImagesPaths.Select((x, i) => (x, i)))
            {
                AddUnitGrid();

                var uri = new Uri(path);
                var bitmap = new BitmapImage(uri);

                PutImageOnGrid(bitmap, 0, i + 1);
                PutImageOnGrid(bitmap, i + 1, 0);
                images.Add(bitmap);
            }
        }

        public void PutImageOnGrid(BitmapImage bitmap, int col, int row)
        {
            var image = new Image();
            image.Source = bitmap;
            Grid.SetColumn(image, col);
            Grid.SetRow(image, row);
            table.Children.Add(image);
        }

        private void PutLabelOnGrid(Label label, int column, int row)
        {
            table.Children.Add(label);
            Grid.SetColumn(label, column);
            Grid.SetRow(label, row);
        }

        private void DisplayData()
        {
            for (int i = 0; i < images.Count; ++i)
            {
                for (int j = 0; j < images.Count; ++j)
                {
                    var label = new Label()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 12
                    };

                    var distance = Distances[i,j];
                    var similarities = Similarities[i,j];

                    label.Content = $"Distance: {distance:0.00}\nSimilarity: {similarities:0.00}";

                    PutLabelOnGrid(label, i + 1, j + 1);
                }
            }
        }

    }
}
