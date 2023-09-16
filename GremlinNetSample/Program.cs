using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Exceptions;
using Gremlin.Net.Structure.IO.GraphSON;
using Newtonsoft.Json;

namespace GremlinNetSample
{
    /// <summary>
    /// Sample program that shows how to get started with the Graph (Gremlin) APIs for Azure Cosmos DB using the open-source connector Gremlin.Net
    /// </summary>
    class Program
    {
        // Azure Cosmos DB Configuration variables
        // Replace the values in these variables to your own.
        // <configureConnectivity>
        private static string Host => Environment.GetEnvironmentVariable("Host") ?? throw new ArgumentException("Missing env var: Host");
        private static string PrimaryKey => Environment.GetEnvironmentVariable("PrimaryKey") ?? throw new ArgumentException("Missing env var: PrimaryKey");
        private static string Database = "Topologydb";
        private static string Container = "Topology";

        private static bool EnableSSL
        {
            get
            {
                if (Environment.GetEnvironmentVariable("EnableSSL") == null)
                {
                    return true;
                }

                if (!bool.TryParse(Environment.GetEnvironmentVariable("EnableSSL"), out bool value))
                {
                    throw new ArgumentException("Invalid env var: EnableSSL is not a boolean");
                }

                return value;
            }
        }

        private static int Port
        {
            get
            {
                if (Environment.GetEnvironmentVariable("Port") == null)
                {
                    return 443;
                }

                if (!int.TryParse(Environment.GetEnvironmentVariable("Port"), out int port))
                {
                    throw new ArgumentException("Invalid env var: Port is not an integer");
                }

                return port;
            }
        }

        // </configureConnectivity>

        // Gremlin queries that will be executed.
        // <defineQueries>

        private static Dictionary<string, string> gremlinQueries = new Dictionary<string, string>
{
    { "Cleanup",        "g.V().drop()" },
    { "AddVertex 1",    $"g.addV('VirtualNetwork').property('id', 'hack-test-vnet-1').property('DiscoveryRegion', 'eastus2').property('Region', 'westus').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('AddressPrefixes', '10.242.121.0-24').property('IsFlowEnabled', true).property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'VirtualNetwork')" },
    { "AddVertex 2",    $"g.addV('VirtualNetwork').property('id', 'hack-test-vnet-2').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('AddressPrefixes', '10.232.224.0-20').property('IsFlowEnabled', true).property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'VirtualNetwork')" },
    { "AddVertex 3",    $"g.addV('VirtualNetwork').property('id', 'hack-test-vnet-3').property('DiscoveryRegion', 'eastus2').property('Region', 'westus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('AddressPrefixes', '10.233.224.0-20').property('IsFlowEnabled', true).property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'VirtualNetwork')" },

    { "AddVertex 4",    $"g.addV('VirtualSubnetwork').property('id', 'hack-test-vnet-2-hack-test-subnet-2').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('AddressPrefix', '10.232.224.0-23').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'VirtualSubnetwork')" },
    { "AddVertex 5",    $"g.addV('VirtualSubnetwork').property('id', 'hack-test-vnet-2-default').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('AddressPrefix', '10.232.231.0-25').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'VirtualSubnetwork')" },
    { "AddVertex 6",    $"g.addV('VirtualSubnetwork').property('id', 'hack-test-vnet-1-default').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('AddressPrefix', '10.242.121.0-25').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'VirtualSubnetwork')" },
    { "AddVertex 7",    $"g.addV('VirtualSubnetwork').property('id', 'hack-test-vnet-3-hack-test-subnet-3').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('AddressPrefix', '10.233.224.0-25').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'VirtualSubnetwork')" },

    { "AddVertex 8",    $"g.addV('VirtualMachine').property('id', 'hack-test-vm-1').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Zones', 1).property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'VirtualMachine')" },
    { "AddVertex 9",    $"g.addV('VirtualMachine').property('id', 'hack-test-vm-2').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Zones', 1).property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'VirtualMachine')" },
    { "AddVertex 10",    $"g.addV('VirtualMachine').property('id', 'hack-test-vm-3').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Zones', 1).property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'VirtualMachine')" },
    { "AddVertex 11",    $"g.addV('VirtualMachine').property('id', 'hack-test-vm-4').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Zones', 1).property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'VirtualMachine')" },

    { "AddVertex 12",    $"g.addV('NetworkInterface').property('id', 'hack-test-vm-1395_z1').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('MACAddress', '60-45-BD-7B-F0-0B').property('PrivateIPAddresses', '10.242.121.4').property('PublicIPAddresses', '20.246.108.205').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkInterface')" },
    { "AddVertex 13",    $"g.addV('NetworkInterface').property('id', 'hack-test-vm-2427_z1').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('MACAddress', '60-45-BD-85-84-C1').property('PrivateIPAddresses', '10.232.231.4').property('PublicIPAddresses', '20.246.109.63').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkInterface')" },
    { "AddVertex 14",    $"g.addV('NetworkInterface').property('id', 'hack-test-vm-3653_z1').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('MACAddress', '60-45-BD-BE-EC-CD').property('PrivateIPAddresses', '10.232.224.4').property('PublicIPAddresses', '20.246.110.26').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkInterface')" },
    { "AddVertex 15",    $"g.addV('NetworkInterface').property('id', 'hack-test-vm-4238_z1').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('MACAddress', '60-45-BD-85-8F-BE').property('NSG', 'hack-test-vm-1-nsg').property('PrivateIPAddresses', '10.233.224.4').property('PublicIPAddresses', '20.246.110.109').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkInterface')" },

    { "AddVertex 16",    $"g.addV('NetworkSecurityGroup').property('id', 'hack-test-nsg-1').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('ResourceGuid', '25d19e30-58bb-437a-ab45-859d33509259').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroup')" },
    { "AddVertex 17",    $"g.addV('NetworkSecurityGroup').property('id', 'hack-test-nsg-2').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('ResourceGuid', 'c1971903-9fba-4113-806c-6022001180bb').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroup')" },
    { "AddVertex 18",    $"g.addV('NetworkSecurityGroup').property('id', 'hack-test-nsg-3').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('ResourceGuid', '15b9e7d0-9fd8-4d9a-9a05-3ec13b9600e5').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroup')" },
    { "AddVertex 19",    $"g.addV('NetworkSecurityGroup').property('id', 'hack-test-vm-1-nsg').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('ResourceGuid', '5e97b80c-d194-40cb-b145-93c244edaeea').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroup')" },
    { "AddVertex 20",    $"g.addV('NetworkSecurityGroup').property('id', 'hack-test-vm-2-nsg').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('ResourceGuid', 'e5eb35f5-8d26-4287-8e5d-ccd8a3744477').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroup')" },
    { "AddVertex 21",    $"g.addV('NetworkSecurityGroup').property('id', 'hack-test-vm-3-nsg').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('ResourceGuid', '5cb77237-ea8b-41cf-970c-8922e94cf934').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroup')" },
    { "AddVertex 22",    $"g.addV('NetworkSecurityGroup').property('id', 'hack-test-vm-4-nsg').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('ResourceGuid', 'a0dd3f9a-c538-4ed9-9a3a-04d433a6022f').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroup')" },

    { "AddVertex 23",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-vm-4-nsg-NRMS-Rule-101').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, placeholder you can delete, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '443').property('DestinationAddressPrefix', '*').property('Priority', 101).property('Protocol', 'tcp').property('RuleType', 'User').property('SourceAddressPrefix', 'VirtualNetwork').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 24",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-vm-4-nsg-NRMS-Rule-103').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '*').property('DestinationAddressPrefix', '*').property('Priority', 103).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'CorpNetPublic').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 25",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-vm-4-nsg-NRMS-Rule-104').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Deny').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '1433,1434,3306,4333,5432,6379,7000,7001,7199,9042,9160,9300,16379,26379,27017').property('DestinationAddressPrefix', '*').property('Priority', 104).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'Internet').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },

    { "AddVertex 26",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-vm-1-nsg-NRMS-Rule-101').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, placeholder you can delete, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '443').property('DestinationAddressPrefix', '*').property('Priority', 101).property('Protocol', 'tcp').property('RuleType', 'User').property('SourceAddressPrefix', 'VirtualNetwork').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 27",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-vm-1-nsg-NRMS-Rule-103').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '*').property('DestinationAddressPrefix', '*').property('Priority', 103).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'CorpNetPublic').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 28",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-vm-1-nsg-NRMS-Rule-104').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Deny').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '1433,1434,3306,4333,5432,6379,7000,7001,7199,9042,9160,9300,16379,26379,27017').property('DestinationAddressPrefix', '*').property('Priority', 104).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'Internet').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },

    { "AddVertex 29",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-vm-2-nsg-NRMS-Rule-101').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, placeholder you can delete, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '443').property('DestinationAddressPrefix', '*').property('Priority', 101).property('Protocol', 'tcp').property('RuleType', 'User').property('SourceAddressPrefix', 'VirtualNetwork').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 30",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-vm-2-nsg-NRMS-Rule-103').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '*').property('DestinationAddressPrefix', '*').property('Priority', 103).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'CorpNetPublic').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 31",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-vm-2-nsg-NRMS-Rule-104').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Deny').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '1433,1434,3306,4333,5432,6379,7000,7001,7199,9042,9160,9300,16379,26379,27017').property('DestinationAddressPrefix', '*').property('Priority', 104).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'Internet').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },

    { "AddVertex 32",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-vm-3-nsg-NRMS-Rule-101').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, placeholder you can delete, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '443').property('DestinationAddressPrefix', '*').property('Priority', 101).property('Protocol', 'tcp').property('RuleType', 'User').property('SourceAddressPrefix', 'VirtualNetwork').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 33",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-vm-3-nsg-NRMS-Rule-103').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '*').property('DestinationAddressPrefix', '*').property('Priority', 103).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'CorpNetPublic').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 34",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-vm-3-nsg-NRMS-Rule-104').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Deny').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '1433,1434,3306,4333,5432,6379,7000,7001,7199,9042,9160,9300,16379,26379,27017').property('DestinationAddressPrefix', '*').property('Priority', 104).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'Internet').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },

    { "AddVertex 35",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-nsg-1-NRMS-Rule-101').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, placeholder you can delete, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '443').property('DestinationAddressPrefix', '*').property('Priority', 101).property('Protocol', 'tcp').property('RuleType', 'User').property('SourceAddressPrefix', 'VirtualNetwork').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 36",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-nsg-1-NRMS-Rule-103').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '*').property('DestinationAddressPrefix', '*').property('Priority', 103).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'CorpNetPublic').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 37",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-nsg-1-NRMS-Rule-104').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Deny').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '1433,1434,3306,4333,5432,6379,7000,7001,7199,9042,9160,9300,16379,26379,27017').property('DestinationAddressPrefix', '*').property('Priority', 104).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'Internet').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },

    { "AddVertex 38",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-nsg-2-NRMS-Rule-101').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, placeholder you can delete, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '443').property('DestinationAddressPrefix', '*').property('Priority', 101).property('Protocol', 'tcp').property('RuleType', 'User').property('SourceAddressPrefix', 'VirtualNetwork').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 39",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-nsg-2-NRMS-Rule-103').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '*').property('DestinationAddressPrefix', '*').property('Priority', 103).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'CorpNetPublic').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 40",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-nsg-2-NRMS-Rule-104').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Deny').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '1433,1434,3306,4333,5432,6379,7000,7001,7199,9042,9160,9300,16379,26379,27017').property('DestinationAddressPrefix', '*').property('Priority', 104).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'Internet').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },

    { "AddVertex 41",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-nsg-3-NRMS-Rule-101').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, placeholder you can delete, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '443').property('DestinationAddressPrefix', '*').property('Priority', 101).property('Protocol', 'tcp').property('RuleType', 'User').property('SourceAddressPrefix', 'VirtualNetwork').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 42",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-nsg-3-NRMS-Rule-103').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Allow').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '*').property('DestinationAddressPrefix', '*').property('Priority', 103).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'CorpNetPublic').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },
    { "AddVertex 43",    $"g.addV('NetworkSecurityGroupRule').property('id', 'hack-test-nsg-3-NRMS-Rule-104').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Access', 'Deny').property('Direction', 'Inbound').property('Description', 'Created by Azure Core Security managed policy, rule can be deleted but do not change source ips, please see aka.ms-cainsgpolicy').property('DestinationPortRange', '1433,1434,3306,4333,5432,6379,7000,7001,7199,9042,9160,9300,16379,26379,27017').property('DestinationAddressPrefix', '*').property('Priority', 104).property('Protocol', '*').property('RuleType', 'User').property('SourceAddressPrefix', 'Internet').property('SourcePortRange', '*').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'NetworkSecurityGroupRule')" },

    { "AddVertex 44",    $"g.addV('StorageAccount').property('id', 'hacktestsa1').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('publicNetworkAccess', 'Enabled').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'StorageAccount')" },
    { "AddVertex 45",    $"g.addV('StorageAccount').property('id', 'hacktestsa2').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('publicNetworkAccess', 'Enabled').property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'StorageAccount')" },

    { "AddVertex 46",    $"g.addV('FlowLogs').property('id', 'hack-test-vnet-1-hack-test-rg-flowlog').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Retention', 2).property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'FlowLogs')" },
    { "AddVertex 47",    $"g.addV('FlowLogs').property('id', 'hack-test-vnet-2-hack-test-rg-flowlog').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Retention', 2).property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'FlowLogs')" },
    { "AddVertex 48",    $"g.addV('FlowLogs').property('id', 'hack-test-vnet-3-hack-test-rg-flowlog').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Retention', 5).property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'FlowLogs')" },
    { "AddVertex 49",    $"g.addV('FlowLogs').property('id', 'hack-test-nsg-1-hack-test-rg-flowlog').property('DiscoveryRegion', 'eastus2').property('Region', 'eastus2').property('Subscription', 'a38f78b2-f840-4628-90f8-009ec9745a16').property('Retention', 5).property('CreatedTime', '{DateTime.Now}').property('LastModifiedTime', '{DateTime.Now}').property('pk', 'FlowLogs')" },

    { "AddEdge 1",      "g.V('hack-test-vnet-1').addE('subnet').from(g.V('hack-test-vnet-1-default'))" },
    { "AddEdge 2",      "g.V('hack-test-vnet-2').addE('subnet').from(g.V('hack-test-vnet-2-hack-test-subnet-2'))" },
    { "AddEdge 3",      "g.V('hack-test-vnet-2').addE('subnet').from(g.V('hack-test-vnet-2-default'))" },
    { "AddEdge 4",      "g.V('hack-test-vnet-3').addE('subnet').from(g.V('hack-test-vnet-3-hack-test-subnet-3'))" },

    { "AddEdge 5",      "g.V('hack-test-vnet-1-default').addE('security').from(g.V('hack-test-nsg-3'))" },
    { "AddEdge 6",      "g.V('hack-test-vnet-2-hack-test-subnet-2').addE('security').from(g.V('hack-test-nsg-1'))" },
    { "AddEdge 7",      "g.V('hack-test-vnet-2-default').addE('security').from(g.V('hack-test-nsg-2'))" },
    { "AddEdge 8",      "g.V('hack-test-vnet-3-hack-test-subnet-3').addE('security').from(g.V('hack-test-nsg-2'))" },

    { "AddEdge 9",      "g.V('hack-test-vnet-1-default').addE('compute').from(g.V('hack-test-vm-1'))" },
    { "AddEdge 10",      "g.V('hack-test-vnet-2-hack-test-subnet-2').addE('compute').from(g.V('hack-test-vm-3'))" },
    { "AddEdge 11",      "g.V('hack-test-vnet-2-default').addE('compute').from(g.V('hack-test-vm-2'))" },
    { "AddEdge 12",      "g.V('hack-test-vnet-3-hack-test-subnet-3').addE('compute').from(g.V('hack-test-vm-4'))" },

    { "AddEdge 46",      "g.V('hack-test-vm-1').addE('nic').from(g.V('hack-test-vm-1395_z1'))" },
    { "AddEdge 47",      "g.V('hack-test-vm-3').addE('nic').from(g.V('hack-test-vm-3653_z1'))" },
    { "AddEdge 48",      "g.V('hack-test-vm-2').addE('nic').from(g.V('hack-test-vm-2427_z1'))" },
    { "AddEdge 49",      "g.V('hack-test-vm-4').addE('nic').from(g.V('hack-test-vm-4238_z1'))" },

    { "AddEdge 13",      "g.V('hack-test-vm-1395_z1').addE('security').from(g.V('hack-test-vm-1-nsg'))" },
    { "AddEdge 14",      "g.V('hack-test-vm-3653_z1').addE('security').from(g.V('hack-test-vm-3-nsg'))" },
    { "AddEdge 15",      "g.V('hack-test-vm-2427_z1').addE('security').from(g.V('hack-test-vm-2-nsg'))" },
    { "AddEdge 16",      "g.V('hack-test-vm-4238_z1').addE('security').from(g.V('hack-test-vm-4-nsg'))" },

    { "AddEdge 17",      "g.V('hack-test-vm-1-nsg-NRMS-Rule-101').addE('rule').to(g.V('hack-test-vm-1-nsg'))" },
    { "AddEdge 18",      "g.V('hack-test-vm-1-nsg-NRMS-Rule-103').addE('rule').to(g.V('hack-test-vm-1-nsg'))" },
    { "AddEdge 19",      "g.V('hack-test-vm-1-nsg-NRMS-Rule-104').addE('rule').to(g.V('hack-test-vm-1-nsg'))" },
    { "AddEdge 20",      "g.V('hack-test-vm-2-nsg-NRMS-Rule-101').addE('rule').to(g.V('hack-test-vm-2-nsg'))" },
    { "AddEdge 21",      "g.V('hack-test-vm-2-nsg-NRMS-Rule-103').addE('rule').to(g.V('hack-test-vm-2-nsg'))" },
    { "AddEdge 22",      "g.V('hack-test-vm-2-nsg-NRMS-Rule-104').addE('rule').to(g.V('hack-test-vm-2-nsg'))" },
    { "AddEdge 23",      "g.V('hack-test-vm-3-nsg-NRMS-Rule-101').addE('rule').to(g.V('hack-test-vm-3-nsg'))" },
    { "AddEdge 24",      "g.V('hack-test-vm-3-nsg-NRMS-Rule-103').addE('rule').to(g.V('hack-test-vm-3-nsg'))" },
    { "AddEdge 25",      "g.V('hack-test-vm-3-nsg-NRMS-Rule-104').addE('rule').to(g.V('hack-test-vm-3-nsg'))" },
    { "AddEdge 26",      "g.V('hack-test-vm-4-nsg-NRMS-Rule-101').addE('rule').to(g.V('hack-test-vm-4-nsg'))" },
    { "AddEdge 27",      "g.V('hack-test-vm-4-nsg-NRMS-Rule-103').addE('rule').to(g.V('hack-test-vm-4-nsg'))" },
    { "AddEdge 28",      "g.V('hack-test-vm-4-nsg-NRMS-Rule-104').addE('rule').to(g.V('hack-test-vm-4-nsg'))" },
    { "AddEdge 29",      "g.V('hack-test-nsg-1-NRMS-Rule-101').addE('rule').to(g.V('hack-test-nsg-1'))" },
    { "AddEdge 30",      "g.V('hack-test-nsg-1-NRMS-Rule-103').addE('rule').to(g.V('hack-test-nsg-1'))" },
    { "AddEdge 31",      "g.V('hack-test-nsg-1-NRMS-Rule-104').addE('rule').to(g.V('hack-test-nsg-1'))" },
    { "AddEdge 32",      "g.V('hack-test-nsg-2-NRMS-Rule-101').addE('rule').to(g.V('hack-test-nsg-2'))" },
    { "AddEdge 33",      "g.V('hack-test-nsg-2-NRMS-Rule-103').addE('rule').to(g.V('hack-test-nsg-2'))" },
    { "AddEdge 34",      "g.V('hack-test-nsg-2-NRMS-Rule-104').addE('rule').to(g.V('hack-test-nsg-2'))" },
    { "AddEdge 35",      "g.V('hack-test-nsg-3-NRMS-Rule-101').addE('rule').to(g.V('hack-test-nsg-3'))" },
    { "AddEdge 36",      "g.V('hack-test-nsg-3-NRMS-Rule-103').addE('rule').to(g.V('hack-test-nsg-3'))" },
    { "AddEdge 37",      "g.V('hack-test-nsg-3-NRMS-Rule-104').addE('rule').to(g.V('hack-test-nsg-3'))" },

    { "AddEdge 38",      "g.V('hack-test-vnet-1-hack-test-rg-flowlog').addE('storage').to(g.V('hacktestsa1'))" },
    { "AddEdge 39",      "g.V('hack-test-vnet-2-hack-test-rg-flowlog').addE('storage').to(g.V('hacktestsa1'))" },
    { "AddEdge 40",      "g.V('hack-test-vnet-3-hack-test-rg-flowlog').addE('storage').to(g.V('hacktestsa2'))" },
    { "AddEdge 41",      "g.V('hack-test-nsg-1-hack-test-rg-flowlog').addE('storage').to(g.V('hacktestsa2'))" },

    { "AddEdge 42",      "g.V('hack-test-vnet-1-hack-test-rg-flowlog').addE('flows').from(g.V('hack-test-vnet-1'))" },
    { "AddEdge 43",      "g.V('hack-test-vnet-2-hack-test-rg-flowlog').addE('flows').from(g.V('hack-test-vnet-2'))" },
    { "AddEdge 44",      "g.V('hack-test-vnet-3-hack-test-rg-flowlog').addE('flows').from(g.V('hack-test-vnet-3'))" },
    { "AddEdge 45",      "g.V('hack-test-nsg-1-hack-test-rg-flowlog').addE('flows').from(g.V('hack-test-nsg-1'))" },
};



        // </defineQueries>

        // Starts a console application that executes every Gremlin query in the gremlinQueries dictionary. 
        static void Main(string[] args)
        {
            // <defineClientandServerObjects>
            string containerLink = "/dbs/" + Database + "/colls/" + Container;
            Console.WriteLine($"Connecting to: host: {Host}, port: {Port}, container: {containerLink}, ssl: {EnableSSL}");
            var gremlinServer = new GremlinServer(Host, Port, enableSsl: EnableSSL,
                                                    username: containerLink,
                                                    password: PrimaryKey);

            ConnectionPoolSettings connectionPoolSettings = new ConnectionPoolSettings()
            {
                MaxInProcessPerConnection = 10,
                PoolSize = 30,
                ReconnectionAttempts = 3,
                ReconnectionBaseDelay = TimeSpan.FromMilliseconds(500)
            };

            var webSocketConfiguration =
                new Action<ClientWebSocketOptions>(options =>
                {
                    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                });


            using (var gremlinClient = new GremlinClient(
                gremlinServer,
                new GraphSON2Reader(),
                new GraphSON2Writer(),
                GremlinClient.GraphSON2MimeType,
                connectionPoolSettings,
                webSocketConfiguration))
            {
                // </defineClientandServerObjects>

                // <executeQueries>
                foreach (var query in gremlinQueries)
                {
                    Console.WriteLine(String.Format("Running this query: {0}: {1}", query.Key, query.Value));

                    // Create async task to execute the Gremlin query.
                    var resultSet = SubmitRequest(gremlinClient, query).Result;
                    if (resultSet.Count > 0)
                    {
                        Console.WriteLine("\tResult:");
                        foreach (var result in resultSet)
                        {
                            // The vertex results are formed as Dictionaries with a nested dictionary for their properties
                            string output = JsonConvert.SerializeObject(result);
                            Console.WriteLine($"\t{output}");
                        }
                        Console.WriteLine();
                    }

                    // Print the status attributes for the result set.
                    // This includes the following:
                    //  x-ms-status-code            : This is the sub-status code which is specific to Cosmos DB.
                    //  x-ms-total-request-charge   : The total request units charged for processing a request.
                    //  x-ms-total-server-time-ms   : The total time executing processing the request on the server.
                    PrintStatusAttributes(resultSet.StatusAttributes);
                    Console.WriteLine();
                }
                // </executeQueries>
            }

            // Exit program
            Console.WriteLine("Done. Press any key to exit...");
            Console.ReadLine();
        }

        private static Task<ResultSet<dynamic>> SubmitRequest(GremlinClient gremlinClient, KeyValuePair<string, string> query)
        {
            try
            {
                return gremlinClient.SubmitAsync<dynamic>(query.Value);
            }
            catch (ResponseException e)
            {
                Console.WriteLine("\tRequest Error!");

                // Print the Gremlin status code.
                Console.WriteLine($"\tStatusCode: {e.StatusCode}");

                // On error, ResponseException.StatusAttributes will include the common StatusAttributes for successful requests, as well as
                // additional attributes for retry handling and diagnostics.
                // These include:
                //  x-ms-retry-after-ms         : The number of milliseconds to wait to retry the operation after an initial operation was throttled. This will be populated when
                //                              : attribute 'x-ms-status-code' returns 429.
                //  x-ms-activity-id            : Represents a unique identifier for the operation. Commonly used for troubleshooting purposes.
                PrintStatusAttributes(e.StatusAttributes);
                Console.WriteLine($"\t[\"x-ms-retry-after-ms\"] : {GetValueAsString(e.StatusAttributes, "x-ms-retry-after-ms")}");
                Console.WriteLine($"\t[\"x-ms-activity-id\"] : {GetValueAsString(e.StatusAttributes, "x-ms-activity-id")}");

                throw;
            }
        }

        private static void PrintStatusAttributes(IReadOnlyDictionary<string, object> attributes)
        {
            Console.WriteLine($"\tStatusAttributes:");
            Console.WriteLine($"\t[\"x-ms-status-code\"] : {GetValueAsString(attributes, "x-ms-status-code")}");
            Console.WriteLine($"\t[\"x-ms-total-server-time-ms\"] : {GetValueAsString(attributes, "x-ms-total-server-time-ms")}");
            Console.WriteLine($"\t[\"x-ms-total-request-charge\"] : {GetValueAsString(attributes, "x-ms-total-request-charge")}");
        }

        public static string GetValueAsString(IReadOnlyDictionary<string, object> dictionary, string key)
        {
            return JsonConvert.SerializeObject(GetValueOrDefault(dictionary, key));
        }

        public static object GetValueOrDefault(IReadOnlyDictionary<string, object> dictionary, string key)
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }

            return null;
        }

        public static ResultSet<dynamic> RunGremlinQuery(GremlinClient gremlinClient, KeyValuePair<string, string> query)
        {
            Console.WriteLine(String.Format("Running this query: {0}: {1}", query.Key, query.Value));

            // Create async task to execute the Gremlin query.
            var resultSet = GremlinNetSample.Program.SubmitRequest(gremlinClient, query).Result;
            Console.WriteLine($"Result set count: {resultSet.Count}");
            if (resultSet.Count > 0)
            {
                Console.WriteLine("\tResult:");
                foreach (var result in resultSet)
                {
                    // The vertex results are formed as Dictionaries with a nested dictionary for their properties
                    string output = JsonConvert.SerializeObject(result);
                    Console.WriteLine($"\t{output}");
                }
                Console.WriteLine();
            }

            // Print the status attributes for the result set.
            // This includes the following:
            //  x-ms-status-code            : This is the sub-status code which is specific to Cosmos DB.
            //  x-ms-total-request-charge   : The total request units charged for processing a request.
            //  x-ms-total-server-time-ms   : The total time executing processing the request on the server.
            PrintStatusAttributes(resultSet.StatusAttributes);
            Console.WriteLine();

            return resultSet;
        }
    }
}
