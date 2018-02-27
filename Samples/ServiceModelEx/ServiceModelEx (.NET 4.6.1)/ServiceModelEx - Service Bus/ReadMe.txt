You will need the Microsoft.ServiceBus.dll assembly.

You can install the Azure SDK, and then add a reference to the assembly at:
C:\Program Files\Microsoft SDKs\Azure\.NET SDK\v2.8\ToolsRef\Microsoft.ServiceBus.dll

You can also get the Microsoft.ServiceBus.dll assembly from Nuget by placing in the root a file called packages.config
with this reference inside: 

<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="WindowsAzure.ServiceBus" version="3.1.2" targetFramework="net461" />
</packages>
