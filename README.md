# SfdcDataSync
### Synch data from Sql Server to Salesforce.com

Build with dotnet core 2.1 and using Sfdc partner wsdl to sync data.

## How to run
```
$ dotnet SfdcDataSync your-config.json [detail-log-path]
```

## System requirement
dotnet core 2.1

## Sample json config file
```
{
  "Name": "Name of your job",
  "Connections": [
    {
      "Name": "NameOfYourSqlConnection",
      "Type": "Sql",
      "Parameter": {
        "ConnectionString": "data source=\"[your server address]\";initial catalog=\"[your database name]\";user id=\"[your user name]\";password=\"[your password]\";max pool size=250;",
        "BatchSize": 200
      }
    },
    {
      "Name": "NameOfYourSfdcConnection",
      "Type": "Sfdc",
      "Parameter": {
        "UserName": "yoursfdc@user.name",
        "Password": "yoursfdcpassword",
        "Token": "yoursfdcapitoken",
        "Endpoint": "https:\/\/test.salesforce.com\/services\/Soap\/u\/43.0"
      }
    }
  ],
  "ResultLoggers": [
    {
      "Name": "YourResultLoggerName",
      "Config": {
        "Connection": "NameOfYourSqlConnection",
        "Command": "INSERT INTO dbo.Log (source_bussiness_key, target_source_bussiness_key, status, error_desc) VALUES (@key, @remote_key, @status, @message); UPDATE dbo.data SET Status = @status PK = @key;"
      }
    }
  ],
  "Tasks": [
    {
      "Name": "Your 1st Task Name",
      "Type": "SqlCommand",
      "TargetConnection": "NameOfYourSqlConnection",
      "TargetCommand": "TRUNCATE TABLE dbo.workspace;"
    },
    {
      "Name": "Your 2nd Task Name",
      "Type": "SqlToSfdc",
      "SourceConnection": "NameOfYourSqlConnection",
      "SourceCommand": "Select field1, field2, field3, field4, 'RecordTypeId'As RecordTypeId From dbo.TableName",
      "TargetConnection": "NameOfYourSfdcConnection",
      "TargetCommand": {
        "Operation": "Upsert",
        "Object": "Contact",
        "UpsertKeyField": "YourUpsertKey__c"
      },
      "Mapping": [
        {
          "From": "field1",
          "To": "YourUpsertKey__c"
        },
        {
          "From": "FirstName",
          "To": "field2",
          "UpdateOnNull": false
        },
        {
          "From": "LastName",
          "To": "field3",
          "UpdateOnNull": false
        },
        {
          "From": "RecordTypeId",
          "To": "RecordTypeId"
        },
        {
          "From": "Email",
          "To": "field4",
          "UpdateOnNull": false
        }
      ],
      "ResultLogger": "YourResultLoggerName"
    },
    {
      "Name": "Your 3rd Task Name",
      "Type": "SfdcToSql",
      "TargetConnection": "NameOfYourSqlConnection",
      "TargetCommand": "INSERT INTO dbo.Contact (Id, AccountId) VALUES( @Id, @AccountId)",
      "SourceConnection": "NameOfYourSfdcConnection",
      "SourceCommand": {
        "Operation": "Select",
        "CommandText": "Select c.Id, c.AccountId From Contact c"
      },
      "Mapping": [
        {
          "From": "Id",
          "To": "Id"
        },
        {
          "From": "AccountId",
          "To": "AccountId"
        }
      ]
    }
  ]
}
```
## Connection Type
- Sql : connection string to connect to your sql server
- Sfdc : connection information to connect to your Sfdc environtment

## Task Type
- SqlCommand : Adhoc sql command to execute to your sql server
- SqlToSfdc : Transfer data from your sql server to sfdc
- SfdcToSql : Transfer data from sfdc to your sql server