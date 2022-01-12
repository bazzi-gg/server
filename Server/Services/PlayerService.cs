using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Server.Services
{

    public interface IPlayerService
    {
        bool HasRacingMasterEmblem(string nickname);
    }
    public class PlayerService : IPlayerService
    {

        public PlayerService(ILoggerFactory logger)
        {
            _hasRacingMasterEmblemCache = new MemoryCache(new MemoryCacheOptions()
            {
                ExpirationScanFrequency = TimeSpan.FromMinutes(30)
            },logger);
        }

        private DateTimeOffset _cacheExpiration => DateTimeOffset.Now.AddMinutes(20);
        private readonly MemoryCache _hasRacingMasterEmblemCache;
        public bool HasRacingMasterEmblem(string nickname)
        {
            if (_hasRacingMasterEmblemCache.TryGetValue(nickname, out bool value))
            {
                return value;
            }
            HtmlWeb web = new();
            var htmlDoc = web.Load("http://kart.nexon.com/Garage/Emblem?strRiderID=" + nickname + "&ced=1");
            if (htmlDoc.Text.StartsWith("<script>alert('차고 정보가 없습니다.") ||
                htmlDoc.Text.StartsWith("<script>alert('라이더 정보가 없습니다.1');") ||
                htmlDoc.Text.StartsWith("<script>alert('존재하지 않는 유저입니다.')"))
            {
                _hasRacingMasterEmblemCache.Set(nickname, false, _cacheExpiration);
                return false;
            }
            var pages = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"CntSec\"]/div[3]/span");
            //해당 차고 엠블럼에서 페이지들을 찾는다.
            var number = Regex.Matches(pages.InnerText, "[0-9]");
            //정규식으로 한 자리 숫자만 추출
            string lastPageStr = number[^1].Value;
            //맨 마지막 숫자 추출(이것이 해당 차고 엠블럼에서 마지막 페이지다.)
            int lastPage = Convert.ToInt32(lastPageStr);
            for (int i = 1; i <= lastPage; i++) //마지막 페이지가 2이상인 경우만 반복
            {
                if (EmblemExist(web.Load("http://kart.nexon.com/Garage/Emblem?strRiderID=" + nickname + "&ced=1&page=" + i))) //마엠블이 있는 경우
                {
                    _hasRacingMasterEmblemCache.Set(nickname, true, _cacheExpiration);
                    return true;
                }
            }
            _hasRacingMasterEmblemCache.Set(nickname, false, _cacheExpiration);
            return false;
            //해당 차고 엠블럼 페이지에 마엠블이 있는지 확인하고 그결과값을 리턴한다.
            static bool EmblemExist(HtmlDocument htmlDoc)
            {
                var nodes = htmlDoc.DocumentNode.SelectNodes("//*[@id=\"CntEmLSec\"]/ul[2]/li");
                foreach (var node in nodes)
                {
                    if (node.SelectSingleNode("span[2]").InnerText == "레이싱 마스터 엠블럼")
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
