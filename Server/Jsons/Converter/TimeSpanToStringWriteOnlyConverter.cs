using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server.Jsons.Converter
{
    /// <summary>
    /// Response 데이터에서 TimeSpan을 레코드 문자열로 변환하는 컨버터 클래스
    /// </summary>
    public class TimeSpanToStringWriteOnlyConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            string str;
            if (value == TimeSpan.Zero)
            {
                str = "";
            }
            else
            {
                str = $"{value.Minutes:00}:{value.Seconds:00}:{value.Milliseconds:000}";
            }
            writer.WriteStringValue(str);
        }
    }
}
