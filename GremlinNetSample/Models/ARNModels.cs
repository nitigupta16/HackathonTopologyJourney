using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ARNEvent 
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("subject")]
    public string Subject { get; set; }

    [JsonProperty("data")]
    public ARNData Data { get; set; }

    [JsonProperty("eventType")]
    public string EventType { get; set; }

    [JsonProperty("eventTime")]
    public DateTime EventTime { get; set; }
}

public class ARNData
{
    [JsonProperty("resources")]
    public List<Resource> Resources { get; set; }
}

public class Resource
{
    [JsonProperty("resourceId")]
    public string ResourceId { get; set; }

    [JsonProperty("armResource")]
    public Dictionary<string, Object> ArmResource { get; set; }

    public ARMResource ArmResourceData { 
        get 
        {
            return JObject.FromObject(this.ArmResource).ToObject<ARMResource>();
        }
        set { } 
    }
}

public class ARMResource
{
    [JsonProperty("properties")]
    public Dictionary<string, Object> Properties { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("etag")]
    public string Etag { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("location")]
    public string Location { get; set; }
}

public enum ResourceType
{
    VirtualNetwork,
    VirtualSubnetwork,
    VirtualMachine,
    NetworkInterface,
    StorageAccount,
}
