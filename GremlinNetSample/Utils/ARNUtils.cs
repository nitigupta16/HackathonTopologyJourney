using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GremlinArnIngestion
{
    class ARNUtils
    {
        // Convert ARNEvent ARMResource to data model object
        public static ResourceVertex GetResourceData(Resource resource)
        {
            ResourceVertex vertex = new ResourceVertex();
            ARMResource armResource = JObject.FromObject(resource.ArmResource).ToObject<ARMResource>();
            var vertexType = GraphUtils.GetVertexLabel(armResource.Type);
            vertex.pk = vertexType;
            vertex.DiscoveryRegion = armResource.Location;
            vertex.Region = armResource.Location;
            vertex.Subscription = resource.ResourceId.Split("/")[2];

            var propertiesJson = JsonConvert.SerializeObject(armResource.Properties);
            var propertiesNode = JsonNode.Parse(propertiesJson);

            if (vertexType == "VirtualNetwork")
            {
                vertex.AddressPrefixes = (string)propertiesNode!["addressSpace"]!["addressPrefixes"]![0];
                Console.WriteLine($"Address prefix: {vertex.AddressPrefixes}");
            }

            return vertex;
        }
    }
}
