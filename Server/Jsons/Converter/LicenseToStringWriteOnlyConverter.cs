
using Kartrider.Api.Endpoints.MatchEndpoint.Models;


using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server.Jsons.Converter
{
    /// <summary>
    /// Response 데이터에서 Kartrider.API.Model.License를 문자열로 변환하는 컨버터 클래스
    /// </summary>
    public class LicenseToStringWriteOnlyConverter : JsonConverter<License>
    {
        public override License Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, License value, JsonSerializerOptions options)
        {
            string str = value.ToString();
            if (value == License.Beginner)
            {
                str = "초보";
            }
            else if (value == License.Newbie)
            {
                str = "뉴비";
            }
            else if (value == License.None)
            {
                str = "없음";
            }
            else if (value == License.Unknown)
            {
                str = "알 수 없음";
            }
            writer.WriteStringValue(str);
        }
    }
}
