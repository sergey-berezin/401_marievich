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
using ArcFaceWPF.Storage;
using System.Linq;
using ArcFaceWPF.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace ArcFaceWPF
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ArcFaceComponent arcFaceComponent;
        private double currentProgress;
        private float[,] distances;
        private string folderPath;
        private List<ImageEntry> imageEntries;
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
            imageEntries = new List<ImageEntry>();
            ImageEntriesFromDb = new ObservableCollection<ImageEntry>();

            UpdateDataBaseView();
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

        public ObservableCollection<ImageEntry> ImageEntriesFromDb { get; set; }

        public ImageEntry SelectedImageEntry { get; set; }

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
            imageEntries.Clear();
            ImagesPaths = null;
            Distances = null;
            Similarities = null;
            CurrentProgress = 0;
        }

        public void ClearDataBase()
        {
            if (!ImageEntriesFromDb.Any())
                return;

            using (var db = new ImagesContext())
            {
                db.Clear();
                db.SaveChanges();
            }

            ImageEntriesFromDb.Clear();
            SelectedImageEntry = null;
        }

        public void RemoveImageFromDataBase()
        {
            if (!ImageEntriesFromDb.Any() || SelectedImageEntry == null)
            {
                return;
            }

            using (var db = new ImagesContext())
            {
                var deletedImage = db.Images
                    .Where(x => x.Id == SelectedImageEntry.Id)
                    .Include(x => x.Details)
                    .First();

                if (deletedImage == null)
                    return;

                db.Details.Remove(deletedImage.Details);
                db.Images.Remove(deletedImage);
                db.SaveChanges();
            }

            ImageEntriesFromDb.Remove(SelectedImageEntry);
        }

        private void GetImages()
        {
            imageEntries.Clear();
            if (ImagesPaths == null)
            {
                return;
            }

            var imageDetails = ImagesPaths.Select(path => new ImageData
            {
                Data = File.ReadAllBytes(path)
            });

            imageEntries = ImagesPaths.Zip(imageDetails, (path, details) => new ImageEntry
            {
                Path = path,
                Details = details,
                Hash = ImageHelper.Hash(details.Data)
            }).ToList();
        }

        private static bool GetEmbeddingsFromDb(ImageEntry image, out float[]? embedding)
        {
            var hash = ImageHelper.Hash(image.Details.Data);

            using var db = new ImagesContext();
            var imageEntry = db.Images
                .Where(x => x.Hash == hash)
                .Include(x => x.Details)
                .Where(x => Equals(x.Details.Data, image.Details.Data))
                .FirstOrDefault();

            embedding = ImageHelper.ByteToFloat(imageEntry?.Embedding);
            return embedding != null;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static void SaveEmbedding(byte[] embedding, ImageEntry image)
        {
            using var db = new ImagesContext();
            var imageEntryFromDb = db.Images
                .Where(x => x.Id == image.Id)
                .FirstOrDefault();

            if (imageEntryFromDb == null)
            {
                var newDetails = new ImageData
                {
                    Data = image.Details.Data
                };
                var newImage = new ImageEntry
                {
                    Path = image.Path,
                    Embedding = embedding,
                    Details = newDetails,
                    Hash = image.Hash
                };

                db.Images.Add(newImage);
                db.Details.Add(newDetails);
            }
            else if (embedding != null)
            {
                image.Embedding = embedding;
            }

            db.SaveChanges();
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
                    var distances = new float[imageEntries.Count, imageEntries.Count];
                    var similarities = new float[imageEntries.Count, imageEntries.Count];
                    var totalProgress = (double)(imageEntries.Count * imageEntries.Count);

                    for (int i = 0; i < imageEntries.Count; i++)
                    {
                        for (int j = 0; j < imageEntries.Count; j++)
                        {
                            if (!GetEmbeddingsFromDb(imageEntries[i], out float[] emb1))
                            {
                                var img1 = Image.Load<Rgb24>(imageEntries[i].Details.Data);
                                emb1 = await arcFaceComponent.GetEmbeddings(img1, cancellationToken);
                                SaveEmbedding(ImageHelper.FloatToByte(emb1), imageEntries[i]);
                            }

                            if (!GetEmbeddingsFromDb(imageEntries[j], out float[] emb2))
                            {
                                var img2 = Image.Load<Rgb24>(imageEntries[j].Details.Data);
                                emb2 = await arcFaceComponent.GetEmbeddings(img2, cancellationToken);
                                SaveEmbedding(ImageHelper.FloatToByte(emb2), imageEntries[j]);
                            }

                            var distance = ArcFaceComponent.Distance(emb1, emb2);
                            var similarity = ArcFaceComponent.Similarity(emb1, emb2);

                            distances[i, j] = distance;
                            similarities[i, j] = similarity;

                            CurrentProgress = ((currentProgress + 1) / totalProgress) * 100;
                        }
                    }

                    Distances = distances;
                    Similarities = similarities;
                    UpdateDataBaseView();
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

        public void UpdateDataBaseView()
        {
            using var db = new ImagesContext();
            foreach (var image in db.Images)
            {
                if (!ImageEntriesFromDb.Any(img => img.Id == image.Id))
                {
                    ImageEntriesFromDb.Add(image);
                }
            }
        }
    }
}
