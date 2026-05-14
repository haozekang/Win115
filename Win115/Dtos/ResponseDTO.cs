using Newtonsoft.Json;
using System.ComponentModel;

namespace Win115.Dtos
{
    public record ResponseDTO<T>(int State, int Code, string? Message, int ErrNo, string? Error, T? Data)
    {
        [JsonProperty("state"), DefaultValue(0)]
        public int State { get; } = State;

        [JsonProperty("code"), DefaultValue(0)]
        public int Code { get; } = Code;

        [JsonProperty("message"), DefaultValue("")]
        public string? Message { get; } = Message;

        [JsonProperty("error"), DefaultValue("")]
        public string? Error { get; } = Error;

        [JsonProperty("errno"), DefaultValue(0)]
        public int ErrNo { get; } = ErrNo;

        [JsonProperty("data"), DefaultValue(null)]
        public T? Data { get; } = Data;

    }
}
