using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using Gremlin.Net.Structure;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Intrinsics.X86;

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
            CheckPropertyChanged("pk", vertex?.pk, vertexType);
            CheckPropertyChanged("DiscoveryRegion", vertex?.DiscoveryRegion, armResource.Location);
            CheckPropertyChanged("Region", vertex?.Region, armResource.Location);
            CheckPropertyChanged("Subscription", vertex?.Subscription, resource.ResourceId.Split("/")[2]);

            var propertiesJson = JsonConvert.SerializeObject(armResource.Properties);
            var propertiesNode = JsonNode.Parse(propertiesJson);

            // Check property changes

            if (vertexType == ResourceType.VirtualNetwork.ToString())
            {
                var addressPrefixes = propertiesNode!["addressSpace"]?["addressPrefixes"]?.ToJsonString();
                CheckPropertyChanged("AddressPrefixes", vertex?.AddressPrefixes, addressPrefixes);

                // TODO: Not sure how to infer IsFlowEnabled based on ARN event
                var enabledFlowLogCategories = propertiesNode!["flowLogConfiguration"]?["enabledFlowLogCategories"];
                bool isFlowEnabled = (enabledFlowLogCategories != null);
                CheckPropertyChanged("IsFlowEnabled", vertex?.IsFlowEnabled, isFlowEnabled);
            }
            else if (vertexType == ResourceType.VirtualSubnetwork.ToString())
            {
                var addressPrefix = (string)propertiesNode!["addressPrefix"];
                CheckPropertyChanged("AddressPrefix", vertex?.AddressPrefix, addressPrefix);
            }
            else if (vertexType == ResourceType.NetworkSecurityGroup.ToString())
            {
                var resourceGuid = (string)propertiesNode!["resourceGuid"];
                CheckPropertyChanged("ResourceGuid", vertex?.ResourceGuid, resourceGuid);
            }
            else if (vertexType == ResourceType.VirtualMachine.ToString())
            {
                int zones = (int)resource.ArmResource["zones"];
                CheckPropertyChanged("Zones", vertex?.Zones, zones);
            }
            else if (vertexType == ResourceType.StorageAccount.ToString())
            {
                string publicNetworkAccess = (string)propertiesNode!["publicNetworkAccess"];
                CheckPropertyChanged("publicNetworkAccess", vertex?.PublicNetworkAccess, publicNetworkAccess);
            }
            else if (vertexType == ResourceType.NetworkInterface.ToString())
            {
                string MACAddress = (string)propertiesNode!["macAddress"];
                CheckPropertyChanged("MACAddress", vertex?.MACAddress, MACAddress);

                JsonArray ipConfigs = propertiesNode?["ipConfigurations"]?.AsArray();
                List<string> privateIPs = ipConfigs.Select(ipConfig => (string)ipConfig?["properties"]?["privateIPAddress"]).ToList();

                string PrivateIPAddresses = JsonConvert.SerializeObject(privateIPs);
                CheckPropertyChanged("PrivateIPAddresses", vertex?.PrivateIPAddresses, PrivateIPAddresses);

                // Public IP Addresses are separate entities; need to have graph nodes for them.
                //string PublicIPAddresses = (string)propertiesNode!["PublicIPAddresses"]?.ToJsonString();
                //CheckPropertyChanged("PublicIPAddresses", vertex?.PublicIPAddresses, PublicIPAddresses);
            }

            // Check edge changes

            if (vertexType == ResourceType.VirtualNetwork.ToString())
            {
                JsonArray subnets = propertiesNode!["subnets"]?.AsArray();
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
                CheckEdgesChanged(EdgeLabel.flows.ToString(), vertex, flowLogIds, true);

                var nsgId = (string)propertiesNode!["networkSecurityGroup"]?["id"]!;
                CheckEdgeChanged(EdgeLabel.nsg.ToString(), vertex, nsgId, false);
            }
            else if (vertexType == ResourceType.VirtualSubnetwork.ToString())
            {
                var nsgId = (string)propertiesNode?["networkSecurityGroup"]!["id"]!;
                CheckEdgeChanged(EdgeLabel.security.ToString(), vertex, nsgId, false);
            }
            else if (vertexType == ResourceType.NetworkSecurityGroup.ToString())
            {
                // Inbound edges to rules
                JsonArray securityRules = propertiesNode?["securityRules"]?.AsArray();
                List<string> ruleIds = securityRules.Select(x => (string)x!["id"]).ToList();
                JsonArray defaultSecurityRules = propertiesNode?["defaultSecurityRules"]?.AsArray();
                ruleIds.AddRange(defaultSecurityRules.Select(x => (string)x!["id"]).ToList());
                CheckEdgesChanged(EdgeLabel.rule.ToString(), vertex, ruleIds, false);
            }
            else if (vertexType == ResourceType.VirtualMachine.ToString())
            {
                JsonArray networkInterfaces = propertiesNode!["networkProfile"]?["networkInterfaces"]?.AsArray();
                List<string> nicIds = networkInterfaces.Select(x => (string)x!["id"]).ToList();
                CheckEdgesChanged(EdgeLabel.nic.ToString(), vertex, nicIds, false);
            }
            else if (vertexType == ResourceType.StorageAccount.ToString())
            {
                // No edge info in SA ARN events
            }
            else if (vertexType == ResourceType.NetworkInterface.ToString())
            {
                var nsgId = (string)propertiesNode!["networkSecurityGroup"]?["id"];
                CheckEdgeChanged(EdgeLabel.security.ToString(), vertex, nsgId, false);

                var vmId = (string)propertiesNode!["virtualMachine"]?["id"];
                CheckEdgeChanged(EdgeLabel.security.ToString(), vertex, vmId, true);
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
                if (arnValueString == "null")
                {
                    // Property was deleted.
                    diffEvent.Properties.Add(new PropertyDiff(propertyName, OperationType.Delete.ToString(), vertexValueString, arnValueString));
                }
                else if (vertexValueString == "null")
                {
                    // Property was added.
                    diffEvent.Properties.Add(new PropertyDiff(propertyName, OperationType.Add.ToString(), vertexValueString, arnValueString));
                }
                else
                {
                    diffEvent.Properties.Add(new PropertyDiff(propertyName, OperationType.Update.ToString(), vertexValueString, arnValueString));
                }

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

            // Filter out null values
            arnEdgeVertexIds.RemoveAll(item => item == null);

            Dictionary<string, GraphEdge> graphEdges = (vertex == null) 
                                                    ? (new Dictionary<string, GraphEdge>()) 
                                                    : outbound ? vertex?.OutboundEdges : vertex?.InboundEdges;
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
