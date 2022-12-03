using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ArcFaceComponentNuget;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;

namespace ArcFaceWPF
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ArcFaceComponent arcFaceComponent;
        private double currentProgress;
        private float[,] distances;
        private string folderPath;
        private List<Image<Rgb24>> images;
        private string[] imagesPaths;
        private bool isStartAvailable = true;
        private float[,] similarities;
        private int totalProgress;

        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;

        public MainViewModel()
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            images = new List<Image<Rgb24>>();
        }

        public double CurrentProgress
        {
            get => currentProgress;
            set
            {
                currentProgress = value;
                OnPropertyChanged();
            }
        }

        public float[,] Distances
        {
            get => distances;
            set
            {
                distances = value;
                OnPropertyChanged();
            }
        }

        public string FolderPath
        {
            get => folderPath;
            set
            {
                folderPath = value;
                if (value != null)
                {
                    ImagesPaths = Directory.GetFiles(FolderPath, "*.png");
                }
                OnPropertyChanged();
            }
        }

        public string[] ImagesPaths
        {
            get => imagesPaths;
            set
            {
                imagesPaths = value;
                OnPropertyChanged();
            }
        }

        public bool IsStartAvailable
        {
            get => isStartAvailable;
            set
            {
                isStartAvailable = value;
                OnPropertyChanged();
            }
        }

        public float[,] Similarities
        {
            get => similarities;
            set
            {
                similarities = value;
                OnPropertyChanged();
            }
        }

        public int TotalProgress
        {
            get => totalProgress;
            set
            {
                totalProgress = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Clear()
        {
            FolderPath = null;
            images.Clear();
            ImagesPaths = null;
            Distances = null;
            Similarities = null;
            CurrentProgress = 0;
        }

        private void GetImages()
        {
            images.Clear();
            if (ImagesPaths == null)
            {
                return;
            }

            foreach (string filename in ImagesPaths)
            {
                images.Add(Image.Load<Rgb24>(filename));
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task Start()
        {
            IsStartAvailable = false;
            Distances = null;
            Similarities = null;
            CurrentProgress = 0;

            try
            {
                GetImages();
                Trace.WriteLine("started ttt");
                cancellationTokenSource.TryReset();
                using (arcFaceComponent = new ArcFaceComponent())
                {
                    var distances = new float[images.Count, images.Count];
                    var similarities = new float[images.Count, images.Count];
                    var totalProgress = (double)(images.Count * images.Count);

                    for (int i = 0; i < images.Count; i++)
                    {
                        for (int j = 0; j < images.Count; j++)
                        {
                            var emb1 = await arcFaceComponent.GetEmbeddings(images[i], cancellationToken);
                            var emb2 = await arcFaceComponent.GetEmbeddings(images[j], cancellationToken);

                            var distance = ArcFaceComponent.Distance(emb1, emb2);
                            var similarity = ArcFaceComponent.Similarity(emb1, emb2);

                            distances[i, j] = distance;
                            similarities[i, j] = similarity;

                            CurrentProgress = ((currentProgress + 1) / totalProgress) * 100;
                        }
                    }

                    Distances = distances;
                    Similarities = similarities;
                }
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("Operation was cancelled");
            }
            finally
            {
                IsStartAvailable = true;
            }
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }
    }
}
