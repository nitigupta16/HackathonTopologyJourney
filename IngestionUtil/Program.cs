using System.Text.Json;


namespace IngestionUtil
{
    class Program
    {
        static void Main(string[] args)
        {
            //DateTime date1 = DateTime.Now;
            string input = "{\"resourceId\":\"abcd\",\"timestamp\":\"5/1/2008 8:30:52 AM\",\"resourceType\":\"VirtualNetwork\",\"operation\":\"Add\",\"properties\":[{\"name\":\"AddressPrefix\",\"propertyOperation\":\"Add\",\"values\":{\"oldValue\":\"123\",\"newValue\":\"234\"}},{\"name\":\"Zones\",\"propertyOperation\":\"Add\",\"values\":{\"oldValue\":\"2\",\"newValue\":\"3\"}}],\"edges\":[{\"label\":\"nic\",\"resources\":[{\"oldResourceId\":\"nic1\",\"direction\":\"from\",\"resourceOperation\":\"Delete\",\"newResourceId\":\"\"},{\"oldResourceId\":\"nic2\",\"direction\":\"from\",\"resourceOperation\":\"Add\",\"newResourceId\":\"nic2\"}]},{\"label\":\"security\",\"resources\":[{\"oldResourceId\":\"nsg1\",\"direction\":\"to\",\"resourceOperation\":\"Add\",\"newResourceId\":\"nsg1\"},{\"oldResourceId\":\"nsg3\",\"direction\":\"to\",\"resourceOperation\":\"Delete\",\"newResourceId\":\"\"}]}]}"; 
            ARNChangeData? aRNChangeData =
            JsonSerializer.Deserialize<ARNChangeData>(input);
            LAIngestionData ingestionContent = UploadDataToLA.FormatUploadData(aRNChangeData);
            UploadDataToLA.IngestDataToLA(new Uri("https://hacktestdce-lcyo.eastus2-1.ingest.monitor.azure.com"), "dcr-49d4b21b4093457ba098ff734bbf6dfd", "Custom-RawDataTopology", ingestionContent);
            GremlinIngestionData gremlinIngestionData = UploadDataToGremlin.FormatUploadData(aRNChangeData);
            UploadDataToGremlin.IngestDataToGremlin(gremlinIngestionData);

            // Exit program
            Console.WriteLine("Done. Press any key to exit...");
            Console.ReadLine();
        }
    }
}
