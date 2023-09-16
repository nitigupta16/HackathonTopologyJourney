using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using Gremlin.Net.Structure;

namespace GremlinArnIngestion
{
    class DiffUtils
    {
        public DiffEvent diffEvent;

        public DiffUtils() => diffEvent = new DiffEvent();

        // Convert ARNEvent Resource to data model object
        public DiffEvent CompareResourceWithVertex(Resource resource, ResourceVertex vertex)
        {
            ARMResource armResource = resource.ArmResourceData;
            var vertexType = GraphUtils.GetVertexLabel(armResource.Type);
            //vertex.pk = vertexType;
            CheckPropertyChanged("pk", vertex.pk, vertexType);
            CheckPropertyChanged("DiscoveryRegion", vertex.DiscoveryRegion, armResource.Location);
            //vertex.DiscoveryRegion = armResource.Location;
            CheckPropertyChanged("Region", vertex.Region, armResource.Location);
            //vertex.Region = armResource.Location;
            CheckPropertyChanged("Subscription", vertex.Subscription, resource.ResourceId.Split("/")[2]);
            //vertex.Subscription = resource.ResourceId.Split("/")[2];

            var propertiesJson = JsonConvert.SerializeObject(armResource.Properties);
            var propertiesNode = JsonNode.Parse(propertiesJson);

            // Check property changes

            if (vertexType == "VirtualNetwork")
            {
                var addressPrefixes = (string)propertiesNode!["addressSpace"]!["addressPrefixes"]!.ToString();
                Console.WriteLine($"Address prefix: {vertex.AddressPrefixes}");
                CheckPropertyChanged("AddressPrefixes", vertex.AddressPrefixes, addressPrefixes);
            }
            else if (vertexType == "VirtualSubnetwork")
            {
                //var addressPrefix = propertiesNode![""]
            }
            else if (vertexType == "VirtualMachine")
            {
                int zones = (int)resource.ArmResource["zones"];
                CheckPropertyChanged("Zones", vertex.Zones, zones);
            }

            // Check edge changes

            if (vertexType == "VirtualNetwork")
            {
                JsonArray subnets = propertiesNode!["subnets"]!.AsArray();
                List<string> subnetIds = new List<string>();
                foreach (JsonNode? subnet in subnets)
                {
                    var subnetId = (string)subnet!["id"]!;
                    subnetIds.Add(subnetId);
                }
                CheckEdgesChanged(EdgeLabel.subnet.ToString(), vertex, subnetIds, false);

                JsonArray flowLogs = propertiesNode!["flowLogs"]!.AsArray();
                List<string> flowLogIds = new List<string>();
                foreach (JsonNode? flowLog in flowLogs)
                {
                    var flowLogId = (string)flowLog!["id"]!;
                    flowLogIds.Add(flowLogId);
                }
                CheckEdgesChanged(EdgeLabel.flowlog.ToString(), vertex, flowLogIds, true);
            }
            else if (vertexType == "VirtualSubnetwork")
            {
                var nsgId = (string)propertiesNode!["networkSecurityGroup"]!["id"]!;
                CheckEdgeChanged(EdgeLabel.nsg.ToString(), vertex, nsgId, false);
            }
            else if (vertexType == "VirtualMachine")
            {

            }

            return diffEvent;
        }

        public bool CheckPropertyChanged(string propertyName, Object vertexValue, Object arnValue)
        {
            var vertexValueString = JsonConvert.SerializeObject(vertexValue);
            var arnValueString = JsonConvert.SerializeObject(arnValue);
            if (vertexValueString != arnValueString)
            {
                Console.WriteLine($"Detected change in property {propertyName}. vertexValue: {vertexValue} , arnValue: {arnValue}");
                diffEvent.Properties.Add(new PropertyDiff(propertyName, OperationType.Update.ToString(), vertexValueString, arnValueString));

                return true;
            }
            return false;
        }

        public bool CheckEdgeChanged(string edgeLabel, ResourceVertex vertex, string arnEdgeVertexId, bool outBound)
        {
            return CheckEdgesChanged(edgeLabel, vertex, new List<string> { arnEdgeVertexId }, outBound);
        }

        public bool CheckEdgesChanged(string edgeLabel, ResourceVertex vertex, List<string> arnEdgeVertexIds, bool outbound)
        {
            var edgeDiffResources = new List<EdgeDiffResource>();

            Dictionary<string, GraphEdge> graphEdges = outbound ? vertex.OutboundEdges : vertex.InboundEdges;
            string directionString = outbound ? "to" : "from";

            foreach (var vertexId in arnEdgeVertexIds)
            {
                if (graphEdges.TryGetValue(vertexId, out var edge))
                {
                    if (edge.label != edgeLabel)
                    {
                        // Delete old edge, add new edge
                        // TODO: This is an unlikely case, so leaving it for later
                    }
                }
                else
                {
                    // Add new edge
                    edgeDiffResources.Add(new EdgeDiffResource(vertexId, directionString, "Add", vertexId));
                }
            }

            // Find deleted edges.
            foreach (var edge in graphEdges.Values)
            {
                if (edge.label == edgeLabel)
                {
                    string edgeVertexId = outbound ? edge.inV : edge.outV;
                    if (!arnEdgeVertexIds.Contains(edgeVertexId))
                    {
                        // Add deleted edge
                        edgeDiffResources.Add(new EdgeDiffResource(edgeVertexId, directionString, "Delete", "null"));
                    }
                }
            }

            if (edgeDiffResources.Count > 0)
            {
                diffEvent.Edges.Add(new EdgeDiff(edgeLabel, edgeDiffResources));
                return true;
            }
            else
            {
                return false;
            }
        }

        public void AddEdgeDiff()
        {

        }
    }
}
