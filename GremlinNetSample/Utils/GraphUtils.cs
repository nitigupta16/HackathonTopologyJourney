using Gremlin.Net.Driver;
using GremlinNetSample;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GremlinArnIngestion
{
    class GraphUtils
    {
        public static ResourceVertex GetVertexDetails(GremlinClient gremlinClient, string vertexId)
        {
            KeyValuePair<string, string> query = new KeyValuePair<string, string>("GetVertex", $"g.V('{vertexId}')");
            ResultSet<dynamic> resultSet = Program.RunGremlinQuery(gremlinClient, query);
            if (resultSet.Count == 0)
            {
                // Did not find any results for vertex; need to add a new vertex.
                // TODO: Set eventType as creation, and proceed.
                return null;
            }
            var result = resultSet.First();
            Console.WriteLine($"Got result: {result}");

            string propertiesJson = JsonConvert.SerializeObject(result["properties"]);
            var properties = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, Object>>>>(propertiesJson);
            Dictionary<string, Object> vertexProperties = new Dictionary<string, object>();
            foreach (var keyValuePair in properties)
            {
                Object propertyValue = keyValuePair.Value[0]["value"];
                Console.WriteLine($"Found value {propertyValue} for property {keyValuePair.Key}");
                vertexProperties.Add(keyValuePair.Key, propertyValue);
            }

            string vertexPropertiesJson = JsonConvert.SerializeObject(vertexProperties);

            var vertexInfo = JsonConvert.DeserializeObject<ResourceVertex>(vertexPropertiesJson);
            vertexInfo.Id = vertexId;

            GetVertexEdges(gremlinClient, vertexInfo);

            return vertexInfo;
        }

        public static void GetVertexEdges(GremlinClient gremlinClient, ResourceVertex vertex)
        {
            KeyValuePair<string, string> query = new KeyValuePair<string, string>("GetVertex", $"g.V('{vertex.Id}').bothE()");
            ResultSet<dynamic> resultSet = Program.RunGremlinQuery(gremlinClient, query);
            if (resultSet.Count == 0)
            {
                // Did not find any edges for vertex
                return;
            }
            foreach (var result in resultSet)
            {
                Console.WriteLine($"Got result: {result}");

                string edgeJson = JsonConvert.SerializeObject(result);
                GraphEdge edge = JsonConvert.DeserializeObject<GraphEdge>(edgeJson);
                if (edge.outV == vertex.Id)
                {
                    vertex.OutboundEdges.Add(edge.inV, edge);
                    Console.WriteLine($"Added outbound edge: {edgeJson} to vertex: {vertex.Id}");
                }
                else
                {
                    vertex.InboundEdges.Add(edge.outV, edge);
                    Console.WriteLine($"Added inbound edge: {edgeJson} to vertex: {vertex.Id}");
                }
            }
            Console.WriteLine($"Vertex: {vertex.Id} has {vertex.InboundEdges.Count + vertex.OutboundEdges.Count} edges now");
        }

        // Convert ARNEvent ARMResource to data model object
        public static Type GetVertexDataModelType(string vertexType)
        {
            if (vertexType == "VirtualNetwork")
            {
                return Type.GetType("VirtualNetwork");
            }
            return Type.GetType("ResourceVertex");
        }

        public static string GetVertexLabel(string resourceType)
        {
            if (resourceType.EndsWith("virtualNetworks"))
            {
                return "VirtualNetwork";
            }
            else if (resourceType.EndsWith("virtualMachines"))
            {
                return "VirtualMachine";
            }
            else if (resourceType.EndsWith("subnets"))
            {
                return "VirtualSubnetwork";
            }
            else if (resourceType.EndsWith("networkInterfaces"))
            {
                return "NetworkInterface";
            }
            else if (resourceType.EndsWith("storageAccounts"))
            {
                return "StorageAccounts";
            }
            // TODO: Add for all resource types
            return "Unknown";
        }
    }
}
