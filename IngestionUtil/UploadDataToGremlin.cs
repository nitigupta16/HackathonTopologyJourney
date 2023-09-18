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
	public class GremlinIngestionData
	{
		public string resourceId { get; set; }
		public string timestamp { get; set; }
		public string resourceType { get; set; }
		public string operation { get; set; }
		public Dictionary<string, string> properties { get; set; }
		public List<Edges> edges { get; set; }

		public GremlinIngestionData(string id, string timestamp, string type, string operation, Dictionary<string, string> properties, List<Edges> edges)
		{
			this.resourceId = id;
			this.timestamp = timestamp;
			this.resourceType = type;
			this.operation = operation;
			this.properties = properties;
			this.edges = edges;
		}

		public static KeyValuePair<string, string> queryToAddVertex(string type, string resourceID, Dictionary<string, string> propertiesToBeAdded)
		{
			string vertexLabel = label;
			string vertexID = resourceID;
			Dictionary<string, string> propertiesList = propertiesToBeAdded;
			StringBuilder queryValue = new StringBuilder($"g.addV('{label}').property('id', '{vertexID}').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', '{label}').property('isDeleted', 'false')");
			string queryKey = "Add Vertex";
			foreach (var property in propertiesToBeAdded)
			{
				queryValue.Append($".property('{property.Key}', '{property.Value}')");
			}
			return new KeyValuePair<string, string>(queryKey, queryValue);
		}

		public static KeyValuePair<string, string> queryToUpdateVertex(string id, Dictionary<string, string> propertiesToBeUpdated)
		{
			string vertexId = id;
			Dictionary<string, string> propertiesList = propertiesToBeUpdated;
			StringBuilder queryValue = new StringBuilder($"g.V('{vertexId}')property('LastModifiedTime', '{DateTime.Now}')");
			string queryKey = $"Update Vertex id = {vertexId}";

			foreach (var property in propertiesToBeUpdated)
			{
				queryValue.Append($".property('{property.Key}', '{property.Value}')");
			}

			return new KeyValuePair<string, string>(queryKey, queryValue);
		}

		public static KeyValuePair<string, string> queryToDeleteVertex(string id)
		{
			string vertexId = id;
			//StringBuilder queryValue = new StringBuilder($"g.V('{vertexId}')property('LastModifiedTime', '{DateTime.Now}')");
			string queryKey = $"Delete Vertex id = {vertexId}";
			string queryValue = $"g.V('{vertexId}')property('LastModifiedTime', '{DateTime.Now}').property('isDeleted', 'false')";

			return new KeyValuePair<string, string>(queryKey, queryValue);
		}

		public static KeyValuePair<string, string> queryToRemoveAllEdgesOfAVertex(string id)
		{
			string vertexId = id;
			string queryKey = $"Delete all nodes to/fro vertex = {vertexId}";
			string queryValue = $"g.V('{vertexId}').bothE().property('label', 'inactive')";

			return new KeyValuePair<string, string>(queryKey, queryValue);
		}

		public static KeyValuePair<string, string> queryToAddEdge(string currentVertexId, string label, string direction, string targetVertex)
		{
			string edgeLabel = label;
			string queryKey = "Add Edge";
			String queryValue = $"g.V('{currentVertexId}').addE('{edgeLabel}').'{direction}'(g.V('{targetVertex}')";

			return new KeyValuePair<string, string>(queryKey, queryValue);

		}

		public static KeyValuePair<string, string> queryToRemoveEdge(string currentVertexID, string label, string direction, string targetVertex)
		{
			string edgeLabel = label;
			string queryKey = "Remove Edge";
			if (direction == "from")
			{
				String queryValue = $"g.V('{currentVertexId}').inE('{edgeLabel}').where(outV().is('{targetVertex}')).property('label', 'inactive')";
			} else
			{
				String queryValue = $"g.V('{currentVertexId}').outE('{edgeLabel}').where(inV().is('{targetVertex}')).property('label', 'inactive')";
			}

			return new KeyValuePair<string, string>(queryKey, queryValue);
		}

	}
	
	
	internal static class UploadDataToGremlin
	{
		public static GremlinIngestionData FormatUploadData(ARNChangeData inputData)                                     
		{

			Dictionary<string, string> properties = new Dictionary<string, string>();
			foreach (Properties property in inputData.properties)
			{
				propertyNameValue.Add(property.name, property.values.newValue);
			}

			GremlinIngestionData gremlinIngestionData = new gremlinIngestionData(inputData.resourceId, inputData.timestamp, inputData.resourceType, inputData.operation, properties, inputData.edges);
			return gremlinIngestionData;
		}

		public static void IngestDataToGremlin (GremlinIngestionData gremlinIngestionData, GremlinClient gremlinClient)
		{
			//query to add/ update/ delete the vertex 
			if (gremlinIngestionData.operation == "Add")
			{
				KeyValuePair<string, string> query = queryToAddVertex(gremlinIngestionData.resourceType, gremlinIngestionData.resourceId, gremlinIngestionData.properties)
			}
			else if (gremlinIngestionData.operation = "Delete")
			{
				KeyValuePair<string, string> query = queryToDeleteVertex(gremlinIngestionData.resourceId)
			}
			else if (gremlinIngestionData.operation = "Update")
			{
				KeyValuePair<string, string> query  = queryToUpdateVertex(gremlinIngestionData.resourceId, gremlinIngestionData.properties)
			}

			var resultSet = SubmitRequest(gremlinClient, query).Result;
			if (resultSet.Count == 0)
			{
				Console.WriteLine($"Couldnt perform {gremlinIngestionData.operation} on vertex with id = {gremlinIngestionData.resourceId}"); 
			}

			//queries to add/ delete the nodes 
			Dictionary<string, string> queriesForUpdatingEdges = new Dictionary<string, string>();

			if (gremlinIngestionData.operation = "Delete")
			{
				KeyValuePair<string, string> query = queryToRemoveAllEdgesOfAVertex(string gremlinIngestionData.resourceId);
				queriesForUpdatingEdges.Add(query);
			}
			else
			{
				foreach (Edges edge in gremlinIngestionData.edges)
				{
					foreach(Resources resource in edge.resources)
					{
						if (resource.resourceOperation == "Delete")
						{
							KeyValuePair<string, string> query = queryToRemoveEdge(gremlinIngestionData.resourceId, gremlinIngestionData.resourceType, resource.direction, resource.oldResourceId)
							queriesForUpdatingEdges.Add(query);
						} 
						else if (resource.resourceOperation == "Add")
						{
							KeyValuePair<string, string> query = queryToAddEdge(gremlinIngestionData.resourceId, gremlinIngestionData.resourceType, resource.direction, resource.oldResourceId)
							queriesForUpdatingEdges.Add(query);
						}
					}
				}
			}
			foreach (KeyValuePair<string, string> query in queriesForUpdatingEdges)
			{
				var resultSet = SubmitRequest(gremlinClient, query).Result;
				if (resultSet.Count == 0)
				{
					Console.WriteLine($"Couldnt perform query {query}");
				}
			}
		}
	}
}
