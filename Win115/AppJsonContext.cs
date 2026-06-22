using System.Collections.Generic;
using System.Text.Json.Serialization;
using Win115.Dtos;

namespace Win115
{
    [JsonSerializable(typeof(AliyunOssCallbackDTO))]
    [JsonSerializable(typeof(OpenUfileFilesDTO))]
    [JsonSerializable(typeof(OpenUfileSearchDTO))]
    [JsonSerializable(typeof(ResponseDTO))]
    [JsonSerializable(typeof(ResponseDTO<OpenRefreshTokenDTO>))]
    [JsonSerializable(typeof(ResponseDTO<OpenAuthDeviceCodeDTO>))]
    [JsonSerializable(typeof(ResponseDTO<GetQeCodeStatusDTO>))]
    [JsonSerializable(typeof(ResponseDTO<OpenDeviceCodeToTokenDTO>))]
    [JsonSerializable(typeof(ProResponseDTO))]
    [JsonSerializable(typeof(ProResponseDTO<OpenFolderAddDTO>))]
    [JsonSerializable(typeof(ProResponseDTO<OpenFolderGetInfoDTO?>))]
    [JsonSerializable(typeof(ProResponseDTO<OpenOfflineGetQuotaInfo>))]
    [JsonSerializable(typeof(ProResponseDTO<OpenOfflineGetTaskListDTO>))]
    [JsonSerializable(typeof(ProResponseDTO<OpenRbListDTO>))]
    [JsonSerializable(typeof(ProResponseDTO<Dictionary<string, OpenUfileDownurlDTO?>?>))]
    [JsonSerializable(typeof(ProResponseDTO<OpenUploadGetTokenDTO>))]
    [JsonSerializable(typeof(ProResponseDTO<OpenUfileUpdateDTO>))]
    [JsonSerializable(typeof(ProResponseDTO<OpenUploadInitNoCallbackDTO>))]
    [JsonSerializable(typeof(ProResponseDTO<OpenUploadResumeDTO>))]
    [JsonSerializable(typeof(ProResponseDTO<OpenUserInfoDTO>))]
    [JsonSerializable(typeof(ProResponseDTO<OpenVideoHistoryDTO?>))]
    [JsonSerializable(typeof(ProResponseDTO<OpenVideoHistoryDTO[]?>))]
    [JsonSerializable(typeof(ProResponseDTO<OpenVideoPlayDTO>))]
    [JsonSerializable(typeof(ProResponseDTO<string[]?>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(OpenRefreshTokenDTO))]
    [JsonSerializable(typeof(OpenAuthDeviceCodeDTO))]
    [JsonSerializable(typeof(GetQeCodeStatusDTO))]
    [JsonSerializable(typeof(OpenDeviceCodeToTokenDTO))]
    [JsonSerializable(typeof(OpenFolderAddDTO))]
    [JsonSerializable(typeof(OpenFolderGetInfoDTO))]
    [JsonSerializable(typeof(OpenOfflineGetQuotaInfo))]
    [JsonSerializable(typeof(OpenOfflineGetTaskListDTO))]
    [JsonSerializable(typeof(OpenRbListDTO))]
    [JsonSerializable(typeof(OpenRbListDataItemDTO))]
    [JsonSerializable(typeof(OpenUfileDownurlDTO))]
    [JsonSerializable(typeof(OpenUploadGetTokenDTO))]
    [JsonSerializable(typeof(OpenUfileUpdateDTO))]
    [JsonSerializable(typeof(OpenUploadInitNoCallbackDTO))]
    [JsonSerializable(typeof(OpenUploadResumeDTO))]
    [JsonSerializable(typeof(OpenUserInfoDTO))]
    [JsonSerializable(typeof(OpenVideoHistoryDTO))]
    [JsonSerializable(typeof(OpenVideoHistoryDTO[]))]
    [JsonSerializable(typeof(OpenVideoPlayDTO))]
    public partial class AppJsonContext : JsonSerializerContext
    {
    }
}
