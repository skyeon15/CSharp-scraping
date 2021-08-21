using Newtonsoft.Json.Linq;
using NSoup;
using NSoup.Nodes;
using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using NSoup.Select;

namespace CSharps_craping
{
    class Program
    {
        //static void Main(string[] args)
        static void Main()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("==========\n1 : 코로나 현황\n2 : 네이버 웹툰 순위\n==========\n번호입력 : ");
            string input = Console.ReadLine();
            Console.WriteLine("==========");
            Console.ForegroundColor = ConsoleColor.White;
            if (input == "1") COVID();
            else if (input == "2") webtoon();
            else
            {
                Console.WriteLine($"{input}은 존재하지 않습니다.");
            }
            Main();
        }
        static void COVID()
        {
            WebClient wc = new WebClient() { Encoding = Encoding.UTF8 };
            JObject json = JObject.Parse(wc.DownloadString("https://apiv2.corona-live.com/domestic-init.json"));

            //누적 확진자
            string data = Regex.Replace(json["stats"]["cases"].ToString(), @"[\s\[\]]", "");
            string[] current = data.Split(",");
            //누적 사망자
            data = Regex.Replace(json["stats"]["deaths"].ToString(), @"[\s\[\]]", "");
            string[] die = data.Split(",");
            //누적 격리해제
            data = Regex.Replace(json["stats"]["recovered"].ToString(), @"[\s\[\]]", "");
            string[] recovered = data.Split(",");
            //현재 치료중 계산
            var hos = string.Format("{0:#,###}", Convert.ToInt32(current[0]) - Convert.ToInt32(recovered[0]) - Convert.ToInt32(die[0]));
            var hos2 = string.Format("({0:+#,###;-#,###})", Convert.ToInt32(current[1]) - Convert.ToInt32(recovered[1]) - Convert.ToInt32(die[1]));
            //실시간 확진자
            var today = string.Format("{0:#,###}", Convert.ToInt32(Regex.Replace(json["statsLive"]["today"].ToString(), @"[\s\[\]]", "")));

            //3자리 콤마, 증감에 양음수 표시
            current[0] = string.Format("{0:#,###}", Convert.ToInt32(current[0]));
            current[1] = string.Format("({0:+#,###;-#,###})", Convert.ToInt32(current[1]));
            die[0] = string.Format("{0:#,###}", Convert.ToInt32(die[0]));
            die[1] = string.Format("({0:+#,###;-#,###})", Convert.ToInt32(die[1]));
            recovered[0] = string.Format("{0:#,###}", Convert.ToInt32(recovered[0]));
            recovered[1] = string.Format("({0:+#,###;-#,###})", Convert.ToInt32(recovered[1]));

            Console.WriteLine($"실시간 국내 코로나 현황\n오늘 실시간 : {today}명" +
                $"\n\n0시 기준\n확진환자 : {current[0]}{current[1]}\n치료중 : {hos}{hos2}\n격리해제 : {recovered[0]}{recovered[1]}" +
                $"\n사망자 : {die[0]}{die[1]}");

            //백신접종자 스크래핑을 위해 User-Agent 설정(안 하면 API에서 거부함)
            wc.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.3; WOW64; Trident/7.0)");
            Document doc = NSoupClient.Parse(wc.DownloadString("https://nip.kdca.go.kr/irgd/cov19stats.do"));

            //3자리 콤마, 데이터 선택
            var fir_day = string.Format("{0:#,###}", Convert.ToInt32(doc.Select("firstCnt").Eq(0).Text));
            var fir_sum = string.Format("{0:#,###}", Convert.ToInt32(doc.Select("firstCnt").Eq(2).Text));
            var sec_day = string.Format("{0:#,###}", Convert.ToInt32(doc.Select("secondCnt").Eq(0).Text));
            var sec_sum = string.Format("{0:#,###}", Convert.ToInt32(doc.Select("secondCnt").Eq(2).Text));

            Console.WriteLine($"\n백신 접종 현황\n1차 접종 : {fir_sum}(+{fir_day})\n2차 접종 : {sec_sum}(+{sec_day})");
        }
        static void webtoon()
        {
            //네이버 웹툰에서 오늘 해당 요일 웹툰 스크래핑
            Document doc = NSoupClient.Parse(new Uri("https://m.comic.naver.com/webtoon/weekday"), 5000);
            Elements datas = doc.Select("div.section_list_toon ul.list_toon li");

            //요일 확인
            string day = doc.Select("div.area_sublnb.lnb_weekday h3.blind").Text;
            //제목, 작가, URL 각 5개 선언
            string[] title = new string[5], author = new string[5], url = new string[5];
            //제목, 작가, URL 5번 불러오기
            for (int i = 0; i < 5; i++)
            {
                title[i] = datas.Eq(i).Select("div.info strong.title").Text;
                author[i] = datas.Eq(i).Select("div.info span.author").Text;
                url[i] = datas.Eq(i).Select("a").Attr("href");
            }

            //출력
            Console.WriteLine($"{day} 웹툰 순위" +
                $"\n1. [{title[0]} - {author[0]}](https://comic.naver.com{url[0]})" +
                $"\n2. [{title[1]} - {author[1]}](https://comic.naver.com{url[1]})" +
                $"\n3. [{title[2]} - {author[2]}](https://comic.naver.com{url[2]})" +
                $"\n4. [{title[3]} - {author[3]}](https://comic.naver.com{url[3]})" +
                $"\n5. [{title[4]} - {author[4]}](https://comic.naver.com{url[4]})");
        }
    }
}
