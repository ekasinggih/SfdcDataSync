using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SfdcDataSync.Core.Adapter;

namespace SfdcDataSync.Core.IntegrationTest
{
    [TestClass]
    public class ConfigurationTest
    {
        [TestMethod]
        public void JobConfiguration()
        {
            var job = new Job();
            job.Name = "Retail Customer";
            job.Connections.Add(new RawConnection
            {
                Name = "SqlConnection",
                Type = ConnectionType.Sql,
                Parameter = new SqlConnectionParameter { ConnectionString = "data source=\"[server address]\";initial catalog=\"[db name]\";user id=\"[user id]\";password=\"[passowrd]\"", BatchSize = 200 }
            });
            job.Connections.Add(new RawConnection
            {
                Name = "SfdcConnection",
                Type = ConnectionType.Sfdc,
                Parameter = new SfdcConnectionParameter
                {
                    Endpoint = "https://test.salesforce.com/services/Soap/u/43.0",
                    UserName = "[username]",
                    Password = "[password]",
                    Token = "[token]"
                }

            });
            job.ResultLoggers.Add(new ResultLoggerConfig
            {
                Name = "Sql",
                Config = new SqlResultLoggerConfig
                {
                    Connection = "SqlConnection",
                    Command = "INSERT INTO dbo.ProcessHistory (id, object_type, primary_key, status, error_desc, create_date) VALUES ( @key, N'RTContact', @remote_key, @status, @message, GETDATE())"
                }
            });
            job.Tasks.Add(new SyncTask
            {
                Name = "Retail Contact Upsert to SFDC Contact",
                Type = SyncType.SqlToSfdc,
                SourceConnection = "SqlConnection",
                SourceCommand = @"SELECT LastName
	, FirstName
	, '01236000000xxWCAAY' AS RecordTypeId
	, Address + CHAR(10) + CHAR(13) + Address2 AS MaillingStreet
	, Phone
	, MobileNo
	, Email
	, 'POS' AS LeadSource
	, '0053C000000zKGaQAM' AS OwnerId
	, BirthDay
	, Gender
	, BirthMonth
	, No
	, Action
FROM RTContacts_TEST
WHERE Action = 'Modify';",
                TargetConnection = "SfdcConnection",
                TargetCommand = new SfdcCommand
                {
                    Operation = SfdcOperation.Upsert,
                    Object = "Contact",
                    UpsertKeyField = "No__c"
                },
                Mapping = new List<FieldMapping>(),
                ResultLogger = "Sql"
            });
            job.Tasks[0].Mapping.Add(new FieldMapping { To = "LastName", From = "LastName" });
            job.Tasks[0].Mapping.Add(new FieldMapping { To = "FirstName", From = "FirstName" });
            job.Tasks[0].Mapping.Add(new FieldMapping { To = "RecordTypeId", From = "RecordTypeId" });
            job.Tasks[0].Mapping.Add(new FieldMapping { To = "MailingStreet", From = "MaillingStreet" });
            job.Tasks[0].Mapping.Add(new FieldMapping { To = "Phone", From = "Phone" });
            job.Tasks[0].Mapping.Add(new FieldMapping { To = "MobilePhone", From = "MobileNo" });
            job.Tasks[0].Mapping.Add(new FieldMapping { To = "Email", From = "Email", UpdateOnNull = false });
            job.Tasks[0].Mapping.Add(new FieldMapping { To = "LeadSource", From = "LeadSource" });
            job.Tasks[0].Mapping.Add(new FieldMapping { To = "OwnerId", From = "OwnerId" });
            job.Tasks[0].Mapping.Add(new FieldMapping { To = "Birth_Day__c", From = "BirthDay" });
            job.Tasks[0].Mapping.Add(new FieldMapping { To = "Gender__c", From = "Gender" });
            job.Tasks[0].Mapping.Add(new FieldMapping { To = "Birth_Month_Only__c", From = "BirthMonth" });
            job.Tasks[0].Mapping.Add(new FieldMapping { To = "No__c", From = "No" });

            var json = JsonConvert.SerializeObject(job);

            #region JSON Result
            /*
{
   "Name":"Retail Customer",
   "Connections":[
      {
         "Name":"SqlConnection",
         "Type":0,
         "Parameter":{
            "ConnectionString":"data source=\"[server address]\";initial catalog=\"[db name]\";user id=\"[user id]\";password=\"[passowrd]\"",
            "BatchSize":200
         }
      },
      {
         "Name":"SfdcConnection",
         "Type":1,
         "Parameter":{
            "UserName":"[username]",
            "Password":"[password]",
            "Token":"[token]",
            "ApiPassword":"[api passowrd]",
            "Endpoint":"https://test.salesforce.com/services/Soap/u/43.0"
         }
      }
   ],
   "ResultLoggers":[
      {
         "Name":"Sql",
         "Config":{
            "Connection":"SqlConnection",
            "Command":"INSERT INTO dbo.ProcessHistory (id, object_type, primary_key, status, error_desc, create_date) VALUES ( @key, N'RTContact', @remote_key, @status, @message, GETDATE())"
         }
      }
   ],
   "Tasks":[
      {
         "Name":"Retail Contact Upsert to SFDC Contact",
         "Type":0,
         "SourceConnection":"SqlConnection",
         "SourceCommand":"SELECT LastName\r\n\t, FirstName\r\n\t, '01236000000xxWCAAY' AS RecordTypeId\r\n\t, Address + CHAR(10) + CHAR(13) + Address2 AS MaillingStreet\r\n\t, Phone\r\n\t, MobileNo\r\n\t, Email\r\n\t, 'POS' AS LeadSource\r\n\t, '0053C000000zKGaQAM' AS OwnerId\r\n\t, BirthDay\r\n\t, Gender\r\n\t, BirthMonth\r\n\t, No\r\n\t, Action\r\nFROM RTContacts_TEST\r\nWHERE Action = 'Modify';",
         "TargetConnection":"SfdcConnection",
         "TargetCommand":{
            "Operation":1,
            "CommandText":null,
            "Object":"Contact",
            "UpsertKeyField":"No__c"
         },
         "Mapping":[
            {
               "From":"LastName",
               "To":"LastName",
               "UpdateOnNull":true
            },
            {
               "From":"FirstName",
               "To":"FirstName",
               "UpdateOnNull":true
            },
            {
               "From":"RecordTypeId",
               "To":"RecordTypeId",
               "UpdateOnNull":true
            },
            {
               "From":"MaillingStreet",
               "To":"MailingStreet",
               "UpdateOnNull":true
            },
            {
               "From":"Phone",
               "To":"Phone",
               "UpdateOnNull":true
            },
            {
               "From":"MobileNo",
               "To":"MobilePhone",
               "UpdateOnNull":true
            },
            {
               "From":"Email",
               "To":"Email",
               "UpdateOnNull":false
            },
            {
               "From":"LeadSource",
               "To":"LeadSource",
               "UpdateOnNull":true
            },
            {
               "From":"OwnerId",
               "To":"OwnerId",
               "UpdateOnNull":true
            },
            {
               "From":"BirthDay",
               "To":"Birth_Day__c",
               "UpdateOnNull":true
            },
            {
               "From":"Gender",
               "To":"Gender__c",
               "UpdateOnNull":true
            },
            {
               "From":"BirthMonth",
               "To":"Birth_Month_Only__c",
               "UpdateOnNull":true
            },
            {
               "From":"No",
               "To":"No__c",
               "UpdateOnNull":true
            }
         ],
         "ResultLogger":null
      }
   ]
}
            */
            #endregion
        }

        [TestMethod]
        public void SerializeJobConfiguration()
        {
            var json = File.ReadAllText("config.json");

            var job = JsonConvert.DeserializeObject<Job>(json);
        }
    }
}
