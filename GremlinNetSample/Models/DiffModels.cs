using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace GremlinArnIngestion
{
    public class DiffEvent
    {
        public DiffEvent()
        {
            Properties = new List<PropertyDiff>();
            Edges = new List<EdgeDiff>();
        }

        [JsonProperty("id")]

        public string Id { get; set; }

        [JsonProperty("timestamp")]

        public DateTime Timestamp { get; set; }

        [JsonProperty("type")]

        public string Type { get; set; }

        [JsonProperty("operation")]

        public string Operation { get; set; }

        [JsonProperty("properties")]

        public List<PropertyDiff> Properties { get; set; }

        [JsonProperty("edges")]

        public List<EdgeDiff> Edges { get; set; }
    }

    public class PropertyDiff
    {
        public PropertyDiff(string name, string operation, string oldValue, string newValue) {
            Name = name;
            Operation = operation;
            Values = new Dictionary<string, string>{
                {"oldValue", oldValue},
                {"newValue", newValue}
            };
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("operation")]
        public string Operation { get; set; }

        [JsonProperty("values")]
        public Dictionary<string, string> Values { get; set; }

    }

    public class EdgeDiff
    {
        public EdgeDiff(string label, List<EdgeDiffResource> resources)
        {
            Label = label;
            Resources = resources;
        }

        [JsonProperty("label")]

        public string Label { get; set; }

        [JsonProperty("resources")]

        public List<EdgeDiffResource> Resources { get; set; }
    }

    public class EdgeDiffResource
    {
        public EdgeDiffResource(string id, string direction, string operation, string newValue)
        {
            this.id = id;  // Resource ID
            this.direction = direction;
            this.operation = operation;
            this.newValue = newValue;
        }

        public string id { get; set; }
        public string direction { get; set; }
        public string operation { get; set; }
        public string newValue { get; set; }

    }

    public enum OperationType
    {
        Add,
        Update,
        Delete,
    }
}
