using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace GremlinArnIngestion
{
    public class ResourceVertex
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("DiscoveryRegion")]
        public string DiscoveryRegion { get; set; }

        [JsonProperty("Region")]
        public string Region { get; set; }

        [JsonProperty("Subscription")]
        public string Subscription { get; set; }

        [JsonProperty("CreatedTime")]
        public DateTime CreatedTime { get; set; }

        [JsonProperty("LastModifiedTime")]
        public DateTime LastModifiedTime { get; set; }

        [JsonProperty("pk")]
        public string pk { get; set; }

        public Dictionary<string, GraphEdge> InboundEdges {  get; set; }
        public Dictionary<string, GraphEdge> OutboundEdges {  get; set; }

        // VirtualNetwork

        [JsonProperty("AddressPrefixes")]
        public string? AddressPrefixes { get; set; }

        [JsonProperty("IsFlowEnabled")]
        public bool? IsFlowEnabled { get; set; }


        // Virtual Subnetwork

        [JsonProperty("AddressPrefix")]
        public string? AddressPrefix { get; set; }

        // NetworkSecurityGroup

        [JsonProperty("ResourceGuid")]
        public string? ResourceGuid { get; set; }

        // StorageAccount

        [JsonProperty("publicNetworkAccess")]
        public string? PublicNetworkAccess { get; set; } 

        // VirtualMachine

        public int? Zones { get; set; }

        // NetworkInterface

        public string? MACAddress { get; set; }
        public string? PrivateIPAddresses { get; set; }
        public string? PublicIPAddresses { get; set; }


        public ResourceVertex()
        {
            InboundEdges = new Dictionary<string, GraphEdge>();
            OutboundEdges = new Dictionary<string, GraphEdge>();
        }
    }

    public class VirtualNetwork: ResourceVertex
    {
        [JsonProperty("AddressPrefixes")]
        public string AddressPrefixes { get; set; }

        [JsonProperty("IsFlowEnabled")]
        public bool IsFlowEnabled { get; set; }
    }

    public class VirtualSubnetwork: ResourceVertex
    {
        public string AddressPrefix { get; set; }
    }

    public class GraphEdge
    {
        public string label { get; set; }
        public string inV { get; set; }
        public string outV { get; set; }

    }

    public enum EdgeLabel
    {
        subnet, flows, // VirtualNetwork
        nsg, // Subnet
        flowlog,
    }
}
