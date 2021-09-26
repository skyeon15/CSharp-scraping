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
        static void Main()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("==========\n1 : 코로나 현황\n" +
                "2 : 네이버 웹툰 순위\n" +
                "3 : 학교 급식\n" +
                "4 : 지상파 편성표\n" +
                "==========\n번호입력 : ");

            string input = Console.ReadLine();
            Console.WriteLine("==========");
            Console.ForegroundColor = ConsoleColor.White;
            if (input == "1")
            {
                Console.WriteLine(COVID());
            }
            else if (input == "2")
            {
                Console.WriteLine(Webtoon());
            }
            else if (input == "3")
            {
                Console.Write("학교이름 : ");
                Console.WriteLine(Eat(Regex.Replace(Console.ReadLine(), @"[^a-zA-Z0-9가-힣_]", "", RegexOptions.Singleline)));
            }
            else if (input == "4")
            {
                Console.WriteLine(TV());
            }
            else
            {
                Console.WriteLine($"{input}은 존재하지 않습니다.");
            }
            Main();
        }
        static string COVID()
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

            //백신접종자 스크래핑을 위해 User-Agent 설정(안 하면 API에서 거부함)
            wc.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.3; WOW64; Trident/7.0)");
            Document doc = NSoupClient.Parse(wc.DownloadString("https://nip.kdca.go.kr/irgd/cov19stats.do"));

            //3자리 콤마, 데이터 선택
            var fir_day = string.Format("{0:#,###}", Convert.ToInt32(doc.Select("firstCnt").Eq(0).Text));
            var fir_sum = string.Format("{0:#,###}", Convert.ToInt32(doc.Select("firstCnt").Eq(2).Text));
            var sec_day = string.Format("{0:#,###}", Convert.ToInt32(doc.Select("secondCnt").Eq(0).Text));
            var sec_sum = string.Format("{0:#,###}", Convert.ToInt32(doc.Select("secondCnt").Eq(2).Text));

            return $"실시간 국내 코로나 현황\n오늘 실시간 : {today}명" +
                $"\n\n0시 기준\n확진환자 : {current[0]}{current[1]}\n치료중 : {hos}{hos2}\n격리해제 : {recovered[0]}{recovered[1]}" +
                $"\n사망자 : {die[0]}{die[1]}" +
                $"\n\n백신 접종 현황\n1차 접종 : {fir_sum}(+{fir_day})\n2차 접종 : {sec_sum}(+{sec_day})";
        }
        static string Webtoon()
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
            return $"{day} 웹툰 순위" +
                $"\n1. [{title[0]} - {author[0]}](https://comic.naver.com{url[0]})" +
                $"\n2. [{title[1]} - {author[1]}](https://comic.naver.com{url[1]})" +
                $"\n3. [{title[2]} - {author[2]}](https://comic.naver.com{url[2]})" +
                $"\n4. [{title[3]} - {author[3]}](https://comic.naver.com{url[3]})" +
                $"\n5. [{title[4]} - {author[4]}](https://comic.naver.com{url[4]})";
        }

        static string Eat(string school_str)
        {
            //학교 번호, 교육청 코드 API 호출
            Document doc = NSoupClient.Parse(new Uri($"https://open.neis.go.kr/hub/schoolInfo?KEY=fe74198d943c4019b9f1a01de4feaae7&SCHUL_NM={school_str}"), 5000);
            string edu = doc.Select("ATPT_OFCDC_SC_CODE").Text;
            string school = doc.Select("SD_SCHUL_CODE").Text;

            //급식 API 호출
            string date = DateTime.Now.ToString("yyyyMMdd");
            doc = NSoupClient.Parse(new Uri($"https://open.neis.go.kr/hub/mealServiceDietInfo?KEY=fe74198d943c4019b9f1a01de4feaae7&ATPT_OFCDC_SC_CODE={edu}&SD_SCHUL_CODE={school}&MLSV_YMD={date}"), 5000);
            Elements datas = doc.Select("row");

            //등록된 급식 수만큼 호출(조식,중식,석식 등)
            string eat_result = "";
            foreach (Element data in datas)
            {
                eat_result += $"**{data.Select("MMEAL_SC_NM").Text}**\n" +
                    $"{data.Select("DDISH_NM").Text.Replace("<br/>", "\n")}\n\n";
            }
            if (eat_result == "")
            {
                if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                {
                    return "주말에는 급식이 없어용";
                }
                return $"{school_str}에 대한 오늘 급식정보를 찾지 못 했어요.";
            }
            return $"{doc.Select("SCHUL_NM").Eq(0).Text} 급식정보\n" + eat_result;
        }

        static string TV()
        {
            //네이버 검색에서 편성표 스크래핑
            Document doc = NSoupClient.Parse(new Uri("https://m.search.naver.com/search.naver?query=%ED%8E%B8%EC%84%B1%ED%91%9C"), 5000);
            Elements datas = doc.Select("li.program_item.on");

            //리턴 문자열 선언
            string str_return = "";
            //방송사 파싱을 위한 int 선언
            int i = 0;

            foreach (var data in datas)
            {
                //url과 프로그램 이름 선택
                string url = data.Select(".pr_name").Attr("href"), name = data.Select(".pr_name").Text;

                //url 비어있으면 프로그램 이름 검색
                if (url == "") url = "http://search.naver.com/search.naver?query=" + name.Replace(" ", "%20");
                //url 있으면 검색 url
                else url = "http://search.naver.com/search.naver?" + url.Substring(url.IndexOf("query="));

                //리턴 문자열 넣기
                str_return += $"{doc.Select("span.els").Eq(i).Text} [{name}]({url})\n";
                i++;
            }

            return str_return;
        }
    }
}
