using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MicroPanel.Models
{
    public class CpuInfo
    {
        [JsonPropertyName("inner")]
        public double Inner { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("info")]
        public List<string>? Info { get; set; }
    }

    public class RamInfo
    {
        [JsonPropertyName("inner")]
        public string? Inner { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("info")]
        public List<string>? Info { get; set; }
    }

    public class SwapInfo
    {
        [JsonPropertyName("inner")]
        public string? Inner { get; set; }

        [JsonPropertyName("percentage")]
        public object? Percentage { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("info")]
        public List<string>? Info { get; set; }
    }

    public class DiskInfo
    {
        [JsonPropertyName("fs")]
        public string? Fs { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("size")]
        public string? Size { get; set; }

        [JsonPropertyName("used")]
        public string? Used { get; set; }

        [JsonPropertyName("available")]
        public long Available { get; set; }

        [JsonPropertyName("use")]
        public int Use { get; set; }

        [JsonPropertyName("mount")]
        public string? Mount { get; set; }

        [JsonPropertyName("rw")]
        public bool Rw { get; set; }

        [JsonPropertyName("percentage")]
        public double Percentage { get; set; }
    }

    public class NodeInfo
    {
        [JsonPropertyName("inner")]
        public double Inner { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("info")]
        public NodeInfoDetail? Info { get; set; }
    }

    public class NodeInfoDetail
    {
        [JsonPropertyName("rss")]
        public string? Rss { get; set; }

        [JsonPropertyName("heapTotal")]
        public string? HeapTotal { get; set; }

        [JsonPropertyName("heapUsed")]
        public string? HeapUsed { get; set; }

        [JsonPropertyName("occupy")]
        public double Occupy { get; set; }
    }

    public class NetworkInfo
    {
        [JsonPropertyName("rx_bytes")]
        public string? RxBytes { get; set; }

        [JsonPropertyName("tx_bytes")]
        public string? TxBytes { get; set; }

        [JsonPropertyName("iface")]
        public string? Iface { get; set; }
    }

    public class OtherInfoItem
    {
        [JsonPropertyName("first")]
        public string? First { get; set; }

        [JsonPropertyName("tail")]
        public object? Tail { get; set; }
    }

    public class EnvironmentVersion
    {
        [JsonPropertyName("node")]
        public string? Node { get; set; }

        [JsonPropertyName("git")]
        public string? Git { get; set; }

        [JsonPropertyName("platform")]
        public string? Platform { get; set; }
    }

    public class SystemStatusData
    {
        [JsonPropertyName("cpuInfo")]
        public CpuInfo? CpuInfo { get; set; }

        [JsonPropertyName("gpuInfo")]
        public object? GpuInfo { get; set; }

        [JsonPropertyName("swapInfo")]
        public SwapInfo? SwapInfo { get; set; }

        [JsonPropertyName("ramInfo")]
        public RamInfo? RamInfo { get; set; }

        [JsonPropertyName("diskSizeInfo")]
        public List<DiskInfo>? DiskSizeInfo { get; set; }

        [JsonPropertyName("nodeInfo")]
        public NodeInfo? NodeInfo { get; set; }

        [JsonPropertyName("networkInfo")]
        public List<NetworkInfo>? NetworkInfo { get; set; }

        [JsonPropertyName("otherInfo")]
        public List<OtherInfoItem>? OtherInfo { get; set; }
    }
}
