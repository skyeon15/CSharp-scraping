using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace COVID19_status
{
    class Program
    {
        static void Main(string[] args)
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
            var hos2 = string.Format("{0:+#,###;-#,###}", Convert.ToInt32(current[1]) - Convert.ToInt32(recovered[1]) - Convert.ToInt32(die[1]));
            //실시간 확진자
            var today = string.Format("{0:#,###}", Convert.ToInt32(Regex.Replace(json["statsLive"]["today"].ToString(), @"[\s\[\]]", "")));

            //3자리 콤마, 증감에 양음수 표시
            current[0] = string.Format("{0:#,###}", Convert.ToInt32(current[0]));
            current[1] = string.Format("{0:+#,###;-#,###}", Convert.ToInt32(current[1]));
            die[0] = string.Format("{0:#,###}", Convert.ToInt32(die[0]));
            die[1] = string.Format("{0:+#,###;-#,###}", Convert.ToInt32(die[1]));
            recovered[0] = string.Format("{0:#,###}", Convert.ToInt32(recovered[0]));
            recovered[1] = string.Format("{0:+#,###;-#,###}", Convert.ToInt32(recovered[1]));

            Console.WriteLine($"실시간 국내 코로나 현황\n오늘 실시간 : {today}명" +
                $"\n\n0시 기준\n확진환자 : {current[0]}({current[1]})\n치료중 : {hos}({hos2})\n격리해제 : {recovered[0]}({recovered[1]})" +
                $"\n사망자 : {die[0]}({die[1]})");
        }
    }
}
