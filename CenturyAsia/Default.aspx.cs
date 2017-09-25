using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CenturyAsia
{
    public class Room
    {
        public Room(int id, int 差幾張 = 2)
        {
            Id = id;
            this.差幾張 = 差幾張;
        }

        public int Id { get; set; }

        public int 差幾張 { get; set; }

        public Dictionary<string, List<DateTime>> TimeTable { get; set; } = new Dictionary<string, List<DateTime>>();
    }


    public class Movie
    {
        public string Name { get; set; }

        public string Id { get; set; }

        public Dictionary<int, List<DateTime>> Times { get; set; } = new Dictionary<int, List<DateTime>>();
    }

    /// <summary>
    /// 以廳為主的模型，主要儲存每一廳播放的電影及時刻
    /// </summary>
    public class MovieTimeList
    {
        public List<TimeListModel> TimeList { get; set; }

        public string RoomName_CodeName { get; set; }

        public string Room => new string(this.RoomName_CodeName.Take(3).ToArray())
            .Replace("廳", "").Replace(" ", "");
    }

    /// <summary>
    /// 時刻模型
    /// </summary>
    public class TimeListModel
    {
        public DateTime Date { get; set; }

        public string Time { get; set; }

        public DateTime RealTime
        {
            get
            {
                var t = Time.Split(':');
                int h, m;
                int.TryParse(t[0], out h);
                int.TryParse(t[1], out m);
                return new DateTime(Date.Year, Date.Month, Date.Day, h, m, 0);
            }
        }
    }

    public partial class _Default : Page
    {
        public List<Room> NeedRooms { get; set; }

        public List<DateTime> NeedDates { get; set; }

        public void GetRooms(DateTime date)
        {
            NeedRooms = new List<Room>()
            {
                new Room(4),new Room(7,1),/*new Room(11),*/
                new Room(18,1),new Room(20,1)
            };

            var ids = MemoryCache.Default.Get("ids") as List<Movie> ?? GetMovies();

            var copyMovies = ids.Select(id => new Movie() { Id = id.Id, Name = id.Name }).ToList();

            foreach (var id in copyMovies)
            {
                var now = date;
                var timeList = GetTimeTable(id.Id, now);
                timeList.ForEach(t =>
                {
                    var room = Convert.ToInt32(t.Room);
                    if (!id.Times.Keys.Contains(room))
                    {
                        id.Times.Add(room, new List<DateTime>());
                    }

                    var times = t.TimeList.Select(ti => ti.RealTime);

                    id.Times[room] = id.Times[room].Concat(times).ToList();
                });
            }

            foreach (var room in NeedRooms)
            {
                var movies = copyMovies.Where(dic => dic.Times.Keys.Contains(room.Id));

                foreach (var movie in movies)
                {
                    var t = movie.Times.Where(m => m.Key == room.Id).First();
                    room.TimeTable.Add(movie.Name, t.Value);
                }
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            NeedDates = new List<DateTime>()
            {
                DateTime.Now,
                DateTime.Now.AddDays(1),
                DateTime.Now.AddDays(2),
                DateTime.Now.AddDays(3),
                DateTime.Now.AddDays(4),
                DateTime.Now.AddDays(5),
            };
        }

        public List<Movie> GetMovies()
        {
            var movies = new List<Movie>();

            for (int i = 0; i <= 4; i++)
            {
                var url = $@"http://www.centuryasia.com.tw/ticket_online.aspx?page={i}";
                WebClient client = new WebClient() { Encoding = Encoding.UTF8 };
                var html = client.DownloadString(url);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                var sections = doc.DocumentNode.Descendants("section");
                var m = sections.Where(sec => sec.Attributes["id"] != null)
                    .Select(sec => new Movie()
                    {
                        Id = sec.Attributes["id"].Value,
                        Name = sec.Descendants("div").Where(div => div.Attributes["class"] != null && div.Attributes["class"].Value == "times_title").First().InnerText
                    })
                    .ToList();
                movies = movies.Concat(m).ToList();
            }

            var x = (int)DateTime.Now.DayOfWeek % 7;
            var policy = new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddDays(3) };

            MemoryCache.Default.Add("ids", movies, policy);

            return movies;
        }

        public List<MovieTimeList> GetTimeTable(string id, DateTime date)
        {
            var key = $"{id}_{date.ToShortDateString()}";
            if (MemoryCache.Default.Get(key) != null)
            {
                return (List<MovieTimeList>)MemoryCache.Default.Get(key);
            }

            WebClient client = new WebClient() { Encoding = System.Text.Encoding.UTF8 };
            NameValueCollection nv = new NameValueCollection();
            nv.Add("date", date.ToString("yyyy/MM/dd"));
            nv.Add("ProgramID", id);
            var data = client.UploadValues(@"http://www.centuryasia.com.tw/Ajax/ProgramMovieTime.ashx", nv);
            var json = Encoding.UTF8.GetString(data);
            var timeLists = JsonConvert.DeserializeObject<List<MovieTimeList>>(json);
            //Response.Write(json);
            timeLists.ForEach(t =>
            {
                t.TimeList.ForEach(ti => ti.Date = date);
            });
            var policy = new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddDays(3) };
            MemoryCache.Default.Add(key, timeLists, policy);
            return timeLists;
        }
    }
}