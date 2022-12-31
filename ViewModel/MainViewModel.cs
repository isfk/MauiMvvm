using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MauiMvvm.ViewModel
{
    [ObservableObject]
    partial class MainViewModel
    {
        public MainViewModel()
        {
            Songs = new ObservableCollection<SongTpl>();
            SongName = string.Empty;
        }

        [ObservableProperty]
        ObservableCollection<SongTpl> songs;

        [ObservableProperty]
        private string songName;

        [RelayCommand]
        async void Search()
        {
            var url = "https://service-l39ky64n-1255944436.bj.apigw.tencentcs.com/release/search/?type=music&offset=0&limit=20&platform=C&keyword=";
            Debug.WriteLine("search btn clicked");
            if (string.IsNullOrWhiteSpace(SongName)) 
            {
                return;
            }

            var client = new HttpClient();
            var resp = await client.GetStringAsync($"{url}{SongName}");
            Debug.WriteLine($"{resp}");
            SongName = string.Empty;
            Songs.Clear();

            var list = JsonConvert.DeserializeObject<List<SongResp>>(resp);
            foreach (var son in list)
            {
                Debug.WriteLine($"{son.Name} - {son.Album.name}");
                var song = new SongTpl()
                {
                    Mid = son.Mid,
                    Name = son.Name,
                    Artist = son.Artist[0],
                    Album = son.Album.name,
                };
                Songs.Add(song);
            }
        }

        [RelayCommand]
        static async Task Download(string mid)
        {
            if (string.IsNullOrWhiteSpace(mid))
            {
                return;
            }

            Debug.WriteLine(mid);
            var url = $"https://service-l39ky64n-1255944436.bj.apigw.tencentcs.com/release/music/?type=music&mid={mid}";
            Debug.WriteLine(url);
            var client = new HttpClient();
            var musicResp= await client.GetStringAsync(url);
            var music = JsonConvert.DeserializeObject<Music>(musicResp);

            Debug.WriteLine($"{music.name} {music.src} {music.img}");

            var lrcPath = @"D:\Music\" + music.name + ".lrc";
            File.WriteAllBytes(lrcPath, System.Text.Encoding.UTF8.GetBytes(music.lrc));
            Debug.WriteLine("download done: lrc");

            if (music.src.Length > 0)
            {
                var srcPath = @"D:\Music\" + music.name + ".mp3";
                using HttpResponseMessage srcResp = await client.GetAsync(music.src);
                using var srcFS = File.Open(srcPath, FileMode.Create);
                using var srcMS = srcResp.Content.ReadAsStream();
                await srcMS.CopyToAsync(srcFS);
                Debug.WriteLine("download done: src");
            }

            if (music.img.Length > 0)
            {
                var imgPath = @"D:\Music\" + music.name + ".jpg";
                using HttpResponseMessage imgResp = await client.GetAsync(music.img);
                using var imgFS = File.Open(imgPath, FileMode.Create);
                using var imgMS = imgResp.Content.ReadAsStream();
                await imgMS.CopyToAsync(imgFS);
                Debug.WriteLine("download done: img");
            }
        }
    }
    struct SongResp
    {
        public string Name { get; set; }
        public string Mid { get; set; }
        public List<string> Artist { get; set; }
        public AlbumStruct Album { get; set; }
    }
    struct SongTpl
    {
        public string Name { get; set; }
        public string Mid { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
    }

    struct Music
    {
        public string lrc;
        public string src;
        public string name;
        public string img;
        public List<string> artist;
        public AlbumStruct album;
    }

    struct AlbumStruct
    {
        public string name { get; set; }
    }
}
