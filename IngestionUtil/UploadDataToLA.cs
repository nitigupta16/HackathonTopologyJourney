using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Ingestion;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Text.Json;

namespace IngestionUtil
{
    public class ARNChangeData 
    {
        public string resourceId { get; set; }
        public string timestamp { get; set; }
        public string resourceType { get; set; }
        public string operation { get; set; }
        public List<Properties> properties { get; set; }
        public List<Edges> edges { get; set; }
    }

    public class Properties
    {
        public string name { get; set; }
        public string propertyOperation { get; set; }
        public Values values { get; set; }
    }

    public class Values
    {
        public string oldValue { get; set; }
        public string? newValue { get; set; }
    }

    public class Edges
    {
        public string label { get; set; }
        public List<Resources> resources { get; set; }
    }

    public class Resources
    {
        public string oldResourceId { get; set; }
        public string direction { get; set; }
        public string resourceOperation { get; set; }
        public string? newResourceId { get; set; }
    }

    public class propertyNameValue 
    { 
        public string Region { get; set; }
        public string Subscription { get; set; }
        public string AddressPrefixes { get; set; }
        public string AddressPrefix { get; set; }
        public string Zones { get; set; }
        public string Description { get; set; }
        public string DestinationAddressPrefix { get; set; }
        public string DestinationPortRange { get; set; }
        public string Direction { get; set; }
        public bool isFlowEnabled { get; set; }
        public string PrivateIPAddresses { get; set; }
        public string PublicIPAddresses { get; set; }
        public int Priority { get; set; }
        public string publicNetworkAccess { get; set; }
        public string ResourceGuid { get; set; }
        public int Retention { get; set; }
        public string RuleType { get; set; }
        public string SourceAddressPrefix { get; set; }
        public string SourcePortRange { get; set; }
        public string MACAddress { get; set; }
    }

    public class edgeLabelValues
    {
        public string compute { get; set; }
        public string nic { get; set; }
        public string flows { get; set; }
        public string storage { get; set; }
        public string security { get; set; }
        public string rule { get; set; }
        public string subnet { get; set; }

    }

    public class LAIngestionData 
    { 
        public string resourceId { get; set; }
        public string timestamp { get; set; }
        public string resourceType { get; set; }
        public string operation { get; set; }

        public propertyNameValue propertyNameValue { get; set; }

        public edgeLabelValues edgeLabelValues { get; set; }

        public LAIngestionData(string id, string timestamp, string type, string operation, propertyNameValue propertyNameValue, edgeLabelValues edgeLabelValues)
        {
            this.resourceId = id;
            this.timestamp = timestamp;
            this.resourceType = type;
            this.operation = operation;
            this.propertyNameValue = propertyNameValue;
            this.edgeLabelValues = edgeLabelValues;
        }
    }
    internal static class UploadDataToLA
    {
        public static LAIngestionData FormatUploadData(ARNChangeData inputData) {

            Dictionary<string, string> propertyNameValue = new Dictionary<string, string>();
            foreach(Properties property in inputData.properties)
            {
                propertyNameValue.Add(property.name, property?.values?.oldValue);
            }

            Dictionary<string, List<string>> edgeLabelValues = new Dictionary<string, List<string>>();
            foreach (Edges edges in inputData.edges) 
            {
                List<string> edgeLabels = null;
                if (edgeLabelValues.ContainsKey(edges.label))
                {
                    edgeLabelValues.TryGetValue(edges.label, out edgeLabels);
                }
                else 
                { 
                    edgeLabels = new List<string>();
                }
                foreach (Resources resources in edges.resources) {
                    edgeLabels.Add(resources.oldResourceId);
                }
                edgeLabelValues.Add(edges.label, edgeLabels);
            }

            Dictionary<string, string> edgeLabelValuesFinal = new Dictionary<string, string>();
            foreach (var item in edgeLabelValues)
            {
                edgeLabelValuesFinal.Add(item.Key, String.Join(",", item.Value ));
            }

            string json = JsonSerializer.Serialize(propertyNameValue);
            propertyNameValue prop = JsonSerializer.Deserialize<propertyNameValue>(json);
            string json1 = JsonSerializer.Serialize(edgeLabelValuesFinal);
            edgeLabelValues prop1 = JsonSerializer.Deserialize<edgeLabelValues>(json1);
            LAIngestionData lAIngestion = new LAIngestionData(inputData.resourceId, inputData.timestamp, inputData.resourceType, inputData.operation, prop, prop1);
            return lAIngestion;
            //return JsonSerializer.Serialize(lAIngestion);
        }

        public static void IngestDataToLA(Uri endpoint, string ruleId, string streamName,  LAIngestionData lAIngestion)
        {
            Console.WriteLine($"Ingesting data to LA.");

            // Create credential and client
            var credential = new DefaultAzureCredential();
            LogsIngestionClient client = new(endpoint, credential);

            DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            string content = JsonSerializer.Serialize(lAIngestion);
            //string contentzone = "{[{\"resourceId\":\"abcd\",\"timestamp\":\"5/1/2008 8:30:52 AM\",\"resourceType\":\"VirtualNetwork\",\"operation\":\"Add\",\"propertyNameValue\":{\"AddressPrefix\":\"123\",\"Zones\":\"2\"},\"edgeLabelValues\":{\"nic\":\"nic1,nic2\",\"security\":\"nsg1,nsg3\"}}]}";
            //HttpContent ingestionContent = new StringContent(contentzone, encoding: Encoding.UTF8, mediaType: "application/json");
            BinaryData data = BinaryData.FromObjectAsJson(lAIngestion);
            
            /*BinaryData data1 = BinaryData.FromObjectAsJson
                (
                    new[] 
                    {
                        new
                        { 
                            operation = "Add",
                            timestamp = "bfbfbfb",
                            resourceId = "hfhhg",
                            resourceType = "prop1",
                            propertyNameValue = new { 
                                Region = "abcd"
                            },
                            edgeLabelValues = new { 
                                nic = "bdgd"
                            }
                        },
                        new
                        {
                            operation = "Delete",
                            timestamp = "ggfgfh",
                            resourceId = "ygyg",
                            resourceType = "prop1",
                            propertyNameValue = new {
                                Region = "abcd"
                            },
                            edgeLabelValues = new {
                                nic = "bdgd"
                            }
                        }
                    });*/

            //Upload logs
            try
            {
                //RequestContent.Create(contentzone);
                Response response = client.Upload(ruleId, streamName, RequestContent.Create(data));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Upload failed with exception " + ex.Message);
            }
        }
    }
}
