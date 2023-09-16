using Gremlin.Net.Driver;
using Gremlin.Net.Structure;
using GremlinNetSample;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GremlinArnIngestion
{
    class ARNEventProcessor
	{
        public static DiffEvent ProcessArnEvent(GremlinClient gremlinClient, string arnEventFilePath) {
            // Read ARN event
            var arnEvent = ReadArnEvent(arnEventFilePath);

            // Get list of resources from ARN event
            var resources = GetModifiedResources(arnEvent);
            var resourceIds = "";
            foreach (var resource in resources)
            {
                resourceIds += (resource.ResourceId);
            }
            Console.WriteLine($"Resource count: {resources.Count}; resourceIds: {resourceIds}");
            Debug.Assert(resources.Count == 1);
            // TODO: If resource count can be greater than 1, we need to process all resources and return a list of DiffEvents

            var modifiedResource = resources[0];
            ARMResource modifiedArmResource = JObject.FromObject(modifiedResource.ArmResource).ToObject<ARMResource>();
            string vertexType = GraphUtils.GetVertexLabel(modifiedArmResource.Type);
            Console.WriteLine($"Vertex type: {vertexType}");

            // Convert ARNEvent resource into data model matching that in graph
            ARNUtils.GetResourceData(modifiedResource);

            // Fetch vertex details from Gremlin
            var vertexDetails = GraphUtils.GetVertexDetails(gremlinClient, (string)resources[0].ArmResource["id"]);
            if (vertexDetails == null)
            {
                // Resource was created. Change eventType accordingly.
            }
            else
            {
                Console.WriteLine($"Vertex: {vertexDetails}");
            }

            // Compare against ARN event
            var diffUtils = new DiffUtils();
            DiffEvent diffEvent = diffUtils.CompareResourceWithVertex(modifiedResource, vertexDetails);

            Console.WriteLine($"Diff event: \n{JsonConvert.SerializeObject(diffEvent)}");

            // Form change according to format

            //var addQuery = FormGremlinAddQuery(resources[0]);
            diffEvent.Id = modifiedResource.ResourceId;
            diffEvent.Timestamp = arnEvent.EventTime;
            diffEvent.Type = vertexType;
            if (arnEvent.EventType.EndsWith("/delete"))
            {
                diffEvent.Operation = "Delete";
            }
            else if (arnEvent.EventType.EndsWith("/write"))
            {
                diffEvent.Operation = (vertexDetails == null) ? "Add" : "Update";
            }

            return diffEvent;
        }

        //static string FormGremlinAddQuery(Resource resource)
        //{
        //    string gremlinQuery = $"g.addV()";
        //    foreach (var keyValuePair in resource.ArmResource)
        //    {
        //        if (keyValuePair.Key != "properties")
        //        {
        //            gremlinQuery += $".property('{keyValuePair.Key}', \"{JsonConvert.SerializeObject(keyValuePair.Value)}\")";
        //        } 
        //        else
        //        {
        //            var propertiesDictionary = JObject.FromObject(keyValuePair.Value).ToObject<Dictionary<string, object>>();
        //            foreach (var propertiesKVP in propertiesDictionary)
        //            {
        //                gremlinQuery += $".property('{propertiesKVP.Key}', \"{JsonConvert.SerializeObject(propertiesKVP.Value)}\")";
        //            }
        //        }
        //    }
        //    Console.WriteLine($"Formed Gremlin query: {gremlinQuery}");
        //    return gremlinQuery;
        //}

        static ARNEvent ReadArnEvent(string filePath)
        {
            // Read file
            var fileContent = File.ReadAllText(filePath);
            List<ARNEvent> arnEvents = JsonConvert.DeserializeObject<List<ARNEvent>>(fileContent);
            Debug.Assert(arnEvents.Count == 1);
            var arnEvent = arnEvents.First();
            Console.WriteLine(arnEvent);

            return arnEvent;
        }

        static List<Resource> GetModifiedResources(ARNEvent arnEvent) {
            List<Resource> resources = new List<Resource>();
            foreach (var resource in arnEvent.Data.Resources) {
                resources.Add(resource);
                //foreach (KeyValuePair<string, Object> keyValuePair in resource.ArmResource) {
                //    Console.WriteLine($"Key: {keyValuePair.Key}");
                //}
            }
            return resources;
        }
    }
}
