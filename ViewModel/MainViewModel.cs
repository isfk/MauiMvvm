using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Android;
using System.Threading;

namespace MauiMvvm.ViewModel
{
    [ObservableObject]
    partial class MainViewModel
    {
        public static string savePath;

        public MainViewModel()
        {
            Songs = new ObservableCollection<SongTpl>();
            SongName = string.Empty;
            OutLog = string.Empty;
            CanDownload = true;

            if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            {
                savePath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Music/";
            }
            else
            {
                savePath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) + "/";
            }

            MusicPath = "下载目录: " + savePath;
        }

        [ObservableProperty]
        ObservableCollection<SongTpl> songs;

        [ObservableProperty]
        private string songName;

        [ObservableProperty]
        private string musicPath;

        [ObservableProperty]
        private bool canDownload;

        [ObservableProperty]
        private string outLog;

        [RelayCommand]
        async void Search()
        {
            Debug.WriteLine("search btn clicked");
            if (string.IsNullOrWhiteSpace(SongName))
            {
                return;
            }

            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (status == PermissionStatus.Denied)
            {
                OutLog = "没有权限, 请给我媒体权限";
                var toast = Toast.Make("没有权限, 请给我媒体权限", ToastDuration.Long, 14);
                await toast.Show();
                status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                if (status == PermissionStatus.Granted)
                {
                    OutLog = string.Empty;
                }
            }
            OutLog = "查找中...";

            var url = "https://service-l39ky64n-1255944436.bj.apigw.tencentcs.com/release/search/?type=music&offset=0&limit=20&platform=C&keyword=";

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
            OutLog = string.Empty;
        }

        [RelayCommand]
        async Task Download(string mid)
        {
            if (string.IsNullOrWhiteSpace(mid))
            {
                return;
            }
            CanDownload = false;

            try
            {
                if (DeviceInfo.Current.Platform != DevicePlatform.Android)
                {
                    if (!System.IO.Directory.Exists(savePath))
                    {
                        System.IO.Directory.CreateDirectory(savePath);
                    }
                    OutLog = savePath + " 目录创建成功\r\n" + OutLog;
                }
            }
            catch (Exception ex)
            {
                OutLog = ex.ToString() + "\r\n" + OutLog;
            }

            Debug.WriteLine(mid);
            Debug.WriteLine(savePath);
            //OutLog = "mid：" + mid + "\r\n" + OutLog;

            var url = $"https://service-l39ky64n-1255944436.bj.apigw.tencentcs.com/release/music/?type=music&mid={mid}";
            Debug.WriteLine(url);
            var client = new HttpClient();
            var musicResp = await client.GetStringAsync(url);
            var music = JsonConvert.DeserializeObject<Music>(musicResp);

            Debug.WriteLine($"{music.name}");
            OutLog = "歌曲名称：" + music.name + "\r\n" + OutLog;

            if (DeviceInfo.Current.Platform != DevicePlatform.Android)
            {
                var lrcPath = savePath + music.name + ".lrc";
                File.WriteAllBytes(lrcPath, System.Text.Encoding.UTF8.GetBytes(music.lrc));
                Debug.WriteLine("download done: lrc");
                OutLog = "download done: lrc\r\n" + OutLog;

                Debug.WriteLine($"{music.img}");
                if (music.img.Length > 0)
                {
                    try
                    {
                        var imgPath = savePath + music.name + ".jpg";
                        Debug.WriteLine(imgPath);
                        using HttpResponseMessage imgResp = await client.GetAsync(music.img);
                        using var imgFS = File.Open(imgPath, FileMode.Create);
                        using var imgMS = imgResp.Content.ReadAsStream();
                        await imgMS.CopyToAsync(imgFS);
                        Debug.WriteLine("download done: img");
                        OutLog = "download done: img\r\n" + OutLog;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("download field: img" + ex.ToString());
                        OutLog = "download field: img：" + ex.ToString() + "\r\n" + OutLog;
                    }
                }
            }

            Debug.WriteLine($"{music.src}");
            if (music.src.Length > 0)
            {
                try
                {
                    var srcPath = savePath + music.artist[0] + "-" + music.name + ".mp3";
                    Debug.WriteLine(srcPath);
                    using HttpResponseMessage srcResp = await client.GetAsync(music.src);
                    using var srcFS = File.Open(srcPath, FileMode.Create);
                    using var srcMS = srcResp.Content.ReadAsStream();
                    await srcMS.CopyToAsync(srcFS);
                    Debug.WriteLine("download done: src");
                    OutLog = "下载完成: " + music.name + "\r\n" + OutLog;

                    //1048576

                    var fileInfo = new System.IO.FileInfo(srcPath);

                    if (((int)fileInfo.Length) <= 1048576)
                    {
                        //文件过小
                    }

                    //震动提醒
                    Vibration.Default.Vibrate();

                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    var toast = Toast.Make("下载完成: " + music.name, ToastDuration.Short, 18);
                    await toast.Show(cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("download field: src " + ex.ToString());
                    OutLog = "下载失败: " + music.name + "\r\n" + OutLog;
                }
            }
            CanDownload = true;

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

    class Music
    {
        public string lrc = "";
        public string src = "";
        public string name = "";
        public string img = "";
        public List<string> artist = null;
        public AlbumStruct album = new AlbumStruct();
    }

    struct AlbumStruct
    {
        public string name { get; set; }
    }
}
